using Api.Models.Common;
using Api.Models.Entities;

namespace Api.Services.Repositories
{
    /// <summary>
    /// Interface exposing methods for interacting with the 'Ticket' entities in the database.
    /// </summary>
    public interface ITicketRepository
    {
        /// <summary>
        /// Retrieves the ticket with the specified ID.
        /// </summary>
        /// <param name="ticketId">the ID of the ticket to retrieve</param>
        /// <returns></returns>
        Task<Ticket> GetTicketByIdAsync(string ticketId);

        /// <summary>
        /// Emits (i.e. creates the ticket entities in the DB) for the given user and the associated cart data.
        /// </summary>
        /// <param name="userId">the user ID to emit the tickets for</param>
        /// <param name="cart">the cart data mapping of {concertID}-{ticketsCount} to emit</param>
        /// <returns>the collection of tickets that were successfully committed to the DB</returns>
        Task<ICollection<Ticket>> TryEmitTickets(string userId, IDictionary<string, int> cart);

        /// <summary>
        /// Retrieves all tickets for the specified user, paginated.
        /// </summary>
        /// <param name="userId">the user ID whose tickets to retrieve</param>
        /// <param name="skip">the number of tickets, ordered by timestamp, to skip</param>
        /// <param name="take">the number of tickets to retrieve, starting from {skip} onwards</param>
        /// <returns></returns>
        Task<PagedResponse<Ticket>> GetAllTicketsForUserAsync(string userId, int skip, int take);
    }
}
