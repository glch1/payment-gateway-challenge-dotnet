using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using Polly.CircuitBreaker;
using PaymentGateway.Api.Interfaces;
using PaymentGateway.Api.Models.Bank;

namespace PaymentGateway.Api.Services;

/// <summary>
/// Client for communicating with the bank simulator API.
/// </summary>
public class BankClient : IBankClient
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly ILogger<BankClient> _logger;

    public BankClient(HttpClient httpClient, IConfiguration configuration, ILogger<BankClient> logger)
    {
        _httpClient = httpClient;
        _baseUrl = configuration["BankSimulator:BaseUrl"] ?? "http://localhost:8080";
        _logger = logger;
    }

    /// <summary>
    /// Processes a payment with the bank simulator.
    /// </summary>
    /// <param name="request">The bank payment request to process</param>
    /// <returns>The bank's response indicating if the payment was authorized or declined</returns>
    /// <exception cref="HttpRequestException">Thrown when the bank returns an error status code</exception>
    public async Task<BankPaymentResponse> ProcessPaymentAsync(BankPaymentRequest request)
    {
        var url = $"{_baseUrl}/payments";

        _logger.LogDebug("Sending payment request to bank. URL: {Url}, Amount: {Amount}, Currency: {Currency}", 
            url, request.Amount, request.Currency);

        try
        {
            var response = await _httpClient.PostAsJsonAsync(url, request);

            _logger.LogDebug("Received response from bank. StatusCode: {StatusCode}", response.StatusCode);

            if (response.StatusCode == HttpStatusCode.ServiceUnavailable)
            {
                _logger.LogDebug("Bank service returned ServiceUnavailable status");
                throw new HttpRequestException("Bank service is unavailable", null, HttpStatusCode.ServiceUnavailable);
            }

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogDebug("Bank rejected the request with BadRequest. Error content: {ErrorContent}", errorContent);
                throw new HttpRequestException($"Bank rejected the request: {errorContent}", null, HttpStatusCode.BadRequest);
            }

            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogDebug("Bank response content: {ResponseContent}", responseContent);

            BankPaymentResponse? bankResponse;
            try
            {
                bankResponse = JsonSerializer.Deserialize<BankPaymentResponse>(responseContent);
            }
            catch (JsonException ex)
            {
                _logger.LogDebug(ex, "Failed to deserialize bank response as JSON. Response content was: {ResponseContent}", responseContent);
                throw new HttpRequestException("Bank returned an invalid response", ex);
            }

            if (bankResponse == null)
            {
                _logger.LogDebug("Failed to deserialize bank response (null result). Response content was: {ResponseContent}", responseContent);
                throw new HttpRequestException("Bank returned an invalid response");
            }

            _logger.LogInformation("Payment processed successfully. Authorized: {Authorized}, AuthorizationCode: {AuthorizationCode}", 
                bankResponse.Authorized, bankResponse.AuthorizationCode);

            return bankResponse;
        }
        catch (BrokenCircuitException ex)
        {
            // Circuit breaker is open - bank service is unavailable
            _logger.LogDebug(ex, "Circuit breaker is open - bank service is unavailable");
            throw new HttpRequestException("Bank service is unavailable", ex, HttpStatusCode.ServiceUnavailable);
        }
        catch (SocketException ex)
        {
            // Socket exceptions (connection refused, etc.) should be treated as service unavailable
            _logger.LogDebug(ex, "Bank socket error - service appears to be unavailable");
            throw new HttpRequestException("Bank service is unavailable", ex, HttpStatusCode.ServiceUnavailable);
        }
        catch (HttpRequestException ex) when (IsConnectionError(ex))
        {
            // Connection errors (service not running, network issues, etc.) should be treated as service unavailable
            _logger.LogDebug(ex, "Bank connection error - service appears to be unavailable");
            throw new HttpRequestException("Bank service is unavailable", ex, HttpStatusCode.ServiceUnavailable);
        }
        catch (HttpRequestException)
        {
            throw;
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogDebug(ex, "Bank request timed out after waiting for response");
            throw new HttpRequestException("Bank request timed out", ex, HttpStatusCode.RequestTimeout);
        }
    }

    /// <summary>
    /// Determines if an HttpRequestException is due to a connection error (service unavailable).
    /// </summary>
    private static bool IsConnectionError(HttpRequestException ex)
    {
        if (ex.InnerException is SocketException)
        {
            return true;
        }

        var message = ex.Message.ToLowerInvariant();
        return message.Contains("connection") && 
               (message.Contains("refused") || 
                message.Contains("actively refused") || 
                message.Contains("could not be established") ||
                message.Contains("unreachable"));
    }
}

