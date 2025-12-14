using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;

namespace PaymentGateway.Api.Interfaces;

/// <summary>
/// Interface for processing payment requests.
/// </summary>
public interface IPaymentService
{
    /// <summary>
    /// Processes a payment request: validates, calls bank, stores result, and returns response.
    /// </summary>
    /// <param name="request">The payment request to process</param>
    /// <returns>The payment response with status and details</returns>
    Task<PostPaymentResponse> ProcessPaymentAsync(PostPaymentRequest request);
}

