using Api.Models.Entities;
using Api.Services.Repositories;

namespace Api.Services
{
    /// <summary>
    /// Service for managing tickets. This particular implementation has no seating
    /// restrictions whatsoever. Tickets are available indefinitely, with a maximum purchase
    /// order of 10.
    /// 
    /// Executes a synchronous best-effort of purchasing tickets, without any atomicity guarantees.
    /// This is an anti-pattern for production-grade systems, but is useful for testing purposes.
    /// </summary>
    public class NonAtomicTicketPurchasingService : TicketPurchasingService
    {
        public NonAtomicTicketPurchasingService(IConcertRepository concertRepository, ITicketRepository ticketRepository, IOrderRepository orderRepository, ICartRepository cartRepository, ILogger<NonAtomicTicketPurchasingService> logger)
            : base(concertRepository, ticketRepository, orderRepository, cartRepository, logger)
        {
        }

        /// <summary>
        /// At all times will return 10 tickets available, without any other restrictions (infinite seating).
        /// A single purchase should not exceed 10 tickets.
        /// </summary>
        /// <param name="concertId">the concert ID to query the remaining ticket availability of</param>
        /// <param name="numTickets">the number of tickets trying to be purchased</param>
        /// <exception cref="InvalidOperationException">if the number of tickets exceeds 10</exception>"
        protected override Task CheckTicketAvailability(string concertId, int numTickets)
        {
            if (numTickets > 10)
            {
                throw new InvalidOperationException("Can't purchase more than 10 tickets at once.");
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Executes a synchronous best-effort of purchasing tickets, without any atomicity guarantees.
        /// This is an anti-pattern for production-grade systems, but is useful for testing purposes.
        /// </summary>
        /// <param name="userId">the user ID for which the purchase was requested</param>
        /// <param name="cartData">the associated cart of {concertId}-{quantity} tickets to purchase</param>
        /// <returns></returns>
        protected async override Task<Order> ExecutePurchaseAsync(string userId, IDictionary<string, int> cartData)
        {
            var tickets = await ticketRepository.TryEmitTickets(userId, cartData);
            var order = await orderRepository.CreateOrder(userId, tickets);
            await cartRepository.ClearCartAsync(userId);

            return order;
        }
    }
}
