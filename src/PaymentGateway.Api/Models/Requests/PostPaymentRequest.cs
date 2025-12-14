using System.ComponentModel.DataAnnotations;

namespace PaymentGateway.Api.Models.Requests;

public class PostPaymentRequest
{
    [Required]
    public string CardNumber { get; set; } = string.Empty;

    [Required]
    [Range(1, 12)]
    public int ExpiryMonth { get; set; }

    [Required]
    public int ExpiryYear { get; set; }

    [Required]
    [StringLength(3, MinimumLength = 3)]
    public string Currency { get; set; } = string.Empty;

    [Required]
    public int Amount { get; set; }

    [Required]
    [StringLength(4, MinimumLength = 3)]
    public string Cvv { get; set; } = string.Empty;
}