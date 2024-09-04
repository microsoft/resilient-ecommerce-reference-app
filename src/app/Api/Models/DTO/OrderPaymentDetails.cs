using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace Api.Models.DTO
{
    /// <summary>
    /// The request body expected when creating a new order item.
    /// </summary>
    public class OrderPaymentDetails : IValidatableObject
    {
        [Required]
        [MaxLength(75)]
        [DisplayName("Cardholder Name")]
        public required string Cardholder { get; set; }

        [Required]
        [CreditCard]
        public required string CardNumber { get; set; }

        [Required]
        [MaxLength(3)]
        [DisplayName("Security Code")]
        [RegularExpression("^[0-9]{3}$", ErrorMessage = "Please enter a valid security code (3-letter CVV)")]
        public required string SecurityCode { get; set; }

        [Required]
        [MaxLength(4)]
        [DisplayName("Expiration")]
        [RegularExpression("^[0,1][0-9][2,3][0-9]$", ErrorMessage = "The expiration date must be MMYY format")]
        public required string ExpirationMonthYear { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!string.IsNullOrEmpty(ExpirationMonthYear) && ExpirationMonthYear.Length == 4)
            {
                var monthSplitString = ExpirationMonthYear.Substring(0, 2);
                var yearSplitString = ExpirationMonthYear.Substring(2, 2);
                if (int.TryParse(monthSplitString, out int cardExpirationMonth) && int.TryParse(yearSplitString, out int cardExpirationYear))
                {
                    if (DateTimeOffset.UtcNow.Year > cardExpirationYear + 2000
                        || DateTimeOffset.UtcNow.Year == cardExpirationYear + 2000 && DateTime.UtcNow.Month > cardExpirationMonth)
                    {
                        yield return new ValidationResult("Please use a card that has not expired", new[] { nameof(ExpirationMonthYear) });
                    }
                    if (cardExpirationMonth > 12)
                    {
                        yield return new ValidationResult("The expiration date must be MMYY format", new[] { nameof(ExpirationMonthYear) });
                    }
                }
            }
        }
    }
}
