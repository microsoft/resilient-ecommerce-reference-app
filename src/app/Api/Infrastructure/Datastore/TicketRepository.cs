using Api.Models.Common;
using Api.Models.Entities;
using Api.Services.Repositories;
using Api.Services.Repositories.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Api.Infrastructure.Datastore
{
    /// <summary>
    /// Implementation of the ticket data model repository that uses the Entity Framework.
    /// </summary>
    public class TicketRepository : ITicketRepository, IDisposable
    {
        private readonly DatabaseContext _database;

        public TicketRepository(DatabaseContext database)
        {
            _database = database;
        }

        /// <inheritdoc />
        public async Task<PagedResponse<Ticket>> GetAllTicketsForUserAsync(string userId, int skip, int take)
        {
            var pageOfData = await _database.Tickets.AsNoTracking().Include(ticket => ticket.UserId).Where(ticket => ticket.UserId == userId)
                .OrderByDescending(ticket => ticket.Id).Skip(skip).Take(take).ToListAsync();
            var totalCount = await _database.Tickets.Where(ticket => ticket.UserId == userId).CountAsync();

            return new PagedResponse<Ticket>() { PageData = pageOfData, Skipped = skip, PageSize = pageOfData.Count, TotalCount = totalCount };
        }

        /// <inheritdoc />
        public async Task<Ticket> GetTicketByIdAsync(string ticketId)
        {
            var ticket = await _database.Tickets.AsNoTracking().Where(ticket => ticket.Id == ticketId).SingleOrDefaultAsync();
            return ticket ?? throw new NotFoundException($"Ticket with ID '{ticketId}' does not exist");
        }

        /// <inheritdoc />
        public async Task<ICollection<Ticket>> TryEmitTickets(string userId, IDictionary<string, int> cartData)
        {

            var retryStrategy = _database.Database.CreateExecutionStrategy();

            return await retryStrategy.ExecuteAsync(async () =>
            {
                using var transaction = _database.Database.BeginTransaction();
                var reservedTickets = new List<Ticket>();

                try
                {
                    foreach (var cartItem in cartData)
                    {
                        string concertId = cartItem.Key;
                        int numTickets = cartItem.Value;

                        var ticketsForConcert = Enumerable.Range(0, numTickets).Select(
                            _ => new Ticket
                            {
                                ConcertId = concertId,
                                UserId = userId,
                            });

                        _database.Tickets.AddRange(ticketsForConcert);
                        reservedTickets.AddRange(ticketsForConcert);
                    }

                    await _database.SaveChangesAsync();
                    transaction.Commit();

                    return reservedTickets;
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw;
                }
            });
        }

        public void Dispose() => _database?.Dispose();
    }
}
