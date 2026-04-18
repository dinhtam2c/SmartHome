using Application.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Middlewares;

public class WebApiExceptionHandler : IExceptionHandler
{
    private readonly ILogger<WebApiExceptionHandler> _logger;

    public WebApiExceptionHandler(ILogger<WebApiExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception ex,
        CancellationToken cancellationToken)
    {
        _logger.LogError(ex, "Exception: {Message}", ex.Message);

        var (statusCode, title, detail) = ex switch
        {
            AppException appEx => (appEx.StatusCode, GetTitle(appEx.StatusCode), ex.Message),
            _ => (StatusCodes.Status500InternalServerError, "Internal server error", "An internal error occured.")
        };

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };

        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }

    private static string GetTitle(int statusCode) => statusCode switch
    {
        404 => "Resource not found",
        400 => "Bad request",
        409 => "Conflict",
        401 => "Unauthorized",
        403 => "Forbidden",
        _ => "Error"
    };
}
