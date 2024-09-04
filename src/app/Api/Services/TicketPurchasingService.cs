using Api.Models.Entities;
using Api.Services.Repositories;
using Microsoft.IdentityModel.Tokens;

namespace Api.Services
{
    /// <summary>
    /// The base interface for the ticket management service.
    /// Each implementation will represent a different strategy of managing
    /// tickets' availability and reservation.
    /// </summary>
    public abstract class TicketPurchasingService
    {
        protected readonly IConcertRepository concertRepository;
        protected readonly ITicketRepository ticketRepository;
        protected readonly IOrderRepository orderRepository;
        protected readonly ICartRepository cartRepository;
        protected readonly ILogger<TicketPurchasingService> logger;

        public TicketPurchasingService(IConcertRepository concertRepository,
                                       ITicketRepository ticketRepository,
                                       IOrderRepository orderRepository,
                                       ICartRepository cartRepository,
                                       ILogger<TicketPurchasingService> logger)
        {
            this.concertRepository = concertRepository;
            this.ticketRepository = ticketRepository;
            this.orderRepository = orderRepository;
            this.cartRepository = cartRepository;
            this.logger = logger;
        }

        /// <summary>
        /// Returns whether or not the specified number of tickets are available for purchase.
        /// </summary>
        /// <param name="concertId">the concert to query the availability of</param>
        /// <returns>the number of available tickets</returns>
        /// <exception cref="InvalidOperationException">if there are no tickets available for the given purchase</exception>
        protected abstract Task CheckTicketAvailability(string concertId, int numTickets);

        protected abstract Task<Order> ExecutePurchaseAsync(string userId, IDictionary<string, int> cartData);

        /// <summary>
        /// Checks out and purchases the tickets found in the given user's cart.
        /// If for any reason the tickets couldn't be purchased, an exception is thrown.
        /// If the purchase is successful, the cart is cleared and the purchased tickets are returned.
        /// </summary>
        /// <param name="userId">the user ID whose cart to checkout</param>
        /// <returns>the purchased tickets</returns>
        /// <exception cref="InvalidOperationException">the checkout operation could not be processed due to ticket availability or user input</exception>
        public async Task<Order> TryCheckoutTickets(string userId)
        {
            var cart = await cartRepository.GetCartAsync(userId);
            if (cart.IsNullOrEmpty())
            {
                throw new InvalidOperationException("Can't checkout tickets. Cart is empty.");
            }

            // Validate availability for all concerts
            await Task.WhenAll(cart.Select(cartItem => CheckTicketAvailability(cartItem.Key, cartItem.Value)));

            // TODO: This is the async part that should be implemented by a derived, async-running, service
            // that guarantees the atomicity of the all the operations involved in a checkout.
            // I.e. a good opportunity to integrate another QCS decoupling Azure service (ServiceBus / LogicApps / Functions etc.).
            // The API will expose the necessary REST endpoints for all the required operations. The async service
            // will orchestrate them.
            return await ExecutePurchaseAsync(userId, cart);
        }
    }
}
