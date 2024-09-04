using Api.Models.Entities;

namespace Api.Models.DTO
{
    /// <summary>
    /// The representation of an order.
    /// This is the model the API reports to clients.
    /// </summary>
    public class OrderDto
    {
        public required string Id { get; set; }

        public ICollection<TicketDto> Tickets { get; set; } = [];

        public required string UserId { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}
