namespace PaymentGateway.Api.Models.Responses;

/// <summary>
/// Base response model for payment operations.
/// </summary>
public class PaymentResponse
{
    /// <summary>
    /// The unique identifier of the payment.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The payment status ("Authorized" or "Declined").
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// The last four digits of the card number (masked for security).
    /// </summary>
    public string CardNumberLastFour { get; set; } = string.Empty;

    /// <summary>
    /// The expiry month (1-12).
    /// </summary>
    public int ExpiryMonth { get; set; }

    /// <summary>
    /// The expiry year.
    /// </summary>
    public int ExpiryYear { get; set; }

    /// <summary>
    /// The currency code (3 characters, e.g., "GBP", "EUR", "USD").
    /// </summary>
    public string Currency { get; set; } = string.Empty;

    /// <summary>
    /// The payment amount in minor units (e.g., 1000 = £10.00 for GBP).
    /// </summary>
    public int Amount { get; set; }
}
