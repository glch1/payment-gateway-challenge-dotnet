using System.Collections.Concurrent;
using PaymentGateway.Api.Interfaces;
using PaymentGateway.Api.Models.Responses;

namespace PaymentGateway.Api.Services;

/// <summary>
/// In-memory repository for storing and retrieving payment records.
/// </summary>
public class PaymentsRepository : IPaymentsRepository
{
    private readonly ConcurrentDictionary<Guid, PaymentResponse> _payments = new();

    /// <summary>
    /// Adds a payment to the repository.
    /// </summary>
    /// <param name="payment">The payment to add</param>
    public Task AddAsync(PaymentResponse payment)
    {
        if (payment == null)
        {
            throw new ArgumentNullException(nameof(payment));
        }

        _payments.TryAdd(payment.Id, payment);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Retrieves a payment by its unique identifier.
    /// </summary>
    /// <param name="id">The payment identifier</param>
    /// <returns>The payment if found, otherwise null</returns>
    public Task<PaymentResponse?> GetAsync(Guid id)
    {
        if (id == Guid.Empty)
        {
            return Task.FromResult<PaymentResponse?>(null);
        }

        _payments.TryGetValue(id, out var payment);
        return Task.FromResult<PaymentResponse?>(payment);
    }
}