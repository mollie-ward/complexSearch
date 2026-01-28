# Task: Rate Limiting & Abuse Prevention

**Task ID:** 017  
**Feature:** Safety & Content Guardrails  
**Type:** Backend Implementation  
**Priority:** High  
**Estimated Complexity:** Low  
**FRD Reference:** FRD-006 (FR-5, FR-6, FR-7)  
**GitHub Issue:** [#35](https://github.com/mollie-ward/complexSearch/issues/35)

---

## Description

Implement comprehensive rate limiting, abuse monitoring, and logging mechanisms to detect suspicious patterns, prevent system abuse, and maintain audit trails for security analysis.

---

## Dependencies

**Depends on:**
- Task 016: Input Validation & Safety Rules
- Task 012: Session Context Storage (for session tracking)

**Blocks:**
- None (enhances existing functionality)

---

## Technical Requirements

### Abuse Monitoring Service Interface

```csharp
public interface IAbuseMonitoringService
{
    Task<SuspiciousActivityReport> DetectSuspiciousActivityAsync(string sessionId);
    Task LogSecurityEventAsync(SecurityEvent securityEvent);
    Task<AbuseReport> GenerateAbuseReportAsync(string sessionId, TimeSpan timeWindow);
    Task<bool> IsSessionBlockedAsync(string sessionId);
    Task BlockSessionAsync(string sessionId, TimeSpan duration, string reason);
}

public class SuspiciousActivityReport
{
    public string SessionId { get; set; }
    public bool IsSuspicious { get; set; }
    public List<SuspiciousPattern> Patterns { get; set; }
    public RiskLevel RiskLevel { get; set; }
    public string Recommendation { get; set; }
}

public class SuspiciousPattern
{
    public PatternType Type { get; set; }
    public string Description { get; set; }
    public double Severity { get; set; }  // 0-1
}

public enum PatternType
{
    RapidRequests,
    RepeatedQueries,
    BulkExtraction,
    PromptInjectionAttempts,
    OffTopicFlood,
    AbnormalBehavior
}

public enum RiskLevel
{
    Low,
    Medium,
    High,
    Critical
}

public class SecurityEvent
{
    public string EventId { get; set; }
    public DateTime Timestamp { get; set; }
    public string SessionId { get; set; }
    public EventType Type { get; set; }
    public string Description { get; set; }
    public Dictionary<string, object> Metadata { get; set; }
}

public enum EventType
{
    QueryValidationFailed,
    RateLimitExceeded,
    PromptInjectionDetected,
    OffTopicQuery,
    BulkExtractionAttempt,
    SessionBlocked,
    AbnormalActivity
}
```

### Suspicious Activity Detection

**Pattern-based anomaly detection:**

```csharp
public class AbuseMonitoringService : IAbuseMonitoringService
{
    private readonly IConversationSessionService _sessionService;
    private readonly ILogger<AbuseMonitoringService> _logger;
    private readonly IMemoryCache _cache;
    
    public async Task<SuspiciousActivityReport> DetectSuspiciousActivityAsync(string sessionId)
    {
        var session = await _sessionService.GetSessionAsync(sessionId);
        var patterns = new List<SuspiciousPattern>();
        
        // Pattern 1: Rapid requests (> 5 in 10 seconds)
        var recentMessages = session.Messages
            .Where(m => m.Timestamp > DateTime.UtcNow.AddSeconds(-10))
            .Count();
        
        if (recentMessages > 5)
        {
            patterns.Add(new SuspiciousPattern
            {
                Type = PatternType.RapidRequests,
                Description = $"{recentMessages} requests in 10 seconds",
                Severity = Math.Min(1.0, recentMessages / 10.0)
            });
        }
        
        // Pattern 2: Repeated identical queries
        var queryGroups = session.Messages
            .Where(m => m.Role == MessageRole.User)
            .GroupBy(m => m.Content.ToLower())
            .Where(g => g.Count() > 3)
            .ToList();
        
        if (queryGroups.Any())
        {
            patterns.Add(new SuspiciousPattern
            {
                Type = PatternType.RepeatedQueries,
                Description = $"{queryGroups.Count} queries repeated multiple times",
                Severity = 0.6
            });
        }
        
        // Pattern 3: High off-topic query ratio
        var offTopicCount = session.Metadata.GetValueOrDefault("offTopicCount", 0);
        var totalQueries = session.MessageCount / 2;  // Assuming user/assistant pairs
        
        if (totalQueries > 5 && (int)offTopicCount > totalQueries * 0.5)
        {
            patterns.Add(new SuspiciousPattern
            {
                Type = PatternType.OffTopicFlood,
                Description = $"{offTopicCount} off-topic queries out of {totalQueries}",
                Severity = 0.7
            });
        }
        
        // Pattern 4: Repeated prompt injection attempts
        var injectionAttempts = session.Metadata.GetValueOrDefault("promptInjectionAttempts", 0);
        
        if ((int)injectionAttempts > 2)
        {
            patterns.Add(new SuspiciousPattern
            {
                Type = PatternType.PromptInjectionAttempts,
                Description = $"{injectionAttempts} prompt injection attempts",
                Severity = 0.9
            });
        }
        
        // Pattern 5: Bulk extraction indicators
        var largeResultRequests = session.Messages
            .Where(m => m.Results?.TotalCount > 50)
            .Count();
        
        if (largeResultRequests > 3)
        {
            patterns.Add(new SuspiciousPattern
            {
                Type = PatternType.BulkExtraction,
                Description = $"{largeResultRequests} requests returning >50 results",
                Severity = 0.8
            });
        }
        
        // Calculate risk level
        var maxSeverity = patterns.Any() ? patterns.Max(p => p.Severity) : 0.0;
        var riskLevel = maxSeverity switch
        {
            >= 0.9 => RiskLevel.Critical,
            >= 0.7 => RiskLevel.High,
            >= 0.4 => RiskLevel.Medium,
            _ => RiskLevel.Low
        };
        
        var recommendation = riskLevel switch
        {
            RiskLevel.Critical => "Block session immediately",
            RiskLevel.High => "Temporarily restrict access and monitor",
            RiskLevel.Medium => "Increase monitoring and logging",
            _ => "Continue normal operation"
        };
        
        return new SuspiciousActivityReport
        {
            SessionId = sessionId,
            IsSuspicious = patterns.Any(),
            Patterns = patterns,
            RiskLevel = riskLevel,
            Recommendation = recommendation
        };
    }
}
```

### Security Event Logging

**Structured logging with dedicated security log:**

```csharp
public async Task LogSecurityEventAsync(SecurityEvent securityEvent)
{
    securityEvent.EventId = Guid.NewGuid().ToString();
    securityEvent.Timestamp = DateTime.UtcNow;
    
    // Log to structured logger
    _logger.LogWarning(
        "Security Event: {EventType} for session {SessionId} - {Description}",
        securityEvent.Type,
        securityEvent.SessionId,
        securityEvent.Description
    );
    
    // Store in dedicated security event store (optional: database, blob storage)
    await StoreSecurityEventAsync(securityEvent);
    
    // Alert if critical
    if (securityEvent.Type == EventType.PromptInjectionDetected ||
        securityEvent.Type == EventType.BulkExtractionAttempt)
    {
        await SendSecurityAlertAsync(securityEvent);
    }
}

private async Task StoreSecurityEventAsync(SecurityEvent securityEvent)
{
    // Implementation: Store in database or Azure Table Storage
    // For v1: Just structured logging is sufficient
    await Task.CompletedTask;
}

private async Task SendSecurityAlertAsync(SecurityEvent securityEvent)
{
    // Implementation: Send alert via email, Slack, Teams, etc.
    _logger.LogCritical(
        "SECURITY ALERT: {EventType} detected for session {SessionId}",
        securityEvent.Type,
        securityEvent.SessionId
    );
    await Task.CompletedTask;
}
```

### Session Blocking

**Temporary and permanent session blocking:**

```csharp
public async Task BlockSessionAsync(string sessionId, TimeSpan duration, string reason)
{
    var blockKey = $"blocked:{sessionId}";
    var blockInfo = new
    {
        SessionId = sessionId,
        BlockedAt = DateTime.UtcNow,
        Duration = duration,
        Reason = reason,
        ExpiresAt = DateTime.UtcNow.Add(duration)
    };
    
    _cache.Set(blockKey, blockInfo, duration);
    
    await LogSecurityEventAsync(new SecurityEvent
    {
        SessionId = sessionId,
        Type = EventType.SessionBlocked,
        Description = $"Session blocked for {duration.TotalMinutes} minutes: {reason}"
    });
    
    _logger.LogWarning(
        "Session {SessionId} blocked for {Duration} minutes: {Reason}",
        sessionId,
        duration.TotalMinutes,
        reason
    );
}

public async Task<bool> IsSessionBlockedAsync(string sessionId)
{
    var blockKey = $"blocked:{sessionId}";
    return _cache.TryGetValue(blockKey, out _);
}
```

### Abuse Report Generation

**Generate comprehensive abuse reports:**

```csharp
public async Task<AbuseReport> GenerateAbuseReportAsync(string sessionId, TimeSpan timeWindow)
{
    var session = await _sessionService.GetSessionAsync(sessionId);
    var suspiciousActivity = await DetectSuspiciousActivityAsync(sessionId);
    
    var windowStart = DateTime.UtcNow.Subtract(timeWindow);
    var recentMessages = session.Messages.Where(m => m.Timestamp >= windowStart).ToList();
    
    return new AbuseReport
    {
        SessionId = sessionId,
        TimeWindow = timeWindow,
        TotalQueries = recentMessages.Count(m => m.Role == MessageRole.User),
        OffTopicQueries = (int)session.Metadata.GetValueOrDefault("offTopicCount", 0),
        PromptInjectionAttempts = (int)session.Metadata.GetValueOrDefault("promptInjectionAttempts", 0),
        RateLimitViolations = (int)session.Metadata.GetValueOrDefault("rateLimitViolations", 0),
        SuspiciousPatterns = suspiciousActivity.Patterns,
        RiskLevel = suspiciousActivity.RiskLevel,
        Recommendation = suspiciousActivity.Recommendation
    };
}

public class AbuseReport
{
    public string SessionId { get; set; }
    public TimeSpan TimeWindow { get; set; }
    public int TotalQueries { get; set; }
    public int OffTopicQueries { get; set; }
    public int PromptInjectionAttempts { get; set; }
    public int RateLimitViolations { get; set; }
    public List<SuspiciousPattern> SuspiciousPatterns { get; set; }
    public RiskLevel RiskLevel { get; set; }
    public string Recommendation { get; set; }
}
```

### Middleware Integration

**Check for blocked sessions:**

```csharp
public class SessionBlockingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IAbuseMonitoringService _abuseMonitoring;
    
    public async Task InvokeAsync(HttpContext context)
    {
        var sessionId = context.Request.Headers["X-Session-Id"].FirstOrDefault();
        
        if (!string.IsNullOrEmpty(sessionId))
        {
            if (await _abuseMonitoring.IsSessionBlockedAsync(sessionId))
            {
                context.Response.StatusCode = 403;  // Forbidden
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "Session is temporarily blocked due to suspicious activity",
                    sessionId = sessionId
                });
                return;
            }
            
            // Check for suspicious activity
            var suspiciousActivity = await _abuseMonitoring.DetectSuspiciousActivityAsync(sessionId);
            
            if (suspiciousActivity.RiskLevel == RiskLevel.Critical)
            {
                await _abuseMonitoring.BlockSessionAsync(
                    sessionId,
                    TimeSpan.FromHours(1),
                    $"Critical risk detected: {string.Join(", ", suspiciousActivity.Patterns.Select(p => p.Type))}"
                );
                
                context.Response.StatusCode = 403;
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "Session blocked due to critical security violations",
                    patterns = suspiciousActivity.Patterns.Select(p => p.Type.ToString())
                });
                return;
            }
        }
        
        await _next(context);
    }
}
```

### Metrics & Dashboards

**Export metrics for monitoring:**

```csharp
public class SecurityMetricsService
{
    public SecurityMetrics GetMetrics(TimeSpan window)
    {
        // Implementation would query security event store
        return new SecurityMetrics
        {
            TimeWindow = window,
            TotalQueries = 0,  // To be implemented
            BlockedSessions = 0,
            PromptInjectionAttempts = 0,
            OffTopicQueries = 0,
            RateLimitViolations = 0,
            TopViolations = new Dictionary<EventType, int>()
        };
    }
}

public class SecurityMetrics
{
    public TimeSpan TimeWindow { get; set; }
    public int TotalQueries { get; set; }
    public int BlockedSessions { get; set; }
    public int PromptInjectionAttempts { get; set; }
    public int OffTopicQueries { get; set; }
    public int RateLimitViolations { get; set; }
    public Dictionary<EventType, int> TopViolations { get; set; }
}
```

---

## Acceptance Criteria

### Functional Criteria

✅ **Abuse Detection:**
- [ ] All 5 suspicious patterns detected
- [ ] Risk levels calculated correctly
- [ ] Recommendations appropriate

✅ **Event Logging:**
- [ ] All security events logged
- [ ] Structured logging format
- [ ] Critical events trigger alerts

✅ **Session Blocking:**
- [ ] Sessions blocked on critical risk
- [ ] Blocked sessions cannot make requests
- [ ] Block expires after duration

✅ **Abuse Reports:**
- [ ] Reports generated correctly
- [ ] All metrics included
- [ ] Time windows respected

### Technical Criteria

✅ **Performance:**
- [ ] Abuse detection <100ms
- [ ] Minimal performance impact (<5%)

✅ **Reliability:**
- [ ] No false positives for normal usage
- [ ] All actual abuse detected (≥95%)

---

## Testing Requirements

### Unit Tests

**Test Coverage:** ≥85%

**Test Cases:**
```csharp
[Fact]
public async Task DetectSuspiciousActivity_RapidRequests_ReturnsHighRisk()

[Fact]
public async Task DetectSuspiciousActivity_RepeatedQueries_DetectsPattern()

[Fact]
public async Task BlockSession_ValidDuration_BlocksSession()

[Fact]
public async Task IsSessionBlocked_BlockedSession_ReturnsTrue()

[Fact]
public async Task GenerateAbuseReport_VariousViolations_IncludesAll()
```

### Integration Tests

- [ ] Test abuse detection with simulated attacks
- [ ] Test session blocking end-to-end
- [ ] Test middleware integration
- [ ] Load test with monitoring enabled

---

## Definition of Done

- [ ] Abuse monitoring service implemented
- [ ] Suspicious activity detection working
- [ ] Security event logging functional
- [ ] Session blocking implemented
- [ ] Abuse reporting working
- [ ] Middleware integrated
- [ ] All unit tests pass (≥85% coverage)
- [ ] Integration tests pass
- [ ] Documentation updated
- [ ] Code reviewed and approved

---

## Related Files

**Created/Modified:**
- `src/VehicleSearch.Infrastructure/Safety/AbuseMonitoringService.cs`
- `src/VehicleSearch.Infrastructure/Safety/SecurityMetricsService.cs`
- `src/VehicleSearch.Core/Interfaces/IAbuseMonitoringService.cs`
- `src/VehicleSearch.Core/Models/SuspiciousActivityReport.cs`
- `src/VehicleSearch.Api/Middleware/SessionBlockingMiddleware.cs`
- `tests/VehicleSearch.Infrastructure.Tests/AbuseMonitoringServiceTests.cs`

**References:**
- FRD-006: Safety & Content Guardrails (FR-5, FR-6, FR-7)
- Task 016: Input validation
