using Api.Models.Entities;
using Api.Services.Repositories;
using Api.Services.Repositories.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace Api.Infrastructure.Datastore
{
    /// <summary>
    /// Implementation of the concert data model repository that uses
    /// the Entity Framework and a Redis cache to store and retrieve data.
    /// </summary>
    public class CachedConcertRepository : IConcertRepository, IDisposable
    {
        private readonly DatabaseContext _database;
        private readonly IDistributedCache _cache;

        public CachedConcertRepository(DatabaseContext database, IDistributedCache cache)
        {
            _database = database;
            _cache = cache;
        }

        /// <inheritdoc />
        public async Task<Concert> CreateConcertAsync(Concert newConcert)
        {
            _database.Add(newConcert);
            await _database.SaveChangesAsync();

            _cache.Remove(CacheKeys.UpcomingConcerts);
            return newConcert;
        }

        /// <inheritdoc />
        public async Task<Concert> UpdateConcertAsync(Concert concert)
        {
            var updatedConcert = await _database.Concerts.FindAsync(concert.Id);
            if (updatedConcert == null)
            {
                throw new NotFoundException($"Can't update concert info. Concert with id '{concert.Id}' not found");
            }

            updatedConcert.StartTime = concert.StartTime;
            updatedConcert.Price = concert.Price;
            updatedConcert.UpdatedOn = DateTimeOffset.UtcNow;

            await _database.SaveChangesAsync();

            _cache.Remove(CacheKeys.UpcomingConcerts);
            return updatedConcert;
        }

        /// <inheritdoc />
        public async Task DeleteConcertAsync(string concertId)
        {
            var existingConcert = _database.Concerts.SingleOrDefault(c => c.Id == concertId);
            if (existingConcert == null)
            {
                throw new NotFoundException($"Can't delete concert. Concert with id '{concertId}' not found");
            }

            _database.Remove(existingConcert);
            await _database.SaveChangesAsync();

            _cache.Remove(CacheKeys.UpcomingConcerts);
        }

        /// <inheritdoc />
        public async Task<Concert> GetConcertByIdAsync(string concertId)
        {
            var concert = await _database.Concerts.AsNoTracking().Where(c => c.Id == concertId).SingleOrDefaultAsync();
            return concert ?? throw new NotFoundException($"Concert with id '{concertId}' does not exist");
        }

        /// <inheritdoc />
        public async Task<IEnumerable<Concert>> GetUpcomingConcertsAsync(int count)
        {
            var concertsJson = await _cache.GetStringAsync(CacheKeys.UpcomingConcerts);
            if (concertsJson != null)
            {
                // We have cached data, deserialize the JSON data
                var cachedConcerts = JsonSerializer.Deserialize<ICollection<Concert>>(concertsJson);

                if (cachedConcerts?.Count >= count)
                {
                    // We have enough data cached. No need to fetch from DB
                    return cachedConcerts.Take(count);
                }
            }

            // Insufficient data in the cache, retrieve data from the repository and cache it for one hour
            var concerts = await _database.Concerts.AsNoTracking()
                .Where(c => c.StartTime > DateTimeOffset.UtcNow && c.IsVisible)
                .OrderBy(c => c.StartTime)
                .Take(count)
                .ToListAsync();
            concertsJson = JsonSerializer.Serialize(concerts);
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
            };
            await _cache.SetStringAsync(CacheKeys.UpcomingConcerts, concertsJson, cacheOptions);

            return concerts ?? [];
        }

        public void Dispose() => _database?.Dispose();
    }
}
