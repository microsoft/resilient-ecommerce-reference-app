using Api.Models.Common;
using Api.Models.Entities;
using Api.Services.Repositories;
using Api.Services.Repositories.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Api.Infrastructure.Datastore
{
    /// <summary>
    /// Implementation of the order data model repository that uses the Entity Framework.
    /// </summary>
    public class OrderRepository : IOrderRepository, IDisposable
    {
        private readonly DatabaseContext _database;

        public OrderRepository(DatabaseContext database)
        {
            _database = database;
        }

        /// <inheritdoc />
        public async Task<PagedResponse<Order>> GetAllOrdersForUserAsync(string userId, int skip, int take)
        {
            var pageOfData = await _database.Orders.AsNoTracking().Include(order => order.Tickets).Where(order => order.UserId == userId)
                .OrderByDescending(order => order.Id).Skip(skip).Take(take).ToListAsync();
            var totalCount = await _database.Orders.Where(order => order.UserId == userId).CountAsync();

            return new PagedResponse<Order>() { PageData = pageOfData, Skipped = skip, PageSize = pageOfData.Count, TotalCount = totalCount };
        }

        /// <inheritdoc />
        public async Task<Order> GetOrderByIdAsync(string orderId)
        {
            var order = await _database.Orders.AsNoTracking().Where(order => order.Id == orderId).SingleOrDefaultAsync();
            return order ?? throw new NotFoundException($"Order with ID '{orderId}' does not exist");
        }

        /// <inheritdoc />
        public async Task<Order> CreateOrder(string userId, ICollection<Ticket> tickets)
        {
            var order = new Order()
            {
                Tickets = tickets,
                UserId = userId,
            };
            _database.Orders.Add(order);
            await _database.SaveChangesAsync();

            return order;
        }

        public void Dispose() => _database?.Dispose();
    }
}
