using System.Text.Json.Serialization;

namespace PaymentGateway.Api.Models.Bank;

/// <summary>
/// Response model from the bank simulator API.
/// </summary>
public class BankPaymentResponse
{
    [JsonPropertyName("authorized")]
    public bool Authorized { get; set; }

    [JsonPropertyName("authorization_code")]
    public string AuthorizationCode { get; set; } = string.Empty;
}

