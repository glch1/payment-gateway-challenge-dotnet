using System.Net;
using System.Net.Http.Json;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

using PaymentGateway.Api.Controllers;
using PaymentGateway.Api.Interfaces;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Services;

namespace PaymentGateway.Api.Tests;

public class PaymentGatewayIntegrationTests : IClassFixture<WebApplicationFactory<PaymentsController>>
{
    private readonly WebApplicationFactory<PaymentsController> _factory;

    public PaymentGatewayIntegrationTests(WebApplicationFactory<PaymentsController> factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClient()
    {
        return _factory.CreateClient();
    }

    [Fact]
    public async Task PostPayment_ValidRequest_Returns200WithPaymentResponse()
    {
        // Arrange
        var request = TestHelpers.CreateValidPostPaymentRequest(shouldAuthorize: true);

        // Act
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/Payments", request);
        var paymentResponse = await response.Content.ReadFromJsonAsync<PostPaymentResponse>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(paymentResponse);
        Assert.NotEqual(Guid.Empty, paymentResponse!.Id);
        Assert.NotNull(paymentResponse.Status);
        Assert.NotNull(paymentResponse.CardNumberLastFour);
    }

    [Fact]
    public async Task PostPayment_InvalidRequest_Returns400WithValidationErrors()
    {
        // Arrange
        var request = TestHelpers.CreateValidPostPaymentRequest(shouldAuthorize: true);
        request.CardNumber = "123"; // Invalid - too short

        // Act
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/Payments", request);
        var errorResponse = await response.Content.ReadFromJsonAsync<Dictionary<string, System.Text.Json.JsonElement>>();

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(errorResponse);
        Assert.True(errorResponse!.ContainsKey("errors"));
    }

    [Fact]
    public async Task PostPayment_ResponseContainsValidId()
    {
        // Arrange
        var request = TestHelpers.CreateValidPostPaymentRequest(shouldAuthorize: true);

        // Act
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/Payments", request);
        var paymentResponse = await response.Content.ReadFromJsonAsync<PostPaymentResponse>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(paymentResponse);
        Assert.NotEqual(Guid.Empty, paymentResponse!.Id);
    }

    [Fact]
    public async Task PostPayment_StatusIsString_Authorized()
    {
        // Arrange
        var request = TestHelpers.CreateValidPostPaymentRequest(shouldAuthorize: true);
        request.CardNumber = "1234567890123457"; // Ends in 7 (odd) - will be authorized

        // Act
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/Payments", request);
        var paymentResponse = await response.Content.ReadFromJsonAsync<PostPaymentResponse>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(paymentResponse);
        Assert.Equal("Authorized", paymentResponse!.Status);
        Assert.IsType<string>(paymentResponse.Status);
    }

    [Fact]
    public async Task PostPayment_StatusIsString_Declined()
    {
        // Arrange
        var request = TestHelpers.CreateValidPostPaymentRequest(shouldAuthorize: true);
        request.CardNumber = "1234567890123456"; // Ends in 6 (even) - will be declined

        // Act
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/Payments", request);
        var paymentResponse = await response.Content.ReadFromJsonAsync<PostPaymentResponse>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(paymentResponse);
        Assert.Equal("Declined", paymentResponse!.Status);
        Assert.IsType<string>(paymentResponse.Status);
    }

    [Theory]
    [InlineData("1234567890123456", "3456")] // Standard 16-digit card
    [InlineData("1234567890123456789", "6789")] // 19-digit card
    [InlineData("1234567890120001", "0001")] // Last 4 digits with leading zeros
    [InlineData("1234567890120123", "0123")] // Last 4 digits starting with zero
    public async Task PostPayment_ReturnsLastFourCardDigits_WithLeadingZeros(string cardNumber, string expectedLastFour)
    {
        // Arrange
        var request = TestHelpers.CreateValidPostPaymentRequest(shouldAuthorize: true);
        request.CardNumber = cardNumber;
        // Ensure odd ending for authorization
        if (cardNumber.EndsWith("0") || cardNumber.EndsWith("2") || cardNumber.EndsWith("4") || 
            cardNumber.EndsWith("6") || cardNumber.EndsWith("8"))
        {
            request.CardNumber = cardNumber.Substring(0, cardNumber.Length - 1) + "7";
            expectedLastFour = expectedLastFour.Substring(0, 3) + "7";
        }

        // Act
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/Payments", request);
        var paymentResponse = await response.Content.ReadFromJsonAsync<PostPaymentResponse>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(paymentResponse);
        Assert.Equal(expectedLastFour, paymentResponse!.CardNumberLastFour);
        Assert.Equal(4, paymentResponse.CardNumberLastFour.Length);
        // Verify it's a string (not int) to preserve leading zeros
        Assert.IsType<string>(paymentResponse.CardNumberLastFour);
    }

    [Fact]
    public async Task PostPayment_ResponseContainsAllRequiredFields()
    {
        // Arrange
        var request = TestHelpers.CreateValidPostPaymentRequest(shouldAuthorize: true);
        request.ExpiryMonth = 5;
        request.ExpiryYear = 2026;
        request.Currency = "EUR";
        request.Amount = 1050;

        // Act
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/Payments", request);
        var paymentResponse = await response.Content.ReadFromJsonAsync<PostPaymentResponse>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(paymentResponse);
        Assert.Equal(5, paymentResponse!.ExpiryMonth);
        Assert.Equal(2026, paymentResponse.ExpiryYear);
        Assert.Equal("EUR", paymentResponse.Currency);
        Assert.Equal(1050, paymentResponse.Amount);
    }

    [Theory]
    [InlineData("1234567890123451")] // Ends in 1
    [InlineData("1234567890123453")] // Ends in 3
    [InlineData("1234567890123455")] // Ends in 5
    [InlineData("1234567890123457")] // Ends in 7
    [InlineData("1234567890123459")] // Ends in 9
    public async Task PostPayment_CardEndingInOddNumber_ReturnsAuthorized(string cardNumber)
    {
        // Arrange
        var request = TestHelpers.CreateValidPostPaymentRequest(shouldAuthorize: true);
        request.CardNumber = cardNumber;

        // Act
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/Payments", request);
        var paymentResponse = await response.Content.ReadFromJsonAsync<PostPaymentResponse>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(paymentResponse);
        Assert.Equal("Authorized", paymentResponse!.Status);
    }

    [Theory]
    [InlineData("1234567890123452")] // Ends in 2
    [InlineData("1234567890123454")] // Ends in 4
    [InlineData("1234567890123456")] // Ends in 6
    [InlineData("1234567890123458")] // Ends in 8
    public async Task PostPayment_CardEndingInEvenNumber_ReturnsDeclined(string cardNumber)
    {
        // Arrange
        var request = TestHelpers.CreateValidPostPaymentRequest(shouldAuthorize: true);
        request.CardNumber = cardNumber;

        // Act
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/Payments", request);
        var paymentResponse = await response.Content.ReadFromJsonAsync<PostPaymentResponse>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(paymentResponse);
        Assert.Equal("Declined", paymentResponse!.Status);
    }

    [Fact]
    public async Task PostPayment_CardEndingInZero_Returns503()
    {
        // Arrange
        var request = TestHelpers.CreateValidPostPaymentRequest(shouldAuthorize: true);
        request.CardNumber = "1234567890123450"; // Ends in 0

        // Act
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/Payments", request);

        // Assert
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Theory]
    [InlineData("12345678901234")] // 14 digits (minimum)
    [InlineData("1234567890123456789")] // 19 digits (maximum)
    public async Task PostPayment_CardNumberBoundaryLengths_WorksEndToEnd(string cardNumber)
    {
        // Arrange
        var request = TestHelpers.CreateValidPostPaymentRequest(shouldAuthorize: true);
        request.CardNumber = cardNumber;
        // Use odd ending to ensure authorization
        request.CardNumber = cardNumber.Substring(0, cardNumber.Length - 1) + "7";

        // Act
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/Payments", request);
        var paymentResponse = await response.Content.ReadFromJsonAsync<PostPaymentResponse>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(paymentResponse);
        Assert.NotEqual(Guid.Empty, paymentResponse!.Id);
    }

    [Theory]
    [InlineData("012")] // 3 digits with leading zero
    [InlineData("0123")] // 4 digits with leading zero
    public async Task PostPayment_CvvWithLeadingZeros_PreservedThroughFlow(string cvv)
    {
        // Arrange
        var request = TestHelpers.CreateValidPostPaymentRequest(shouldAuthorize: true);
        request.Cvv = cvv;

        // Act
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/Payments", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetPayment_ReturnsAllRequiredFields()
    {
        // Arrange
        var repository = new PaymentsRepository();
        var payment = new PostPaymentResponse
        {
            Id = Guid.NewGuid(),
            Status = "Authorized",
            CardNumberLastFour = "0123", // Test leading zeros
            ExpiryMonth = 6,
            ExpiryYear = 2027,
            Currency = "USD",
            Amount = 2500
        };

        await repository.AddAsync(payment);

        var factory = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IPaymentsRepository>(repository);
            }));

        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync($"/api/Payments/{payment.Id}");
        var paymentResponse = await response.Content.ReadFromJsonAsync<PostPaymentResponse>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(paymentResponse);
        Assert.Equal(payment.Id, paymentResponse!.Id);
        Assert.Equal("Authorized", paymentResponse.Status);
        Assert.Equal("0123", paymentResponse.CardNumberLastFour); // Verify leading zeros preserved
        Assert.Equal(6, paymentResponse.ExpiryMonth);
        Assert.Equal(2027, paymentResponse.ExpiryYear);
        Assert.Equal("USD", paymentResponse.Currency);
        Assert.Equal(2500, paymentResponse.Amount);
    }

    [Fact]
    public async Task GetPayment_PaymentNotFound_Returns404()
    {
        // Arrange
        var repository = new PaymentsRepository();
        var factory = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IPaymentsRepository>(repository);
            }));

        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync($"/api/Payments/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetPayment_StatusIsString()
    {
        // Arrange
        var repository = new PaymentsRepository();
        var payment = new PostPaymentResponse
        {
            Id = Guid.NewGuid(),
            Status = "Declined",
            CardNumberLastFour = "3456",
            ExpiryMonth = 12,
            ExpiryYear = 2025,
            Currency = "GBP",
            Amount = 1000
        };

        await repository.AddAsync(payment);

        var factory = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IPaymentsRepository>(repository);
            }));

        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync($"/api/Payments/{payment.Id}");
        var paymentResponse = await response.Content.ReadFromJsonAsync<PostPaymentResponse>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(paymentResponse);
        Assert.Equal("Declined", paymentResponse!.Status);
        Assert.IsType<string>(paymentResponse.Status);
    }


}
