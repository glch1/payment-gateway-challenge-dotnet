using PaymentGateway.Api.Models.Bank;
using PaymentGateway.Api.Models.Requests;

namespace PaymentGateway.Api.Tests;

public static class TestHelpers
{
    public static PostPaymentRequest CreateValidPostPaymentRequest(bool shouldAuthorize = false)
    {
        // Odd ending (1,3,5,7,9) = Authorized, Even ending (2,4,6,8) = Declined
        var lastDigit = shouldAuthorize ? "7" : "6";

        return new PostPaymentRequest
        {
            CardNumber = $"123456789012345{lastDigit}",
            ExpiryMonth = 12,
            ExpiryYear = DateTime.UtcNow.Year + 1,
            Currency = "GBP",
            Amount = 1000,
            Cvv = "123"
        };
    }

    public static BankPaymentRequest CreateValidBankPaymentRequest()
    {
        return new BankPaymentRequest
        {
            CardNumber = "1234567890123456",
            ExpiryDate = $"{12:D2}/{DateTime.UtcNow.Year + 1}",
            Currency = "GBP",
            Amount = 1000,
            Cvv = "123"
        };
    }
}
