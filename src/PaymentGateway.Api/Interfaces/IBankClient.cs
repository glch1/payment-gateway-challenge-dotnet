using PaymentGateway.Api.Models.Bank;

namespace PaymentGateway.Api.Interfaces;

/// <summary>
/// Interface for communicating with the bank simulator API.
/// </summary>
public interface IBankClient
{
    /// <summary>
    /// Processes a payment with the bank simulator.
    /// </summary>
    /// <param name="request">The bank payment request to process</param>
    /// <returns>The bank's response indicating if the payment was authorized or declined</returns>
    /// <exception cref="HttpRequestException">Thrown when the bank returns an error status code</exception>
    Task<BankPaymentResponse> ProcessPaymentAsync(BankPaymentRequest request);
}

