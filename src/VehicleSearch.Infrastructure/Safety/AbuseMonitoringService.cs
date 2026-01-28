using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using VehicleSearch.Core.Enums;
using VehicleSearch.Core.Interfaces;
using VehicleSearch.Core.Models;

namespace VehicleSearch.Infrastructure.Safety;

/// <summary>
/// Service for monitoring abuse, detecting suspicious patterns, and blocking sessions.
/// </summary>
public class AbuseMonitoringService : IAbuseMonitoringService
{
    private readonly ILogger<AbuseMonitoringService> _logger;
    private readonly IMemoryCache _cache;
    private readonly IConversationSessionService _sessionService;
    
    // Configuration thresholds
    private readonly int _rapidRequestsCount;
    private readonly int _rapidRequestsWindowSeconds;
    private readonly int _repeatedQueryCount;
    private readonly double _offTopicRatioThreshold;
    private readonly int _promptInjectionAttempts;
    private readonly int _largeResultRequests;
    private readonly int _largeResultThreshold;
    private readonly TimeSpan _criticalRiskDuration;
    private readonly TimeSpan _highRiskDuration;

    public AbuseMonitoringService(
        ILogger<AbuseMonitoringService> logger,
        IMemoryCache cache,
        IConversationSessionService sessionService,
        IConfiguration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));

        // Load configuration with defaults
        _rapidRequestsCount = configuration.GetValue("AbuseMonitoring:Thresholds:RapidRequests:Count", 5);
        _rapidRequestsWindowSeconds = configuration.GetValue("AbuseMonitoring:Thresholds:RapidRequests:WindowSeconds", 10);
        _repeatedQueryCount = configuration.GetValue("AbuseMonitoring:Thresholds:RepeatedQueryCount", 3);
        _offTopicRatioThreshold = configuration.GetValue("AbuseMonitoring:Thresholds:OffTopicRatioThreshold", 0.5);
        _promptInjectionAttempts = configuration.GetValue("AbuseMonitoring:Thresholds:PromptInjectionAttempts", 2);
        _largeResultRequests = configuration.GetValue("AbuseMonitoring:Thresholds:LargeResultRequests", 3);
        _largeResultThreshold = configuration.GetValue("AbuseMonitoring:Thresholds:LargeResultThreshold", 50);
        
        _criticalRiskDuration = configuration.GetValue("AbuseMonitoring:Blocking:CriticalRiskDuration", TimeSpan.FromHours(1));
        _highRiskDuration = configuration.GetValue("AbuseMonitoring:Blocking:HighRiskDuration", TimeSpan.FromMinutes(30));
    }

    /// <inheritdoc/>
    public async Task<SuspiciousActivityReport> DetectSuspiciousActivityAsync(
        string sessionId, 
        CancellationToken cancellationToken = default)
    {
        var patterns = new List<SuspiciousPattern>();
        
        // Get session metadata
        var metadata = await GetSessionMetadataAsync(sessionId, cancellationToken);

        // Pattern 1: Rapid Requests
        var rapidRequestPattern = DetectRapidRequests(sessionId, metadata);
        if (rapidRequestPattern != null)
        {
            patterns.Add(rapidRequestPattern);
        }

        // Pattern 2: Repeated Queries
        var repeatedQueryPattern = DetectRepeatedQueries(metadata);
        if (repeatedQueryPattern != null)
        {
            patterns.Add(repeatedQueryPattern);
        }

        // Pattern 3: Off-Topic Flood
        var offTopicPattern = DetectOffTopicFlood(metadata);
        if (offTopicPattern != null)
        {
            patterns.Add(offTopicPattern);
        }

        // Pattern 4: Prompt Injection Attempts
        var injectionPattern = DetectPromptInjectionAttempts(metadata);
        if (injectionPattern != null)
        {
            patterns.Add(injectionPattern);
        }

        // Pattern 5: Bulk Extraction
        var bulkExtractionPattern = DetectBulkExtraction(metadata);
        if (bulkExtractionPattern != null)
        {
            patterns.Add(bulkExtractionPattern);
        }

        // Calculate risk level
        var maxSeverity = patterns.Any() ? patterns.Max(p => p.Severity) : 0.0;
        var riskLevel = CalculateRiskLevel(maxSeverity);
        var recommendation = GetRecommendation(riskLevel);

        return new SuspiciousActivityReport
        {
            SessionId = sessionId,
            DetectedPatterns = patterns,
            RiskLevel = riskLevel,
            MaxSeverity = maxSeverity,
            Recommendation = recommendation,
            AnalyzedAt = DateTime.UtcNow
        };
    }

    /// <inheritdoc/>
    public Task LogSecurityEventAsync(
        SecurityEvent securityEvent, 
        CancellationToken cancellationToken = default)
    {
        if (securityEvent == null)
        {
            throw new ArgumentNullException(nameof(securityEvent));
        }

        // Determine log level based on event type
        var logLevel = securityEvent.EventType switch
        {
            EventType.SessionBlocked => LogLevel.Critical,
            EventType.PromptInjectionDetected => LogLevel.Critical,
            EventType.BulkExtractionAttempt => LogLevel.Warning,
            EventType.RateLimitExceeded => LogLevel.Warning,
            EventType.AbnormalActivity => LogLevel.Warning,
            _ => LogLevel.Information
        };

        // Log with structured data
        _logger.Log(
            logLevel,
            "Security Event: {EventType} | Session: {SessionId} | Description: {Description} | EventId: {EventId} | Metadata: {@Metadata}",
            securityEvent.EventType,
            securityEvent.SessionId,
            securityEvent.Description,
            securityEvent.EventId,
            securityEvent.Metadata
        );

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task BlockSessionAsync(
        string sessionId, 
        TimeSpan duration, 
        string reason, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            throw new ArgumentException("Session ID cannot be null or empty.", nameof(sessionId));
        }

        var blockInfo = new SessionBlockInfo
        {
            SessionId = sessionId,
            BlockedAt = DateTime.UtcNow,
            Duration = duration,
            ExpiresAt = DateTime.UtcNow.Add(duration),
            Reason = reason
        };

        var cacheKey = $"blocked_session:{sessionId}";
        _cache.Set(cacheKey, blockInfo, duration);

        // Log security event
        await LogSecurityEventAsync(new SecurityEvent
        {
            SessionId = sessionId,
            EventType = EventType.SessionBlocked,
            Description = $"Session blocked for {duration.TotalMinutes:F0} minutes. Reason: {reason}",
            Metadata = new Dictionary<string, object>
            {
                ["Duration"] = duration.ToString(),
                ["Reason"] = reason,
                ["ExpiresAt"] = blockInfo.ExpiresAt
            }
        }, cancellationToken);

        _logger.LogWarning(
            "Session {SessionId} blocked for {Duration} minutes. Reason: {Reason}",
            sessionId,
            duration.TotalMinutes,
            reason
        );
    }

    /// <inheritdoc/>
    public Task UnblockSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return Task.CompletedTask;
        }

        var cacheKey = $"blocked_session:{sessionId}";
        _cache.Remove(cacheKey);

        _logger.LogInformation("Session {SessionId} unblocked", sessionId);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<bool> IsSessionBlockedAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return Task.FromResult(false);
        }

        var cacheKey = $"blocked_session:{sessionId}";
        var blockInfo = _cache.Get<SessionBlockInfo>(cacheKey);

        return Task.FromResult(blockInfo?.IsActive ?? false);
    }

    /// <inheritdoc/>
    public Task<SessionBlockInfo?> GetBlockInfoAsync(
        string sessionId, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return Task.FromResult<SessionBlockInfo?>(null);
        }

        var cacheKey = $"blocked_session:{sessionId}";
        var blockInfo = _cache.Get<SessionBlockInfo>(cacheKey);

        return Task.FromResult(blockInfo?.IsActive == true ? blockInfo : null);
    }

    /// <inheritdoc/>
    public async Task<AbuseReport> GenerateAbuseReportAsync(
        string sessionId, 
        TimeSpan? timeWindow = null, 
        CancellationToken cancellationToken = default)
    {
        timeWindow ??= TimeSpan.FromHours(1);
        var windowEnd = DateTime.UtcNow;
        var windowStart = windowEnd.Subtract(timeWindow.Value);

        var metadata = await GetSessionMetadataAsync(sessionId, cancellationToken);
        var suspiciousActivity = await DetectSuspiciousActivityAsync(sessionId, cancellationToken);

        // Extract metrics from metadata
        var totalQueries = metadata.GetValueOrDefault("TotalQueries", 0);
        var offTopicCount = metadata.GetValueOrDefault("OffTopicCount", 0);
        var injectionCount = metadata.GetValueOrDefault("PromptInjectionCount", 0);
        var rateLimitCount = metadata.GetValueOrDefault("RateLimitViolations", 0);

        return new AbuseReport
        {
            SessionId = sessionId,
            WindowStart = windowStart,
            WindowEnd = windowEnd,
            TotalQueries = Convert.ToInt32(totalQueries),
            OffTopicQueryCount = Convert.ToInt32(offTopicCount),
            PromptInjectionAttempts = Convert.ToInt32(injectionCount),
            RateLimitViolations = Convert.ToInt32(rateLimitCount),
            SuspiciousPatterns = suspiciousActivity.DetectedPatterns,
            RiskLevel = suspiciousActivity.RiskLevel,
            Recommendation = suspiciousActivity.Recommendation,
            GeneratedAt = DateTime.UtcNow
        };
    }

    /// <inheritdoc/>
    public async Task TrackQueryAsync(
        string sessionId, 
        string query, 
        int resultCount = 0,
        bool wasOffTopic = false,
        bool hadPromptInjection = false,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return;
        }

        try
        {
            var session = await _sessionService.GetSessionAsync(sessionId, cancellationToken);
            
            // Use lock for thread-safe metadata updates
            lock (session.Metadata)
            {
                // Initialize or get existing counters
                var totalQueries = session.Metadata.GetValueOrDefault("TotalQueries", 0);
                var offTopicCount = session.Metadata.GetValueOrDefault("OffTopicCount", 0);
                var injectionCount = session.Metadata.GetValueOrDefault("PromptInjectionCount", 0);
                var largeResultCount = session.Metadata.GetValueOrDefault("LargeResultCount", 0);

                // Update counters
                session.Metadata["TotalQueries"] = Convert.ToInt32(totalQueries) + 1;
                
                if (wasOffTopic)
                {
                    session.Metadata["OffTopicCount"] = Convert.ToInt32(offTopicCount) + 1;
                }
                
                if (hadPromptInjection)
                {
                    session.Metadata["PromptInjectionCount"] = Convert.ToInt32(injectionCount) + 1;
                }
                
                if (resultCount >= _largeResultThreshold)
                {
                    session.Metadata["LargeResultCount"] = Convert.ToInt32(largeResultCount) + 1;
                }

                // Track query history
                var queryHistory = session.Metadata.GetValueOrDefault("QueryHistory", new List<string>()) as List<string> ?? new List<string>();
                queryHistory.Add(query);
                
                // Keep only recent queries (last 20)
                if (queryHistory.Count > 20)
                {
                    queryHistory = queryHistory.Skip(queryHistory.Count - 20).ToList();
                }
                session.Metadata["QueryHistory"] = queryHistory;

                // Track request timestamps for rapid request detection
                var timestamps = session.Metadata.GetValueOrDefault("RequestTimestamps", new List<DateTime>()) as List<DateTime> ?? new List<DateTime>();
                timestamps.Add(DateTime.UtcNow);
                
                // Keep only recent timestamps (last 10 seconds)
                var cutoff = DateTime.UtcNow.AddSeconds(-_rapidRequestsWindowSeconds);
                timestamps = timestamps.Where(t => t > cutoff).ToList();
                session.Metadata["RequestTimestamps"] = timestamps;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to track query for session {SessionId}", sessionId);
        }
    }

    #region Private Helper Methods

    private async Task<Dictionary<string, object>> GetSessionMetadataAsync(
        string sessionId, 
        CancellationToken cancellationToken)
    {
        try
        {
            var session = await _sessionService.GetSessionAsync(sessionId, cancellationToken);
            return session.Metadata;
        }
        catch
        {
            // Return empty metadata if session doesn't exist
            return new Dictionary<string, object>();
        }
    }

    private SuspiciousPattern? DetectRapidRequests(string sessionId, Dictionary<string, object> metadata)
    {
        var timestamps = metadata.GetValueOrDefault("RequestTimestamps", new List<DateTime>()) as List<DateTime> ?? new List<DateTime>();
        
        if (timestamps.Count <= _rapidRequestsCount)
        {
            return null;
        }

        var cutoff = DateTime.UtcNow.AddSeconds(-_rapidRequestsWindowSeconds);
        var recentRequests = timestamps.Where(t => t > cutoff).ToList();

        if (recentRequests.Count > _rapidRequestsCount)
        {
            // Calculate severity based on how many requests over the threshold
            var excess = recentRequests.Count - _rapidRequestsCount;
            var severity = Math.Min(0.5 + (excess * 0.1), 1.0);

            return new SuspiciousPattern
            {
                PatternType = PatternType.RapidRequests,
                Severity = severity,
                Description = $"{recentRequests.Count} requests in {_rapidRequestsWindowSeconds} seconds (threshold: {_rapidRequestsCount})",
                Metadata = new Dictionary<string, object>
                {
                    ["RequestCount"] = recentRequests.Count,
                    ["WindowSeconds"] = _rapidRequestsWindowSeconds,
                    ["Threshold"] = _rapidRequestsCount
                }
            };
        }

        return null;
    }

    private SuspiciousPattern? DetectRepeatedQueries(Dictionary<string, object> metadata)
    {
        var queryHistory = metadata.GetValueOrDefault("QueryHistory", new List<string>()) as List<string> ?? new List<string>();
        
        if (queryHistory.Count < _repeatedQueryCount)
        {
            return null;
        }

        // Group queries and find most common
        var queryCounts = queryHistory
            .GroupBy(q => q.ToLower().Trim())
            .Select(g => new { Query = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .FirstOrDefault();

        if (queryCounts != null && queryCounts.Count > _repeatedQueryCount)
        {
            return new SuspiciousPattern
            {
                PatternType = PatternType.RepeatedQueries,
                Severity = 0.6,
                Description = $"Same query repeated {queryCounts.Count} times (threshold: {_repeatedQueryCount})",
                Metadata = new Dictionary<string, object>
                {
                    ["RepeatCount"] = queryCounts.Count,
                    ["Threshold"] = _repeatedQueryCount
                }
            };
        }

        return null;
    }

    private SuspiciousPattern? DetectOffTopicFlood(Dictionary<string, object> metadata)
    {
        var totalQueries = Convert.ToInt32(metadata.GetValueOrDefault("TotalQueries", 0));
        var offTopicCount = Convert.ToInt32(metadata.GetValueOrDefault("OffTopicCount", 0));

        if (totalQueries <= 5)
        {
            return null; // Need more data
        }

        var offTopicRatio = (double)offTopicCount / totalQueries;

        if (offTopicRatio > _offTopicRatioThreshold)
        {
            return new SuspiciousPattern
            {
                PatternType = PatternType.OffTopicFlood,
                Severity = 0.7,
                Description = $"{offTopicRatio:P0} of queries are off-topic (threshold: {_offTopicRatioThreshold:P0})",
                Metadata = new Dictionary<string, object>
                {
                    ["OffTopicCount"] = offTopicCount,
                    ["TotalQueries"] = totalQueries,
                    ["Ratio"] = offTopicRatio,
                    ["Threshold"] = _offTopicRatioThreshold
                }
            };
        }

        return null;
    }

    private SuspiciousPattern? DetectPromptInjectionAttempts(Dictionary<string, object> metadata)
    {
        var injectionCount = Convert.ToInt32(metadata.GetValueOrDefault("PromptInjectionCount", 0));

        if (injectionCount > _promptInjectionAttempts)
        {
            return new SuspiciousPattern
            {
                PatternType = PatternType.PromptInjectionAttempts,
                Severity = 0.9,
                Description = $"{injectionCount} prompt injection attempts detected (threshold: {_promptInjectionAttempts})",
                Metadata = new Dictionary<string, object>
                {
                    ["InjectionCount"] = injectionCount,
                    ["Threshold"] = _promptInjectionAttempts
                }
            };
        }

        return null;
    }

    private SuspiciousPattern? DetectBulkExtraction(Dictionary<string, object> metadata)
    {
        var largeResultCount = Convert.ToInt32(metadata.GetValueOrDefault("LargeResultCount", 0));

        if (largeResultCount > _largeResultRequests)
        {
            return new SuspiciousPattern
            {
                PatternType = PatternType.BulkExtraction,
                Severity = 0.8,
                Description = $"{largeResultCount} requests with >{_largeResultThreshold} results (threshold: {_largeResultRequests})",
                Metadata = new Dictionary<string, object>
                {
                    ["LargeResultCount"] = largeResultCount,
                    ["ResultThreshold"] = _largeResultThreshold,
                    ["Threshold"] = _largeResultRequests
                }
            };
        }

        return null;
    }

    private RiskLevel CalculateRiskLevel(double maxSeverity)
    {
        return maxSeverity switch
        {
            >= 0.9 => RiskLevel.Critical,
            >= 0.7 => RiskLevel.High,
            >= 0.4 => RiskLevel.Medium,
            _ => RiskLevel.Low
        };
    }

    private string GetRecommendation(RiskLevel riskLevel)
    {
        return riskLevel switch
        {
            RiskLevel.Critical => "Block session immediately and investigate",
            RiskLevel.High => "Restrict access and monitor closely",
            RiskLevel.Medium => "Increase monitoring frequency",
            RiskLevel.Low => "Normal operation - continue monitoring",
            _ => "No action required"
        };
    }

    #endregion
}
