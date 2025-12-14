using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using PaymentGateway.Api.Interfaces;
using PaymentGateway.Api.Models.Bank;
using PaymentGateway.Api.Services;

namespace PaymentGateway.Api.Tests;

public class BankClientTests
{
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly HttpClient _httpClient;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<ILogger<BankClient>> _loggerMock;
    private readonly IBankClient _bankClient;

    public BankClientTests()
    {
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        _configurationMock = new Mock<IConfiguration>();
        _loggerMock = new Mock<ILogger<BankClient>>();

        var baseUrlSection = new Mock<IConfigurationSection>();
        baseUrlSection.Setup(x => x.Value).Returns("http://localhost:8080");
        _configurationMock.Setup(x => x["BankSimulator:BaseUrl"]).Returns("http://localhost:8080");

        _bankClient = new BankClient(_httpClient, _configurationMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task ProcessPaymentAsync_AuthorizedResponse_ReturnsAuthorized()
    {
        // Arrange
        var request = CreateValidRequest();
        var bankResponse = new BankPaymentResponse
        {
            Authorized = true,
            AuthorizationCode = "test-auth-code-123"
        };

        SetupHttpResponse(HttpStatusCode.OK, bankResponse);

        // Act
        var result = await _bankClient.ProcessPaymentAsync(request);

        // Assert
        Assert.True(result.Authorized);
        Assert.Equal("test-auth-code-123", result.AuthorizationCode);
        VerifyHttpCall("http://localhost:8080/payments");
    }

    [Fact]
    public async Task ProcessPaymentAsync_DeclinedResponse_ReturnsDeclined()
    {
        // Arrange
        var request = CreateValidRequest();
        var bankResponse = new BankPaymentResponse
        {
            Authorized = false,
            AuthorizationCode = string.Empty
        };

        SetupHttpResponse(HttpStatusCode.OK, bankResponse);

        // Act
        var result = await _bankClient.ProcessPaymentAsync(request);

        // Assert
        Assert.False(result.Authorized);
        Assert.Empty(result.AuthorizationCode);
    }

    [Fact]
    public async Task ProcessPaymentAsync_SendsCorrectRequestFormat()
    {
        // Arrange
        var request = CreateValidRequest();
        request.ExpiryDate = "05/2025";

        SetupHttpResponse(HttpStatusCode.OK, new BankPaymentResponse { Authorized = true, AuthorizationCode = "test" });

        // Act
        await _bankClient.ProcessPaymentAsync(request);

        // Assert
        _httpMessageHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Post &&
                req.RequestUri!.ToString() == "http://localhost:8080/payments"),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task ProcessPaymentAsync_ServiceUnavailable_ThrowsHttpRequestException()
    {
        // Arrange
        var request = CreateValidRequest();
        SetupHttpResponse(HttpStatusCode.ServiceUnavailable, null);

        // Act
        var exception = await Assert.ThrowsAsync<HttpRequestException>(
            () => _bankClient.ProcessPaymentAsync(request));

        // Assert
        Assert.Contains("Bank service is unavailable", exception.Message);
    }

    [Fact]
    public async Task ProcessPaymentAsync_BadRequest_ThrowsHttpRequestException()
    {
        // Arrange
        var request = CreateValidRequest();
        var errorResponse = new { error_message = "Not all required properties were sent" };
        SetupHttpResponse(HttpStatusCode.BadRequest, errorResponse, isJson: true);

        // Act
        var exception = await Assert.ThrowsAsync<HttpRequestException>(
            () => _bankClient.ProcessPaymentAsync(request));

        // Assert
        Assert.Contains("Bank rejected the request", exception.Message);
    }

    [Fact]
    public async Task ProcessPaymentAsync_Timeout_ThrowsHttpRequestException()
    {
        // Arrange
        var request = CreateValidRequest();
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException("Request timed out", new TimeoutException()));

        // Act
        var exception = await Assert.ThrowsAsync<HttpRequestException>(
            () => _bankClient.ProcessPaymentAsync(request));

        // Assert
        Assert.Contains("Bank request timed out", exception.Message);
    }

    [Fact]
    public async Task ProcessPaymentAsync_InvalidJsonResponse_ThrowsHttpRequestException()
    {
        // Arrange
        var request = CreateValidRequest();
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("invalid json", Encoding.UTF8, "application/json")
        };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        // Act
        var exception = await Assert.ThrowsAsync<HttpRequestException>(
            () => _bankClient.ProcessPaymentAsync(request));

        // Assert
        Assert.NotNull(exception);
    }

    [Fact]
    public async Task ProcessPaymentAsync_UsesBaseUrlFromConfiguration()
    {
        // Arrange
        var customBaseUrl = "http://custom-bank-url:9090";
        _configurationMock.Setup(x => x["BankSimulator:BaseUrl"]).Returns(customBaseUrl);
        var customLogger = new Mock<ILogger<BankClient>>();
        var customClient = new BankClient(_httpClient, _configurationMock.Object, customLogger.Object);
        var request = CreateValidRequest();
        SetupHttpResponse(HttpStatusCode.OK, new BankPaymentResponse { Authorized = true, AuthorizationCode = "test" });

        // Act
        await customClient.ProcessPaymentAsync(request);

        // Assert
        VerifyHttpCall($"{customBaseUrl}/payments");
    }

    [Fact]
    public async Task ProcessPaymentAsync_DefaultBaseUrl_WhenConfigMissing()
    {
        // Arrange
        var emptyConfig = new Mock<IConfiguration>();
        emptyConfig.Setup(x => x["BankSimulator:BaseUrl"]).Returns((string?)null);
        var defaultLogger = new Mock<ILogger<BankClient>>();
        var defaultClient = new BankClient(_httpClient, emptyConfig.Object, defaultLogger.Object);
        var request = CreateValidRequest();
        SetupHttpResponse(HttpStatusCode.OK, new BankPaymentResponse { Authorized = true, AuthorizationCode = "test" });

        // Act
        await defaultClient.ProcessPaymentAsync(request);

        // Assert
        VerifyHttpCall("http://localhost:8080/payments");
    }

    private void SetupHttpResponse(HttpStatusCode statusCode, object? content, bool isJson = false)
    {
        var response = new HttpResponseMessage(statusCode);

        if (content != null)
        {
            if (isJson)
            {
                response.Content = new StringContent(
                    JsonSerializer.Serialize(content),
                    Encoding.UTF8,
                    "application/json");
            }
            else
            {
                response.Content = JsonContent.Create(content);
            }
        }

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);
    }

    private void VerifyHttpCall(string expectedUrl)
    {
        _httpMessageHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Post &&
                req.RequestUri!.ToString() == expectedUrl),
            ItExpr.IsAny<CancellationToken>());
    }

    private static BankPaymentRequest CreateValidRequest()
    {
        return new BankPaymentRequest
        {
            CardNumber = "1234567890123456",
            ExpiryDate = "12/2025",
            Currency = "GBP",
            Amount = 1000,
            Cvv = "123"
        };
    }
}

