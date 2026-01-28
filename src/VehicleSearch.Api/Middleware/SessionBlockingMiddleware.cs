using System.Text.Json;
using VehicleSearch.Core.Enums;
using VehicleSearch.Core.Interfaces;
using VehicleSearch.Core.Models;

namespace VehicleSearch.Api.Middleware;

/// <summary>
/// Middleware for checking blocked sessions and auto-blocking critical risk sessions.
/// </summary>
public class SessionBlockingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SessionBlockingMiddleware> _logger;

    public SessionBlockingMiddleware(
        RequestDelegate next, 
        ILogger<SessionBlockingMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(
        HttpContext context, 
        IAbuseMonitoringService abuseMonitoringService)
    {
        // Only apply to search and query endpoints
        if (!context.Request.Path.StartsWithSegments("/api/v1/search") &&
            !context.Request.Path.StartsWithSegments("/api/v1/query") &&
            !context.Request.Path.StartsWithSegments("/api/v1/conversation"))
        {
            await _next(context);
            return;
        }

        try
        {
            // Get session ID from headers
            var sessionId = context.Request.Headers["X-Session-Id"].FirstOrDefault();
            
            if (string.IsNullOrEmpty(sessionId))
            {
                // No session ID, allow request to proceed
                await _next(context);
                return;
            }

            // Check if session is blocked
            var isBlocked = await abuseMonitoringService.IsSessionBlockedAsync(sessionId, context.RequestAborted);
            
            if (isBlocked)
            {
                var blockInfo = await abuseMonitoringService.GetBlockInfoAsync(sessionId, context.RequestAborted);
                
                _logger.LogWarning(
                    "Blocked session {SessionId} attempted access. Reason: {Reason}, Expires: {ExpiresAt}",
                    sessionId,
                    blockInfo?.Reason,
                    blockInfo?.ExpiresAt
                );

                context.Response.StatusCode = 403; // Forbidden
                context.Response.ContentType = "application/json";

                var errorResponse = new
                {
                    error = new
                    {
                        code = "SESSION_BLOCKED",
                        message = "Your session has been temporarily blocked due to suspicious activity.",
                        details = new
                        {
                            blockedAt = blockInfo?.BlockedAt,
                            expiresAt = blockInfo?.ExpiresAt,
                            remainingTime = blockInfo?.RemainingTime.ToString(@"hh\:mm\:ss"),
                            reason = blockInfo?.Reason
                        }
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

            // Detect suspicious activity before processing request
            var suspiciousActivity = await abuseMonitoringService.DetectSuspiciousActivityAsync(
                sessionId, 
                context.RequestAborted);

            // Auto-block critical risk sessions
            if (suspiciousActivity.RiskLevel == RiskLevel.Critical)
            {
                _logger.LogCritical(
                    "Critical risk detected for session {SessionId}. Auto-blocking for 1 hour.",
                    sessionId
                );

                await abuseMonitoringService.BlockSessionAsync(
                    sessionId,
                    TimeSpan.FromHours(1),
                    $"Automatic block due to critical risk: {suspiciousActivity.Recommendation}",
                    context.RequestAborted
                );

                // Log security event
                await abuseMonitoringService.LogSecurityEventAsync(new SecurityEvent
                {
                    SessionId = sessionId,
                    EventType = EventType.AbnormalActivity,
                    Description = "Critical risk detected - session auto-blocked",
                    Metadata = new Dictionary<string, object>
                    {
                        ["RiskLevel"] = suspiciousActivity.RiskLevel.ToString(),
                        ["MaxSeverity"] = suspiciousActivity.MaxSeverity,
                        ["DetectedPatterns"] = suspiciousActivity.DetectedPatterns.Select(p => p.PatternType.ToString()).ToList()
                    }
                }, context.RequestAborted);

                context.Response.StatusCode = 403;
                context.Response.ContentType = "application/json";

                var errorResponse = new
                {
                    error = new
                    {
                        code = "SESSION_BLOCKED",
                        message = "Your session has been blocked due to suspicious activity patterns.",
                        details = new
                        {
                            riskLevel = suspiciousActivity.RiskLevel.ToString(),
                            detectedPatterns = suspiciousActivity.DetectedPatterns.Select(p => p.Description).ToList(),
                            recommendation = suspiciousActivity.Recommendation
                        }
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

            // Log high risk but don't block yet
            if (suspiciousActivity.RiskLevel == RiskLevel.High)
            {
                _logger.LogWarning(
                    "High risk activity detected for session {SessionId}: {Patterns}",
                    sessionId,
                    string.Join(", ", suspiciousActivity.DetectedPatterns.Select(p => p.PatternType))
                );

                await abuseMonitoringService.LogSecurityEventAsync(new SecurityEvent
                {
                    SessionId = sessionId,
                    EventType = EventType.AbnormalActivity,
                    Description = "High risk activity detected",
                    Metadata = new Dictionary<string, object>
                    {
                        ["RiskLevel"] = suspiciousActivity.RiskLevel.ToString(),
                        ["MaxSeverity"] = suspiciousActivity.MaxSeverity,
                        ["DetectedPatterns"] = suspiciousActivity.DetectedPatterns.Select(p => p.PatternType.ToString()).ToList()
                    }
                }, context.RequestAborted);
            }

            // Allow request to proceed
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during session blocking check");
            // Let the exception propagate to the exception handling middleware
            throw;
        }
    }
}
