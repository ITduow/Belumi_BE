namespace Belumi.Core.Exceptions;

public class NotFoundException : Exception
{
    public string ErrorCode { get; }

    public NotFoundException(string message, string errorCode = "NOT_FOUND")
        : base(message)
    {
        ErrorCode = errorCode;
    }
}
