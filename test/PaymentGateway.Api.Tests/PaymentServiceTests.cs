using Microsoft.Extensions.Logging;
using Moq;
using PaymentGateway.Api.Exceptions;
using PaymentGateway.Api.Interfaces;
using PaymentGateway.Api.Models;
using PaymentGateway.Api.Models.Bank;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Services;
using System.Net;

namespace PaymentGateway.Api.Tests;

public class PaymentServiceTests
{
    private readonly Mock<IBankClient> _bankClientMock;
    private readonly Mock<IPaymentsRepository> _repositoryMock;
    private readonly Mock<ILogger<PaymentService>> _loggerMock;
    private readonly PaymentService _paymentService;

    public PaymentServiceTests()
    {
        _bankClientMock = new Mock<IBankClient>();
        _repositoryMock = new Mock<IPaymentsRepository>();
        _loggerMock = new Mock<ILogger<PaymentService>>();
        _paymentService = new PaymentService(_bankClientMock.Object, _repositoryMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task ProcessPaymentAsync_InvalidRequest_ThrowsValidationException_DoesNotCallBank_DoesNotStore()
    {
        // Arrange
        var request = new PostPaymentRequest
        {
            CardNumber = "123", // Invalid - too short
            ExpiryMonth = 12,
            ExpiryYear = 2025,
            Currency = "GBP",
            Amount = 1000,
            Cvv = "123"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(() => _paymentService.ProcessPaymentAsync(request));

        Assert.NotEmpty(exception.Errors);
        Assert.Contains("Card number must be between 14 and 19 characters long", exception.Errors);

        // Verify bank and repository were not called
        _repositoryMock.Verify(x => x.AddAsync(It.IsAny<PostPaymentResponse>()), Times.Never);
        _bankClientMock.Verify(x => x.ProcessPaymentAsync(It.IsAny<BankPaymentRequest>()), Times.Never);
    }

    [Fact]
    public async Task ProcessPaymentAsync_ValidRequest_Authorized_StoresPayment()
    {
        // Arrange
        var request = CreateValidRequest();
        var bankResponse = new BankPaymentResponse
        {
            Authorized = true,
            AuthorizationCode = "AUTH123"
        };

        _bankClientMock
            .Setup(x => x.ProcessPaymentAsync(It.IsAny<BankPaymentRequest>()))
            .ReturnsAsync(bankResponse);

        // Act
        var result = await _paymentService.ProcessPaymentAsync(request);

        // Assert
        Assert.Equal(PaymentStatus.Authorized, result.Status);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("3456", result.CardNumberLastFour);
        Assert.Equal(request.ExpiryMonth, result.ExpiryMonth);
        Assert.Equal(request.ExpiryYear, result.ExpiryYear);
        Assert.Equal(request.Currency, result.Currency);
        Assert.Equal(request.Amount, result.Amount);

        // Verify stored
        _repositoryMock.Verify(x => x.AddAsync(
            It.Is<PostPaymentResponse>(p =>
                p.Id == result.Id &&
                p.Status == PaymentStatus.Authorized)),
            Times.Once);
    }

    [Fact]
    public async Task ProcessPaymentAsync_ValidRequest_Declined_StoresPayment()
    {
        // Arrange
        var request = CreateValidRequest();
        var bankResponse = new BankPaymentResponse
        {
            Authorized = false,
            AuthorizationCode = string.Empty
        };

        _bankClientMock
            .Setup(x => x.ProcessPaymentAsync(It.IsAny<BankPaymentRequest>()))
            .ReturnsAsync(bankResponse);

        // Act
        var result = await _paymentService.ProcessPaymentAsync(request);

        // Assert
        Assert.Equal(PaymentStatus.Declined, result.Status);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("3456", result.CardNumberLastFour);

        // Verify stored
        _repositoryMock.Verify(x => x.AddAsync(
            It.Is<PostPaymentResponse>(p =>
                p.Id == result.Id &&
                p.Status == PaymentStatus.Declined)),
            Times.Once);
    }

    [Fact]
    public async Task ProcessPaymentAsync_ValidRequest_MapsToBankRequestCorrectly()
    {
        // Arrange
        var request = new PostPaymentRequest
        {
            CardNumber = "1234567890123456",
            ExpiryMonth = 5,
            ExpiryYear = DateTime.UtcNow.Year + 1, // Ensure future date
            Currency = "GBP",
            Amount = 1000,
            Cvv = "123"
        };

        var bankResponse = new BankPaymentResponse
        {
            Authorized = true,
            AuthorizationCode = "AUTH123"
        };

        _bankClientMock
            .Setup(x => x.ProcessPaymentAsync(It.IsAny<BankPaymentRequest>()))
            .ReturnsAsync(bankResponse);

        // Act
        await _paymentService.ProcessPaymentAsync(request);

        // Assert
        var expectedExpiryDate = $"{request.ExpiryMonth:D2}/{request.ExpiryYear}";
        _bankClientMock.Verify(x => x.ProcessPaymentAsync(
            It.Is<BankPaymentRequest>(r =>
                r.CardNumber == request.CardNumber &&
                r.ExpiryDate == expectedExpiryDate &&
                r.Currency == request.Currency &&
                r.Amount == request.Amount &&
                r.Cvv == request.Cvv)),
            Times.Once);
    }

    [Fact]
    public async Task ProcessPaymentAsync_BankServiceUnavailable_ThrowsException_DoesNotStore()
    {
        // Arrange
        var request = CreateValidRequest();

        _bankClientMock
            .Setup(x => x.ProcessPaymentAsync(It.IsAny<BankPaymentRequest>()))
            .ThrowsAsync(new HttpRequestException("Bank service is unavailable", null, HttpStatusCode.ServiceUnavailable));

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => _paymentService.ProcessPaymentAsync(request));

        // Verify not stored
        _repositoryMock.Verify(x => x.AddAsync(It.IsAny<PostPaymentResponse>()), Times.Never);
    }

    [Fact]
    public async Task ProcessPaymentAsync_BankTimeout_ThrowsException_DoesNotStore()
    {
        // Arrange
        var request = CreateValidRequest();

        _bankClientMock
            .Setup(x => x.ProcessPaymentAsync(It.IsAny<BankPaymentRequest>()))
            .ThrowsAsync(new HttpRequestException("Bank request timed out", null, HttpStatusCode.RequestTimeout));

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => _paymentService.ProcessPaymentAsync(request));

        // Verify not stored
        _repositoryMock.Verify(x => x.AddAsync(It.IsAny<PostPaymentResponse>()), Times.Never);
    }

    [Fact]
    public async Task ProcessPaymentAsync_MasksCardNumber_Correctly()
    {
        // Arrange
        var request = CreateValidRequest();
        request.CardNumber = "1234567890123456";

        var bankResponse = new BankPaymentResponse
        {
            Authorized = true,
            AuthorizationCode = "AUTH123"
        };

        _bankClientMock
            .Setup(x => x.ProcessPaymentAsync(It.IsAny<BankPaymentRequest>()))
            .ReturnsAsync(bankResponse);

        // Act
        var result = await _paymentService.ProcessPaymentAsync(request);

        // Assert
        Assert.Equal("3456", result.CardNumberLastFour);
    }

    [Fact]
    public async Task ProcessPaymentAsync_ValidCardNumber_MasksCorrectly()
    {
        // Arrange
        var request = CreateValidRequest();
        request.CardNumber = "1234567890123456"; // Valid 16-digit card

        var bankResponse = new BankPaymentResponse
        {
            Authorized = true,
            AuthorizationCode = "AUTH123"
        };

        _bankClientMock
            .Setup(x => x.ProcessPaymentAsync(It.IsAny<BankPaymentRequest>()))
            .ReturnsAsync(bankResponse);

        // Act
        var result = await _paymentService.ProcessPaymentAsync(request);

        // Assert
        Assert.Equal("3456", result.CardNumberLastFour); // Last 4 digits of 1234567890123456
    }

    [Fact]
    public async Task ProcessPaymentAsync_InvalidExpiryMonth_ThrowsValidationException_DoesNotStore()
    {
        // Arrange
        var request = new PostPaymentRequest
        {
            CardNumber = "1234567890123456",
            ExpiryMonth = 13, // Invalid month
            ExpiryYear = 2025,
            Currency = "GBP",
            Amount = 1000,
            Cvv = "123"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(() => _paymentService.ProcessPaymentAsync(request));

        Assert.NotEmpty(exception.Errors);
        Assert.Contains("Expiry month must be between 1 and 12", exception.Errors);

        _repositoryMock.Verify(x => x.AddAsync(It.IsAny<PostPaymentResponse>()), Times.Never);
        _bankClientMock.Verify(x => x.ProcessPaymentAsync(It.IsAny<BankPaymentRequest>()), Times.Never);
    }

    private static PostPaymentRequest CreateValidRequest()
    {
        return new PostPaymentRequest
        {
            CardNumber = "1234567890123456",
            ExpiryMonth = 12,
            ExpiryYear = 2025,
            Currency = "GBP",
            Amount = 1000,
            Cvv = "123"
        };
    }
}

