using PaymentGateway.Api.Helpers;
using PaymentGateway.Api.Models;
using PaymentGateway.Api.Models.Requests;

namespace PaymentGateway.Api.Tests;

public class PaymentValidatorTests
{

    [Fact]
    public void Validate_ValidPaymentRequest_ReturnsSuccess()
    {
        // Arrange
        var request = TestHelpers.CreateValidPostPaymentRequest();

        // Act
        var result = PaymentValidator.Validate(request);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_CardNumber_Empty_ReturnsError()
    {
        // Arrange
        var request = TestHelpers.CreateValidPostPaymentRequest();
        request.CardNumber = string.Empty;

        // Act
        var result = PaymentValidator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Card number is required", result.Errors);
    }

    [Fact]
    public void Validate_CardNumber_TooShort_ReturnsError()
    {
        // Arrange
        var request = TestHelpers.CreateValidPostPaymentRequest();
        request.CardNumber = "1234567890123"; // 13 digits

        // Act
        var result = PaymentValidator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Card number must be between 14 and 19 characters long", result.Errors);
    }

    [Fact]
    public void Validate_CardNumber_TooLong_ReturnsError()
    {
        // Arrange
        var request = TestHelpers.CreateValidPostPaymentRequest();
        request.CardNumber = "12345678901234567890"; // 20 digits

        // Act
        var result = PaymentValidator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Card number must be between 14 and 19 characters long", result.Errors);
    }

    [Fact]
    public void Validate_CardNumber_Exactly14Digits_ReturnsSuccess()
    {
        // Arrange
        var request = TestHelpers.CreateValidPostPaymentRequest();
        request.CardNumber = "12345678901234"; // 14 digits

        // Act
        var result = PaymentValidator.Validate(request);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_CardNumber_Exactly19Digits_ReturnsSuccess()
    {
        // Arrange
        var request = TestHelpers.CreateValidPostPaymentRequest();
        request.CardNumber = "1234567890123456789"; // 19 digits

        // Act
        var result = PaymentValidator.Validate(request);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_CardNumber_ContainsLetters_ReturnsError()
    {
        // Arrange
        var request = TestHelpers.CreateValidPostPaymentRequest();
        request.CardNumber = "12345678901234AB";

        // Act
        var result = PaymentValidator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Card number must only contain numeric characters", result.Errors);
    }

    [Fact]
    public void Validate_ExpiryMonth_LessThan1_ReturnsError()
    {
        // Arrange
        var request = TestHelpers.CreateValidPostPaymentRequest();
        request.ExpiryMonth = 0;

        // Act
        var result = PaymentValidator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Expiry month must be between 1 and 12", result.Errors);
    }

    [Fact]
    public void Validate_ExpiryMonth_GreaterThan12_ReturnsError()
    {
        // Arrange
        var request = TestHelpers.CreateValidPostPaymentRequest();
        request.ExpiryMonth = 13;

        // Act
        var result = PaymentValidator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Expiry month must be between 1 and 12", result.Errors);
    }

    [Fact]
    public void Validate_ExpiryYear_InPast_ReturnsError()
    {
        // Arrange
        var request = TestHelpers.CreateValidPostPaymentRequest();
        request.ExpiryYear = DateTime.UtcNow.Year - 1;
        request.ExpiryMonth = 12;

        // Act
        var result = PaymentValidator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Expiry year must be in the future", result.Errors);
    }

    [Fact]
    public void Validate_ExpiryDate_CurrentMonthButPast_ReturnsError()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var request = TestHelpers.CreateValidPostPaymentRequest();
        request.ExpiryYear = now.Year;
        request.ExpiryMonth = now.Month - 1; // Last month

        // Act
        var result = PaymentValidator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Expiry date must be in the future", result.Errors);
    }

    [Fact]
    public void Validate_ExpiryDate_CurrentMonth_ReturnsSuccess()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var request = TestHelpers.CreateValidPostPaymentRequest();
        request.ExpiryYear = now.Year;
        request.ExpiryMonth = now.Month; // Current month is valid

        // Act
        var result = PaymentValidator.Validate(request);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_ExpiryDate_FutureMonth_ReturnsSuccess()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var request = TestHelpers.CreateValidPostPaymentRequest();

        // Handle December edge case - if current month is 12, next month is next year
        if (now.Month == 12)
        {
            request.ExpiryYear = now.Year + 1;
            request.ExpiryMonth = 1; // January next year
        }
        else
        {
            request.ExpiryYear = now.Year;
            request.ExpiryMonth = now.Month + 1; // Next month
        }

        // Act
        var result = PaymentValidator.Validate(request);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_Currency_Empty_ReturnsError()
    {
        // Arrange
        var request = TestHelpers.CreateValidPostPaymentRequest();
        request.Currency = string.Empty;

        // Act
        var result = PaymentValidator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Currency is required", result.Errors);
    }

    [Fact]
    public void Validate_Currency_TooShort_ReturnsError()
    {
        // Arrange
        var request = TestHelpers.CreateValidPostPaymentRequest();
        request.Currency = "GB";

        // Act
        var result = PaymentValidator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Currency must be exactly 3 characters", result.Errors);
    }

    [Fact]
    public void Validate_Currency_TooLong_ReturnsError()
    {
        // Arrange
        var request = TestHelpers.CreateValidPostPaymentRequest();
        request.Currency = "GBPP";

        // Act
        var result = PaymentValidator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Currency must be exactly 3 characters", result.Errors);
    }

    [Fact]
    public void Validate_Currency_Unsupported_ReturnsError()
    {
        // Arrange
        var request = TestHelpers.CreateValidPostPaymentRequest();
        request.Currency = "JPY";

        // Act
        var result = PaymentValidator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Currency must be one of: GBP, EUR, USD", result.Errors);
    }

    [Fact]
    public void Validate_Currency_GBP_ReturnsSuccess()
    {
        // Arrange
        var request = TestHelpers.CreateValidPostPaymentRequest();
        request.Currency = "GBP";

        // Act
        var result = PaymentValidator.Validate(request);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_Currency_EUR_ReturnsSuccess()
    {
        // Arrange
        var request = TestHelpers.CreateValidPostPaymentRequest();
        request.Currency = "EUR";

        // Act
        var result = PaymentValidator.Validate(request);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_Currency_USD_ReturnsSuccess()
    {
        // Arrange
        var request = TestHelpers.CreateValidPostPaymentRequest();
        request.Currency = "USD";

        // Act
        var result = PaymentValidator.Validate(request);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_Currency_CaseInsensitive_ReturnsSuccess()
    {
        // Arrange
        var request = TestHelpers.CreateValidPostPaymentRequest();
        request.Currency = "gbp";

        // Act
        var result = PaymentValidator.Validate(request);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_Amount_Zero_ReturnsError()
    {
        // Arrange
        var request = TestHelpers.CreateValidPostPaymentRequest();
        request.Amount = 0;

        // Act
        var result = PaymentValidator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Amount must be at greater than 0", result.Errors);
    }

    [Fact]
    public void Validate_Amount_Negative_ReturnsError()
    {
        // Arrange
        var request = TestHelpers.CreateValidPostPaymentRequest();
        request.Amount = -100;

        // Act
        var result = PaymentValidator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Amount must be at greater than 0", result.Errors);
    }

    [Fact]
    public void Validate_Amount_Minimum_ReturnsSuccess()
    {
        // Arrange
        var request = TestHelpers.CreateValidPostPaymentRequest();
        request.Amount = 1;

        // Act
        var result = PaymentValidator.Validate(request);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_Cvv_Empty_ReturnsError()
    {
        // Arrange
        var request = TestHelpers.CreateValidPostPaymentRequest();
        request.Cvv = string.Empty;

        // Act
        var result = PaymentValidator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("CVV is required", result.Errors);
    }

    [Fact]
    public void Validate_Cvv_TooShort_ReturnsError()
    {
        // Arrange
        var request = TestHelpers.CreateValidPostPaymentRequest();
        request.Cvv = "12"; // 2 digits

        // Act
        var result = PaymentValidator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("CVV must be between 3 and 4 characters long", result.Errors);
    }

    [Fact]
    public void Validate_Cvv_TooLong_ReturnsError()
    {
        // Arrange
        var request = TestHelpers.CreateValidPostPaymentRequest();
        request.Cvv = "12345"; // 5 digits

        // Act
        var result = PaymentValidator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("CVV must be between 3 and 4 characters long", result.Errors);
    }

    [Fact]
    public void Validate_Cvv_ThreeDigits_ReturnsSuccess()
    {
        // Arrange
        var request = TestHelpers.CreateValidPostPaymentRequest();
        request.Cvv = "123";

        // Act
        var result = PaymentValidator.Validate(request);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_Cvv_FourDigits_ReturnsSuccess()
    {
        // Arrange
        var request = TestHelpers.CreateValidPostPaymentRequest();
        request.Cvv = "1234";

        // Act
        var result = PaymentValidator.Validate(request);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_Cvv_ContainsLetters_ReturnsError()
    {
        // Arrange
        var request = TestHelpers.CreateValidPostPaymentRequest();
        request.Cvv = "12A";

        // Act
        var result = PaymentValidator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("CVV must only contain numeric characters", result.Errors);
    }

    [Fact]
    public void Validate_MultipleErrors_ReturnsAllErrors()
    {
        // Arrange
        var request = new PostPaymentRequest
        {
            CardNumber = "123", // Too short
            ExpiryMonth = 0, // Invalid
            ExpiryYear = 2020, // Past
            Currency = "JPY", // Unsupported
            Amount = -100, // Negative
            Cvv = "AB" // Invalid
        };

        // Act
        var result = PaymentValidator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.True(result.Errors.Count >= 6); // Should have multiple errors
    }

}

