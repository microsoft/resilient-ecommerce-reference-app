using System.Collections;
using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Models.Entities
{
    /// <summary>
    /// The representation of an order.
    /// This is the entity modeled as the DB schema.
    /// </summary>
    public class Order
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string? Id { get; set; }

        public ICollection<Ticket> Tickets { get; set; } = [];

        public string? UserId { get; set; }
        public User? User { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}
