using System.ComponentModel.DataAnnotations;

namespace Api.Models.DTO
{
    /// <summary>
    /// The representation of a cart item.
    /// This is the model the API reports to clients.
    /// </summary>
    public class CartItemDto
    {
        public required string ConcertId { get; set; }

        [Range(0, 10)]
        public required int Quantity { get; set; }
    }
}
