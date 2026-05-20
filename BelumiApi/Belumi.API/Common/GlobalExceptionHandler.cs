using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Belumi.Core.Exceptions;

namespace Belumi.API.Common;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IProblemDetailsService _problemDetails;

    public GlobalExceptionHandler(
        ILogger<GlobalExceptionHandler> logger,
        IProblemDetailsService problemDetails)
    {
        _logger = logger;
        _problemDetails = problemDetails;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken ct)
    {
        var (status, title, errorCode) = exception switch
        {
            ValidationException ve  => (400, ve.Message,  ve.ErrorCode),
            NotFoundException nf    => (404, nf.Message,  nf.ErrorCode),
            ConflictException ce    => (409, ce.Message,  ce.ErrorCode),
            UnauthorizedException ue => (401, ue.Message, ue.ErrorCode),
            ForbiddenException      => (403, "Forbidden", "FORBIDDEN"),
            _ => (500, "An unexpected error occurred.", "INTERNAL_SERVER_ERROR")
        };

        if (status == 500)
        {
            _logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);
        }

        context.Response.StatusCode = status;

        return await _problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            Exception = exception,
            ProblemDetails =
            {
                Title  = title,
                Status = status,
                Detail = exception.Message,
                Extensions =
                {
                    ["errorCode"] = errorCode,
                    ["errors"]    = exception is ValidationException valEx
                                    ? valEx.Errors
                                    : null
                }
            }
        });
    }
}
