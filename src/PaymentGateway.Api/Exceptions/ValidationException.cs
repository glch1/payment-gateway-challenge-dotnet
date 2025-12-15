namespace PaymentGateway.Api.Exceptions;

/// <summary>
/// Exception thrown when payment request validation fails.
/// </summary>
public class ValidationException : Exception
{
    public List<string> Errors { get; }

    public ValidationException(List<string> errors) : base("Payment request validation failed")
    {
        Errors = errors;
    }
}

