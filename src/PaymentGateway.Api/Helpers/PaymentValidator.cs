using PaymentGateway.Api.Constants;
using PaymentGateway.Api.Models;
using PaymentGateway.Api.Models.Requests;

namespace PaymentGateway.Api.Helpers;

public static class PaymentValidator
{
    public static ValidationResult Validate(PostPaymentRequest request)
    {
        var errors = new List<string>();

        ValidateCardNumber(request.CardNumber, errors);
        ValidateExpiryMonth(request.ExpiryMonth, errors);
        ValidateExpiryYear(request.ExpiryYear, request.ExpiryMonth, errors);
        ValidateCurrency(request.Currency, errors);
        ValidateAmount(request.Amount, errors);
        ValidateCvv(request.Cvv, errors);

        return errors.Count == 0
            ? ValidationResult.Success()
            : ValidationResult.Failure(errors);
    }

    /// <summary>
    /// Validates the card number. Checks that it is not empty, is between 14-19 characters long, and contains only numeric characters.
    /// </summary>
    /// <param name="cardNumber">The card number to validate</param>
    /// <param name="errors">The list to add validation errors to</param>
    private static void ValidateCardNumber(string cardNumber, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(cardNumber))
        {
            errors.Add("Card number is required");
            return;
        }

        if (cardNumber.Length < CardDetailsConstants.CardNumberMinLength ||
            cardNumber.Length > CardDetailsConstants.CardNumberMaxLength)
        {
            errors.Add($"Card number must be between {CardDetailsConstants.CardNumberMinLength} and {CardDetailsConstants.CardNumberMaxLength} characters long");
        }

        if (!cardNumber.All(char.IsDigit))
        {
            errors.Add("Card number must only contain numeric characters");
        }
    }

    /// <summary>
    /// Validates the expiry month. Checks that it is between 1 and 12.
    /// </summary>
    /// <param name="expiryMonth">The expiry month to validate</param>
    /// <param name="errors">The list to add validation errors to</param>
    private static void ValidateExpiryMonth(int expiryMonth, List<string> errors)
    {
        if (expiryMonth < CardDetailsConstants.ExpiryMonthMin ||
            expiryMonth > CardDetailsConstants.ExpiryMonthMax)
        {
            errors.Add($"Expiry month must be between {CardDetailsConstants.ExpiryMonthMin} and {CardDetailsConstants.ExpiryMonthMax}");
        }
    }

    /// <summary>
    /// Validates the expiry year and month combination. Checks that the expiry date is in the future.
    /// A card expiring in the current month is considered valid (expires at end of month).
    /// </summary>
    /// <param name="expiryYear">The expiry year to validate</param>
    /// <param name="expiryMonth">The expiry month to validate (used for month+year combination check)</param>
    /// <param name="errors">The list to add validation errors to</param>
    private static void ValidateExpiryYear(int expiryYear, int expiryMonth, List<string> errors)
    {
        var now = DateTime.UtcNow;
        var currentYear = now.Year;
        var currentMonth = now.Month;

        if (expiryYear < currentYear)
        {
            errors.Add("Expiry year must be in the future");
            return;
        }

        if (expiryYear == currentYear && expiryMonth < currentMonth)
        {
            errors.Add("Expiry date must be in the future");
        }
    }

    /// <summary>
    /// Validates the currency. Checks that it is not empty, is exactly 3 characters, and is one of the supported currencies (GBP, EUR, USD).
    /// </summary>
    /// <param name="currency">The currency code to validate</param>
    /// <param name="errors">The list to add validation errors to</param>
    private static void ValidateCurrency(string currency, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(currency))
        {
            errors.Add("Currency is required");
            return;
        }

        if (currency.Length != CardDetailsConstants.CurrencyLength)
        {
            errors.Add($"Currency must be exactly {CardDetailsConstants.CurrencyLength} characters");
            return;
        }

        if (!SupportedCurrencies.ALL.Contains(currency.ToUpperInvariant()))
        {
            errors.Add($"Currency must be one of: {string.Join(", ", SupportedCurrencies.ALL)}");
        }
    }

    /// <summary>
    /// Validates the payment amount. Checks that it is at least 1 minor currency unit (e.g., 1 cent, 1 penny).
    /// </summary>
    /// <param name="amount">The amount to validate (in minor currency units)</param>
    /// <param name="errors">The list to add validation errors to</param>
    private static void ValidateAmount(int amount, List<string> errors)
    {
        if (amount < CardDetailsConstants.AmountMinimum)
        {
            errors.Add($"Amount must be at least {CardDetailsConstants.AmountMinimum} minor currency unit");
        }
    }

    /// <summary>
    /// Validates the CVV (Card Verification Value). Checks that it is not empty, is between 3-4 characters long, and contains only numeric characters.
    /// </summary>
    /// <param name="cvv">The CVV to validate</param>
    /// <param name="errors">The list to add validation errors to</param>
    private static void ValidateCvv(string cvv, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(cvv))
        {
            errors.Add("CVV is required");
            return;
        }

        if (cvv.Length < CardDetailsConstants.CvvMinLength ||
            cvv.Length > CardDetailsConstants.CvvMaxLength)
        {
            errors.Add($"CVV must be between {CardDetailsConstants.CvvMinLength} and {CardDetailsConstants.CvvMaxLength} characters long");
        }

        if (!cvv.All(char.IsDigit))
        {
            errors.Add("CVV must only contain numeric characters");
        }
    }
}
