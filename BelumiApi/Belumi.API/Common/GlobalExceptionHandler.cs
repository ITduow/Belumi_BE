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
        context.Response.ContentType = "application/problem+json; charset=utf-8";

        var problemDetails = new ProblemDetails
        {
            Title = title,
            Status = status,
            Detail = exception.Message
        };

        problemDetails.Extensions["errorCode"] = errorCode;
        if (exception is ValidationException valEx)
        {
            problemDetails.Extensions["errors"] = valEx.Errors;
        }

        await context.Response.WriteAsJsonAsync(problemDetails, cancellationToken: ct);
        return true;
    }
}
