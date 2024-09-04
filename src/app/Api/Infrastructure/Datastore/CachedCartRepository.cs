using Api.Services.Repositories;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;

namespace Api.Infrastructure.Datastore
{
    /// <summary>
    /// Implementation of the cart data model repository that uses a Redis cache
    /// </summary>
    public class CachedCartRepository : ICartRepository, IDisposable
    {
        private readonly IDistributedCache cache;

        public CachedCartRepository(IDistributedCache cache)
        {
            this.cache = cache;
        }

        /// <inheritdoc />
        public async Task ClearCartAsync(string userId)
        {
            var currentCart = new Dictionary<string, int>();
            await UpdateCartAsync(userId, currentCart);
        }

        /// <inheritdoc />
        public async Task<IDictionary<string, int>> GetCartAsync(string userId)
        {
            var cacheKey = CacheKeys.Cart(userId);
            var cartData = await cache.GetStringAsync(cacheKey);

            if (cartData == null)
            {
                return new Dictionary<string, int>();
            }
            else
            {
                var cartObject = JsonConvert.DeserializeObject<Dictionary<string, int>>(cartData);
                return cartObject ?? new Dictionary<string, int>();
            }
        }

        /// <inheritdoc />
        public async Task<IDictionary<string, int>> UpdateCartAsync(string userId, string concertId, int count)
        {
            var currentCart = await GetCartAsync(userId);

            if (count == 0)
            {
                currentCart.Remove(concertId);
            }
            else
            {
                currentCart[concertId] = count;
            }

            await UpdateCartAsync(userId, currentCart);
            return currentCart;
        }

        /// <inheritdoc />
        private async Task UpdateCartAsync(string userId, IDictionary<string, int> currentCart)
        {
            var cartData = JsonConvert.SerializeObject(currentCart);
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
            };
            var cacheKey = CacheKeys.Cart(userId);

            await cache.SetStringAsync(cacheKey, cartData, cacheOptions);
        }

        public void Dispose()
        { }
    }
}
