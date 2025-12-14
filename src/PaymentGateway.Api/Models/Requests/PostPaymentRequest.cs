using System.ComponentModel.DataAnnotations;

using PaymentGateway.Api.Constants;

namespace PaymentGateway.Api.Models.Requests;

public class PostPaymentRequest
{
    [Required]
    public string CardNumber { get; set; } = string.Empty;

    [Required]
    [Range(CardDetailsConstants.ExpiryMonthMin, CardDetailsConstants.ExpiryMonthMax)]
    public int ExpiryMonth { get; set; }

    [Required]
    public int ExpiryYear { get; set; }

    [Required]
    [StringLength(CardDetailsConstants.CurrencyLength, MinimumLength = CardDetailsConstants.CurrencyLength)]
    public string Currency { get; set; } = string.Empty;

    [Required]
    public int Amount { get; set; }

    [Required]
    [StringLength(CardDetailsConstants.CvvMaxLength, MinimumLength = CardDetailsConstants.CvvMinLength)]
    public string Cvv { get; set; } = string.Empty;
}
