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
    /// <returns>The last 4 digits as an integer</returns>
    public static int MaskCardNumber(string cardNumber)
    {
        if (string.IsNullOrWhiteSpace(cardNumber) || cardNumber.Length < 4)
        {
            return 0;
        }

        var lastFour = cardNumber.Substring(cardNumber.Length - 4);
        return int.Parse(lastFour);
    }
}

