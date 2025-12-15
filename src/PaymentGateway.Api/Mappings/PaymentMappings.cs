using PaymentGateway.Api.Models.Bank;
using PaymentGateway.Api.Models.Requests;

namespace PaymentGateway.Api.Mappings;

/// <summary>
/// Static mapping methods for payment-related DTOs.
/// </summary>
public static class PaymentMappings
{
    /// <summary>
    /// Maps a PostPaymentRequest to a BankPaymentRequest.
    /// </summary>
    /// <param name="request">The API payment request</param>
    /// <returns>The bank payment request</returns>
    public static BankPaymentRequest MapToBankRequest(PostPaymentRequest request)
    {
        var expiryDate = $"{request.ExpiryMonth:D2}/{request.ExpiryYear}";

        return new BankPaymentRequest
        {
            CardNumber = request.CardNumber,
            ExpiryDate = expiryDate,
            Currency = request.Currency,
            Amount = request.Amount,
            Cvv = request.Cvv
        };
    }
}

