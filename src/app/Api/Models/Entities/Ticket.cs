using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Models.Entities
{
    /// <summary>
    /// The representation of a ticket.
    /// This is the entity modeled as the DB schema.
    /// </summary>
    public class Ticket
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string? Id { get; set; }

        public string? ConcertId { get; set; }
        public Concert? Concert { get; set; }

        public string? UserId { get; set; }
        public User? User { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}
