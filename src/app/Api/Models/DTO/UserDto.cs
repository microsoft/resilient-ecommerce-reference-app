using System.ComponentModel.DataAnnotations;

namespace Api.Models.DTO
{
    /// <summary>
    /// The representation of a user.
    /// This is the model the API reports to clients.
    /// </summary>
    public class UserDto
    {
        [Required]
        public required string Id { get; set; }

        [Required]
        [EmailAddress]
        public required string Email { get; set; }

        [StringLength(16)]
        public string Phone { get; set; } = string.Empty;

        [Required]
        [StringLength(64, MinimumLength = 4)]
        public required string DisplayName { get; set; }
    }
}
