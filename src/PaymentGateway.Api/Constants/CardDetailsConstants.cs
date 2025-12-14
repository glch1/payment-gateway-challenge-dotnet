namespace PaymentGateway.Api.Constants;

public static class CardDetailsConstants
{
    // Card Number
    public const int CardNumberMinLength = 14;
    public const int CardNumberMaxLength = 19;

    // Expiry Month
    public const int ExpiryMonthMin = 1;
    public const int ExpiryMonthMax = 12;

    // Currency
    public const int CurrencyLength = 3;

    // Amount
    public const int AmountMinimum = 1;

    // CVV
    public const int CvvMinLength = 3;
    public const int CvvMaxLength = 4;
}
