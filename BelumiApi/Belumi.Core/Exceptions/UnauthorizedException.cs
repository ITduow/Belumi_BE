namespace Belumi.Core.Exceptions;

public class UnauthorizedException : Exception
{
    public string ErrorCode => "UNAUTHORIZED";

    public UnauthorizedException(string message) : base(message)
    {
    }
}
