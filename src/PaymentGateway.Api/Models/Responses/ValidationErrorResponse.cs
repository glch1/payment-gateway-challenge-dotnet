namespace PaymentGateway.Api.Models.Responses;

public class ValidationErrorResponse
{
    /// <summary>
    /// List of validation error messages.
    /// </summary>
    public List<string> Errors { get; set; } = new();
}

