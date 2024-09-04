using Api.Models.Common;
using Api.Models.Entities;

namespace Api.Services.Repositories
{
    /// <summary>
    /// Interface exposing methods for interacting with the 'Order' entities in the database.
    /// </summary>
    public interface IOrderRepository
    {
        /// <summary>
        /// Retrieve an order by its ID.
        /// </summary>
        /// <param name="orderId">the ID of the order to retrieve</param>
        /// <returns>the tracked model entity of the order</returns>
        /// <exception cref="NotFoundException">if the order entity could not be found</exception>
        Task<Order> GetOrderByIdAsync(string orderId);

        /// <summary>
        /// Creates a new order for the specified user with the specified tickets.
        /// </summary>
        /// <param name="userId">the ID of the user the order is for</param>
        /// <param name="tickets">the collection of tickets to checkout as part of this order</param>
        /// <returns>the tracked order database entity that has been newly created</returns>
        Task<Order> CreateOrder(string userId, ICollection<Ticket> tickets);

        /// <summary>
        /// Retrieves all orders for the specified user, paginated.
        /// </summary>
        /// <param name="userId">the ID of the user whose orders to retrieve</param>
        /// <param name="skip">the number of orders, ordered by timestamp, to skip</param>
        /// <param name="take">the number of orders to retrieve, starting from {skip} onwards</param>
        /// <returns>a page of tracked order entities</returns>
        Task<PagedResponse<Order>> GetAllOrdersForUserAsync(string userId, int skip, int take);
    }
}
