using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Models.Entities
{
    /// <summary>
    /// The representation of a user.
    /// This is the entity modeled as the DB schema.
    /// </summary>
    public class User
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string? Id { get; set; }

        public string DisplayName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}
