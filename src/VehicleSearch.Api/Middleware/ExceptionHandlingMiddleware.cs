using System.Net;
using System.Text.Json;
using VehicleSearch.Core.Exceptions;

namespace VehicleSearch.Api.Middleware;

/// <summary>
/// Middleware for global exception handling.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExceptionHandlingMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="logger">The logger instance.</param>
    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Invokes the middleware.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        _logger.LogError(exception, "An unhandled exception occurred. TraceId: {TraceId}", context.TraceIdentifier);

        var (statusCode, errorCode, message, details) = exception switch
        {
            ValidationException validationEx => (
                HttpStatusCode.BadRequest,
                "VALIDATION_ERROR",
                validationEx.Message,
                validationEx.Errors),
            ServiceException serviceEx => (
                HttpStatusCode.InternalServerError,
                serviceEx.ErrorCode,
                serviceEx.Message,
                new List<string>()),
            _ => (
                HttpStatusCode.InternalServerError,
                "INTERNAL_ERROR",
                "An internal error occurred. Please try again later.",
                new List<string>())
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var errorResponse = new
        {
            error = new
            {
                code = errorCode,
                message = message,
                details = details
            },
            timestamp = DateTime.UtcNow.ToString("O"),
            traceId = context.TraceIdentifier
        };

        var json = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });

        await context.Response.WriteAsync(json);
    }
}
