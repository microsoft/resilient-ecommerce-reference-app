using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Models.Entities
{
    /// <summary>
    /// The representation of a concert.
    /// This is the entity modeled as the DB schema.
    /// </summary>
    public class Concert
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string? Id { get; set; }
        public bool IsVisible { get; set; }
        public string Artist { get; set; } = string.Empty;
        public string Genre { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public double Price { get; set; }
        public DateTimeOffset StartTime { get; set; } = DateTimeOffset.UtcNow.AddDays(30);
        public DateTimeOffset CreatedOn { get; set; } = DateTimeOffset.UtcNow;
        public string CreatedBy { get; set; } = string.Empty;
        public DateTimeOffset UpdatedOn { get; set; } = DateTimeOffset.UtcNow;
        public string UpdatedBy { get; set; } = string.Empty;

        /// <summary>
        /// Required when the selected Ticket Management Service is not ReleCloud Api
        /// </summary>
        public string? TicketManagementServiceConcertId { get; set; } = string.Empty;
    }
}