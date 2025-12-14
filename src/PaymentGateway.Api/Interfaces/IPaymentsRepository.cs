using PaymentGateway.Api.Models.Responses;

namespace PaymentGateway.Api.Interfaces;

/// <summary>
/// Interface for storing and retrieving payment records.
/// </summary>
public interface IPaymentsRepository
{
    /// <summary>
    /// Adds a payment to the repository.
    /// </summary>
    /// <param name="payment">The payment to add</param>
    Task AddAsync(PostPaymentResponse payment);

    /// <summary>
    /// Retrieves a payment by its unique identifier.
    /// </summary>
    /// <param name="id">The payment identifier</param>
    /// <returns>The payment if found, otherwise null</returns>
    Task<PostPaymentResponse?> GetAsync(Guid id);
}

