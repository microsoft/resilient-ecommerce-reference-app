using System.ComponentModel.DataAnnotations;

namespace Api.Models.DTO
{
    /// <summary>
    /// The HTTP request model the API accepts when creating a new user.
    /// </summary>
    public class UserRequest
    {
        [Required]
        [EmailAddress]
        public required string Email { get; set; }

        [StringLength(16)]
        public string Phone { get; set; } = string.Empty;

        [Required]
        [StringLength(32, MinimumLength = 4)]
        public required string DisplayName { get; set; }
    }
}
