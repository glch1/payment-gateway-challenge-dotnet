namespace PaymentGateway.Api.Models;

public class ValidationResult
{
    public bool IsValid { get; init; }
    public List<string> Errors { get; init; } = new();

    private ValidationResult(bool isValid, List<string> errors)
    {
        IsValid = isValid;
        Errors = errors;
    }

    public static ValidationResult Success()
    {
        return new ValidationResult(true, new List<string>());
    }

    public static ValidationResult Failure(List<string> errors)
    {
        return new ValidationResult(false, errors);
    }

    public static ValidationResult Failure(string error)
    {
        return new ValidationResult(false, new List<string> { error });
    }
}
