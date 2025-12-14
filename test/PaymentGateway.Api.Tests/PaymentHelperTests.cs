using PaymentGateway.Api.Helpers;

namespace PaymentGateway.Api.Tests;

public class PaymentHelperTests
{
    [Fact]
    public void MaskCardNumber_ValidCardNumber_ReturnsLastFourDigits()
    {
        // Arrange
        var cardNumber = "1234567890123456";

        // Act
        var result = PaymentHelper.MaskCardNumber(cardNumber);

        // Assert
        Assert.Equal("3456", result);
    }

    [Fact]
    public void MaskCardNumber_MinMaxLengthCards_ReturnsLastFourDigits()
    {
        // Arrange & Act & Assert
        Assert.Equal("1234", PaymentHelper.MaskCardNumber("12345678901234")); // 14 digits (min)
        Assert.Equal("6789", PaymentHelper.MaskCardNumber("1234567890123456789")); // 19 digits (max)
    }

    [Fact]
    public void MaskCardNumber_Exactly4Digits_ReturnsAllFourDigits()
    {
        // Arrange
        var cardNumber = "1234";

        // Act
        var result = PaymentHelper.MaskCardNumber(cardNumber);

        // Assert
        Assert.Equal("1234", result);
    }

    [Fact]
    public void MaskCardNumber_LastFourDigitsWithLeadingZeros_PreservesLeadingZeros()
    {
        // Arrange & Act & Assert
        Assert.Equal("0001", PaymentHelper.MaskCardNumber("1234567890120001")); // Last 4 are 0001
        Assert.Equal("0000", PaymentHelper.MaskCardNumber("1234567890120000")); // Last 4 are 0000
        Assert.Equal("0123", PaymentHelper.MaskCardNumber("1234567890120123")); // Last 4 are 0123
    }

    [Fact]
    public void MaskCardNumber_NullEmptyOrWhitespace_ReturnsEmptyString()
    {
        // Arrange & Act & Assert
        Assert.Equal(string.Empty, PaymentHelper.MaskCardNumber(null!));
        Assert.Equal(string.Empty, PaymentHelper.MaskCardNumber(string.Empty));
        Assert.Equal(string.Empty, PaymentHelper.MaskCardNumber("   "));
    }

    [Fact]
    public void MaskCardNumber_LessThan4Digits_ReturnsEmptyString()
    {
        // Arrange & Act & Assert
        Assert.Equal(string.Empty, PaymentHelper.MaskCardNumber("123")); // 3 digits
        Assert.Equal(string.Empty, PaymentHelper.MaskCardNumber("12")); // 2 digits
    }

    [Fact]
    public void MaskCardNumber_ContainsInvalidCharacters_ReturnsEmptyString()
    {
        // Arrange & Act & Assert
        Assert.Equal(string.Empty, PaymentHelper.MaskCardNumber("12345678901234AB")); // Contains letters
        Assert.Equal(string.Empty, PaymentHelper.MaskCardNumber("1234-5678-9012-3456")); // Contains hyphens
        Assert.Equal(string.Empty, PaymentHelper.MaskCardNumber("1234 5678 9012 3456")); // Contains spaces
    }
}

