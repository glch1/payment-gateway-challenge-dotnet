namespace PaymentGateway.Api.Constants;

public static class SupportedCurrencies
{
    public const string GBP = "GBP";
    public const string EUR = "EUR";
    public const string USD = "USD";

    public static readonly HashSet<string> ALL = new()
    {
        GBP,
        EUR,
        USD
    };
}

