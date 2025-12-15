namespace PaymentGateway.Api.Helpers;

/// <summary>
/// Static helper methods for payment-related operations.
/// </summary>
public static class PaymentHelper
{
    /// <summary>
    /// Masks a card number by extracting the last 4 digits.
    /// </summary>
    /// <param name="cardNumber">The full card number</param>
    /// <returns>The last 4 digits as a string, preserving leading zeros. Returns empty string for invalid input.</returns>
    public static string MaskCardNumber(string cardNumber)
    {
        if (string.IsNullOrWhiteSpace(cardNumber) || cardNumber.Length < 4 || !cardNumber.All(char.IsDigit))
        {
            return string.Empty;
        }

        return cardNumber.Substring(cardNumber.Length - 4);
    }
}

