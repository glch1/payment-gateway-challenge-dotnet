namespace PaymentGateway.Api.Models.Responses;

public class ErrorResponse
{
    /// <summary>
    /// The error message of what went wrong.
    /// </summary>
    public string Error { get; set; } = string.Empty;
}

