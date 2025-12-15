using System.ComponentModel.DataAnnotations;

using PaymentGateway.Api.Constants;

namespace PaymentGateway.Api.Models.Requests;

/// <summary>
/// Request model for processing a payment.
/// </summary>
public class PostPaymentRequest
{
    /// <summary>
    /// The card number (14-19 digits, numeric only).
    /// </summary>
    [Required]
    public string CardNumber { get; set; } = string.Empty;

    /// <summary>
    /// The expiry month (1-12).
    /// </summary>
    [Required]
    [Range(CardDetailsConstants.ExpiryMonthMin, CardDetailsConstants.ExpiryMonthMax)]
    public int ExpiryMonth { get; set; }

    /// <summary>
    /// The expiry year (must be in the future).
    /// </summary>
    [Required]
    public int ExpiryYear { get; set; }

    /// <summary>
    /// The currency code (3 characters, e.g., "GBP", "EUR", "USD").
    /// </summary>
    [Required]
    [StringLength(CardDetailsConstants.CurrencyLength, MinimumLength = CardDetailsConstants.CurrencyLength)]
    public string Currency { get; set; } = string.Empty;

    /// <summary>
    /// The payment amount in minor units (e.g., 1000 = £10.00 for GBP).
    /// </summary>
    [Required]
    public int Amount { get; set; }

    /// <summary>
    /// The card verification value (CVV) - 3 or 4 digits, numeric only.
    /// </summary>
    [Required]
    [StringLength(CardDetailsConstants.CvvMaxLength, MinimumLength = CardDetailsConstants.CvvMinLength)]
    public string Cvv { get; set; } = string.Empty;
}
