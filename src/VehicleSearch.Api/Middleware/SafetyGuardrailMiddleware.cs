using System.Text.Json;
using VehicleSearch.Core.Interfaces;

namespace VehicleSearch.Api.Middleware;

/// <summary>
/// Middleware for applying safety guardrails to API requests.
/// </summary>
public class SafetyGuardrailMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SafetyGuardrailMiddleware> _logger;

    public SafetyGuardrailMiddleware(RequestDelegate next, ILogger<SafetyGuardrailMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context, ISafetyGuardrailService safetyService)
    {
        // Only apply safety checks to search and query endpoints
        if (context.Request.Path.StartsWithSegments("/api/v1/search") ||
            context.Request.Path.StartsWithSegments("/api/v1/query"))
        {
            try
            {
                // Extract query from request
                var query = await ExtractQueryFromRequest(context.Request);

                if (!string.IsNullOrEmpty(query))
                {
                    // Get session ID from headers or use anonymous
                    var sessionId = context.Request.Headers["X-Session-Id"].FirstOrDefault() ?? "anonymous";

                    // Validate the query
                    var result = await safetyService.ValidateQueryAsync(query, sessionId, context.RequestAborted);

                    if (!result.IsValid)
                    {
                        _logger.LogWarning(
                            "Safety validation failed for query. ViolationType: {ViolationType}, Message: {Message}",
                            result.ViolationType,
                            result.Message
                        );

                        context.Response.StatusCode = 400; // Bad Request
                        context.Response.ContentType = "application/json";

                        var errorResponse = new
                        {
                            error = new
                            {
                                code = result.ViolationType?.ToString() ?? "VALIDATION_ERROR",
                                message = result.Message,
                                details = result.Errors
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
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during safety validation");
                // Let the exception propagate to the exception handling middleware
                throw;
            }
        }

        await _next(context);
    }

    private async Task<string?> ExtractQueryFromRequest(HttpRequest request)
    {
        // Check query string first
        if (request.Query.ContainsKey("query"))
        {
            return request.Query["query"].ToString();
        }

        if (request.Query.ContainsKey("q"))
        {
            return request.Query["q"].ToString();
        }

        // Check request body for POST requests
        if (request.Method == HttpMethods.Post && request.ContentType?.Contains("application/json") == true)
        {
            try
            {
                // Enable buffering so the request body can be read multiple times
                request.EnableBuffering();

                using var reader = new StreamReader(request.Body, leaveOpen: true);
                var body = await reader.ReadToEndAsync();
                request.Body.Position = 0; // Reset position for next middleware

                if (!string.IsNullOrWhiteSpace(body))
                {
                    using var jsonDocument = JsonDocument.Parse(body);
                    if (jsonDocument.RootElement.TryGetProperty("query", out var queryElement))
                    {
                        return queryElement.GetString();
                    }
                    if (jsonDocument.RootElement.TryGetProperty("message", out var messageElement))
                    {
                        return messageElement.GetString();
                    }
                }
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse JSON request body for query extraction");
                // Return null to skip validation - malformed JSON will be caught by model validation
            }
        }

        return null;
    }
}
