namespace Belumi.Core.Exceptions;

public class ValidationException : Exception
{
    public string ErrorCode { get; }
    public IDictionary<string, string[]> Errors { get; }

    public ValidationException(
        IDictionary<string, string[]> errors,
        string message = "Validation failed",
        string errorCode = "VALIDATION_ERROR")
        : base(message)
    {
        Errors = errors;
        ErrorCode = errorCode;
    }
}
