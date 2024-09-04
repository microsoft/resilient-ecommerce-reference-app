using Api.Models.Entities;

namespace Api.Models.DTO
{
    /// <summary>
    /// The representation of a ticket.
    /// This is the model the API reports to clients.
    /// </summary>
    public class TicketDto
    {
        public required string Id { get; set; }

        public required string ConcertId { get; set; }
        public Concert? Concert { get; set; }

        public required string UserId { get; set; }
    }
}
