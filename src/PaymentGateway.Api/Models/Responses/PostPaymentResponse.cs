namespace PaymentGateway.Api.Models.Responses;

public class PostPaymentResponse
{
    public Guid Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public string CardNumberLastFour { get; set; } = string.Empty;
    public int ExpiryMonth { get; set; }
    public int ExpiryYear { get; set; }
    public string Currency { get; set; }
    public int Amount { get; set; }
}
