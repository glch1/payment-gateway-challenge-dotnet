using System.Net;
using System.Net.Http.Json;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

using Moq;

using PaymentGateway.Api.Controllers;
using PaymentGateway.Api.Interfaces;
using PaymentGateway.Api.Models;
using PaymentGateway.Api.Models.Bank;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Services;

namespace PaymentGateway.Api.Tests;

public class PaymentsControllerTests
{
    [Fact]
    public async Task PostPayment_ValidRequest_Authorized_Returns200WithAuthorizedStatus()
    {
        // Arrange
        var bankClientMock = new Mock<IBankClient>();
        var repository = new PaymentsRepository();

        var bankResponse = new BankPaymentResponse
        {
            Authorized = true,
            AuthorizationCode = "AUTH123"
        };

        bankClientMock
            .Setup(x => x.ProcessPaymentAsync(It.IsAny<BankPaymentRequest>()))
            .ReturnsAsync(bankResponse);

        var factory = new WebApplicationFactory<PaymentsController>();
        var client = factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IPaymentsRepository>(repository);
                services.AddScoped<IPaymentService>(_ =>
                    new PaymentService(bankClientMock.Object, repository, Mock.Of<Microsoft.Extensions.Logging.ILogger<PaymentService>>()));
                services.AddScoped<IBankClient>(_ => bankClientMock.Object);
            }))
            .CreateClient();

        var request = new PostPaymentRequest
        {
            CardNumber = "1234567890123457", // Ends in 7 (odd) - will be authorized
            ExpiryMonth = 12,
            ExpiryYear = DateTime.UtcNow.Year + 1,
            Currency = "GBP",
            Amount = 1000,
            Cvv = "123"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/Payments", request);
        var paymentResponse = await response.Content.ReadFromJsonAsync<PostPaymentResponse>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(paymentResponse);
        Assert.Equal("Authorized", paymentResponse!.Status);
        Assert.NotEqual(Guid.Empty, paymentResponse.Id);
    }

    [Fact]
    public async Task PostPayment_ValidRequest_Declined_Returns200WithDeclinedStatus()
    {
        // Arrange
        var bankClientMock = new Mock<IBankClient>();
        var repository = new PaymentsRepository();

        var bankResponse = new BankPaymentResponse
        {
            Authorized = false,
            AuthorizationCode = string.Empty
        };

        bankClientMock
            .Setup(x => x.ProcessPaymentAsync(It.IsAny<BankPaymentRequest>()))
            .ReturnsAsync(bankResponse);

        var factory = new WebApplicationFactory<PaymentsController>();
        var client = factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IPaymentsRepository>(repository);
                services.AddScoped<IPaymentService>(_ =>
                    new PaymentService(bankClientMock.Object, repository, Mock.Of<Microsoft.Extensions.Logging.ILogger<PaymentService>>()));
                services.AddScoped<IBankClient>(_ => bankClientMock.Object);
            }))
            .CreateClient();

        var request = new PostPaymentRequest
        {
            CardNumber = "1234567890123456", // Ends in 6 (even) - will be declined
            ExpiryMonth = 12,
            ExpiryYear = DateTime.UtcNow.Year + 1,
            Currency = "GBP",
            Amount = 1000,
            Cvv = "123"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/Payments", request);
        var paymentResponse = await response.Content.ReadFromJsonAsync<PostPaymentResponse>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(paymentResponse);
        Assert.Equal("Declined", paymentResponse!.Status);
    }

    [Fact]
    public async Task PostPayment_InvalidRequest_Returns400WithValidationErrors()
    {
        // Arrange
        var bankClientMock = new Mock<IBankClient>();
        var repository = new PaymentsRepository();

        var factory = new WebApplicationFactory<PaymentsController>();
        var client = factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IPaymentsRepository>(repository);
                services.AddScoped<IPaymentService>(_ =>
                    new PaymentService(bankClientMock.Object, repository, Mock.Of<Microsoft.Extensions.Logging.ILogger<PaymentService>>()));
                services.AddScoped<IBankClient>(_ => bankClientMock.Object);
            }))
            .CreateClient();

        var request = new PostPaymentRequest
        {
            CardNumber = "123", // Invalid - too short
            ExpiryMonth = 12,
            ExpiryYear = DateTime.UtcNow.Year + 1,
            Currency = "GBP",
            Amount = 1000,
            Cvv = "123"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/Payments", request);
        var errorResponse = await response.Content.ReadFromJsonAsync<Dictionary<string, System.Text.Json.JsonElement>>();

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(errorResponse);
        Assert.True(errorResponse!.ContainsKey("errors"));

        var errors = errorResponse["errors"];
        Assert.True(errors.ValueKind == System.Text.Json.JsonValueKind.Array);
        Assert.True(errors.GetArrayLength() > 0);

        // Verify bank was not called
        bankClientMock.Verify(x => x.ProcessPaymentAsync(It.IsAny<BankPaymentRequest>()), Times.Never);
    }

    [Fact]
    public async Task PostPayment_BankServiceUnavailable_Returns503()
    {
        // Arrange
        var bankClientMock = new Mock<IBankClient>();
        var repository = new PaymentsRepository();

        bankClientMock
            .Setup(x => x.ProcessPaymentAsync(It.IsAny<BankPaymentRequest>()))
            .ThrowsAsync(new HttpRequestException("Bank service is unavailable", null, HttpStatusCode.ServiceUnavailable));

        var factory = new WebApplicationFactory<PaymentsController>();
        var client = factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IPaymentsRepository>(repository);
                services.AddScoped<IPaymentService>(_ =>
                    new PaymentService(bankClientMock.Object, repository, Mock.Of<Microsoft.Extensions.Logging.ILogger<PaymentService>>()));
                services.AddScoped<IBankClient>(_ => bankClientMock.Object);
            }))
            .CreateClient();

        var request = new PostPaymentRequest
        {
            CardNumber = "1234567890123450", // Ends in 0 - will cause 503
            ExpiryMonth = 12,
            ExpiryYear = DateTime.UtcNow.Year + 1,
            Currency = "GBP",
            Amount = 1000,
            Cvv = "123"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/Payments", request);

        // Assert
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task GetPayment_PaymentExists_Returns200()
    {
        // Arrange
        var repository = new PaymentsRepository();
        var payment = new PostPaymentResponse
        {
            Id = Guid.NewGuid(),
            Status = "Authorized",
            CardNumberLastFour = "3456",
            ExpiryMonth = 12,
            ExpiryYear = 2025,
            Currency = "GBP",
            Amount = 1000
        };

        await repository.AddAsync(payment);

        var factory = new WebApplicationFactory<PaymentsController>();
        var client = factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services => ((ServiceCollection)services)
                .AddSingleton<IPaymentsRepository>(repository)))
            .CreateClient();

        // Act
        var response = await client.GetAsync($"/api/Payments/{payment.Id}");
        var paymentResponse = await response.Content.ReadFromJsonAsync<PostPaymentResponse>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(paymentResponse);
        Assert.Equal(payment.Id, paymentResponse!.Id);
        Assert.Equal("Authorized", paymentResponse.Status);
    }

    [Fact]
    public async Task GetPayment_PaymentNotFound_Returns404()
    {
        // Arrange
        var repository = new PaymentsRepository();
        var factory = new WebApplicationFactory<PaymentsController>();
        var client = factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services => ((ServiceCollection)services)
                .AddSingleton<IPaymentsRepository>(repository)))
            .CreateClient();

        // Act
        var response = await client.GetAsync($"/api/Payments/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

