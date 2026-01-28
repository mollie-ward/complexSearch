# Task: Input Validation & Safety Rules

**Task ID:** 016  
**Feature:** Safety & Content Guardrails  
**Type:** Backend Implementation  
**Priority:** Critical  
**Estimated Complexity:** Medium  
**FRD Reference:** FRD-006 (FR-1, FR-2, FR-3, FR-4)  
**GitHub Issue:** [#33](https://github.com/mollie-ward/complexSearch/issues/33)

---

## Description

Implement comprehensive input validation, safety guardrails, and abuse prevention mechanisms to ensure queries are appropriate, detect malicious inputs, and prevent system misuse.

---

## Dependencies

**Depends on:**
- Task 001: Backend API Scaffolding
- Task 007: Query Intent Classification (for off-topic detection)

**Blocks:**
- All API endpoints (safety is prerequisite)

---

## Technical Requirements

### Safety Service Interface

```csharp
public interface ISafetyGuardrailService
{
    Task<ValidationResult> ValidateQueryAsync(string query);
    Task<bool> IsOffTopicAsync(string query);
    Task<bool> ContainsPromptInjectionAsync(string query);
    Task<bool> ContainsInappropriateContentAsync(string query);
    Task<RateLimitResult> CheckRateLimitAsync(string sessionId);
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<ValidationError> Errors { get; set; }
    public SafetyViolationType? ViolationType { get; set; }
    public string Message { get; set; }
}

public class ValidationError
{
    public string Field { get; set; }
    public string ErrorCode { get; set; }
    public string Message { get; set; }
}

public enum SafetyViolationType
{
    OffTopic,
    PromptInjection,
    InappropriateContent,
    ExcessiveLength,
    InvalidCharacters,
    RateLimitExceeded,
    BulkExtraction
}

public class RateLimitResult
{
    public bool IsAllowed { get; set; }
    public int RemainingRequests { get; set; }
    public TimeSpan RetryAfter { get; set; }
}
```

### Input Length Validation

**Enforce query length limits:**

```csharp
public async Task<ValidationResult> ValidateQueryLengthAsync(string query)
{
    const int MaxLength = 500;
    const int MinLength = 2;
    
    if (string.IsNullOrWhiteSpace(query))
    {
        return new ValidationResult
        {
            IsValid = false,
            Errors = new List<ValidationError>
            {
                new()
                {
                    Field = "query",
                    ErrorCode = "QUERY_EMPTY",
                    Message = "Query cannot be empty"
                }
            }
        };
    }
    
    if (query.Length > MaxLength)
    {
        return new ValidationResult
        {
            IsValid = false,
            ViolationType = SafetyViolationType.ExcessiveLength,
            Errors = new List<ValidationError>
            {
                new()
                {
                    Field = "query",
                    ErrorCode = "QUERY_TOO_LONG",
                    Message = $"Query exceeds maximum length of {MaxLength} characters"
                }
            }
        };
    }
    
    if (query.Length < MinLength)
    {
        return new ValidationResult
        {
            IsValid = false,
            Errors = new List<ValidationError>
            {
                new()
                {
                    Field = "query",
                    ErrorCode = "QUERY_TOO_SHORT",
                    Message = $"Query must be at least {MinLength} characters"
                }
            }
        };
    }
    
    return new ValidationResult { IsValid = true };
}
```

### Character Validation

**Block dangerous characters and patterns:**

```csharp
public async Task<ValidationResult> ValidateCharactersAsync(string query)
{
    // Block SQL injection patterns
    var sqlPatterns = new[]
    {
        @"(\bOR\b|\bAND\b).*=",
        @"';--",
        @"UNION\s+SELECT",
        @"DROP\s+TABLE",
        @"INSERT\s+INTO",
        @"DELETE\s+FROM"
    };
    
    foreach (var pattern in sqlPatterns)
    {
        if (Regex.IsMatch(query, pattern, RegexOptions.IgnoreCase))
        {
            return new ValidationResult
            {
                IsValid = false,
                ViolationType = SafetyViolationType.InvalidCharacters,
                Message = "Query contains potentially malicious patterns"
            };
        }
    }
    
    // Block excessive special characters (potential obfuscation)
    var specialCharCount = query.Count(c => !char.IsLetterOrDigit(c) && !char.IsWhiteSpace(c));
    if (specialCharCount > query.Length * 0.3)  // > 30% special chars
    {
        return new ValidationResult
        {
            IsValid = false,
            ViolationType = SafetyViolationType.InvalidCharacters,
            Message = "Query contains excessive special characters"
        };
    }
    
    return new ValidationResult { IsValid = true };
}
```

### Off-Topic Detection

**Detect non-vehicle queries:**

```csharp
public async Task<bool> IsOffTopicAsync(string query)
{
    var lowerQuery = query.ToLower();
    
    // Vehicle-related keywords (positive indicators)
    var vehicleKeywords = new[]
    {
        "car", "vehicle", "bmw", "audi", "mercedes", "toyota", "ford",
        "suv", "sedan", "hatchback", "estate", "coupe",
        "mileage", "price", "engine", "transmission", "petrol", "diesel",
        "features", "leather", "navigation", "parking"
    };
    
    // Off-topic keywords (negative indicators)
    var offTopicKeywords = new[]
    {
        "weather", "news", "recipe", "movie", "music",
        "sports", "politics", "stock", "crypto", "bitcoin"
    };
    
    var hasVehicleKeyword = vehicleKeywords.Any(k => lowerQuery.Contains(k));
    var hasOffTopicKeyword = offTopicKeywords.Any(k => lowerQuery.Contains(k));
    
    // If no vehicle keywords and has off-topic keywords
    if (!hasVehicleKeyword && hasOffTopicKeyword)
    {
        return true;
    }
    
    // Use LLM for advanced detection (optional, for edge cases)
    if (!hasVehicleKeyword && query.Length > 20)
    {
        return await DetectOffTopicWithLLMAsync(query);
    }
    
    return false;
}

private async Task<bool> DetectOffTopicWithLLMAsync(string query)
{
    var prompt = $@"
Determine if the following query is related to vehicles, cars, or automotive topics.
Respond with 'YES' if it's vehicle-related, 'NO' if it's off-topic.

Query: ""{query}""

Response:";

    var response = await _llmService.GenerateAsync(prompt);
    return response.Trim().ToUpper().StartsWith("NO");
}
```

### Prompt Injection Detection

**Detect attempts to manipulate the system:**

```csharp
public async Task<bool> ContainsPromptInjectionAsync(string query)
{
    var injectionPatterns = new[]
    {
        // System prompt overrides
        @"ignore (previous|above|all) (instructions|prompts)",
        @"you are now",
        @"new instructions",
        @"disregard.*instructions",
        
        // Role manipulation
        @"act as",
        @"pretend (you are|to be)",
        @"roleplay",
        
        // Information extraction
        @"show me (your|the) (system prompt|instructions)",
        @"what are your (rules|guidelines|instructions)",
        @"reveal.*prompt",
        
        // Jailbreak attempts
        @"DAN mode",
        @"developer mode",
        @"jailbreak",
        
        // Data extraction
        @"list all (vehicles|cars|data)",
        @"give me (everything|all data)",
        @"dump (database|index)"
    };
    
    foreach (var pattern in injectionPatterns)
    {
        if (Regex.IsMatch(query, pattern, RegexOptions.IgnoreCase))
        {
            _logger.LogWarning(
                "Potential prompt injection detected: {Query}",
                query
            );
            return true;
        }
    }
    
    return false;
}
```

### Rate Limiting

**Prevent abuse with rate limits:**

```csharp
public class RateLimitingService : IRateLimitingService
{
    private readonly IMemoryCache _cache;
    private const int RequestsPerMinute = 10;
    private const int RequestsPerHour = 100;
    
    public async Task<RateLimitResult> CheckRateLimitAsync(string sessionId)
    {
        var minuteKey = $"ratelimit:minute:{sessionId}";
        var hourKey = $"ratelimit:hour:{sessionId}";
        
        var minuteCount = _cache.GetOrCreate(minuteKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
            return 0;
        });
        
        var hourCount = _cache.GetOrCreate(hourKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
            return 0;
        });
        
        if (minuteCount >= RequestsPerMinute)
        {
            return new RateLimitResult
            {
                IsAllowed = false,
                RemainingRequests = 0,
                RetryAfter = TimeSpan.FromSeconds(60)
            };
        }
        
        if (hourCount >= RequestsPerHour)
        {
            return new RateLimitResult
            {
                IsAllowed = false,
                RemainingRequests = 0,
                RetryAfter = TimeSpan.FromHours(1)
            };
        }
        
        // Increment counters
        _cache.Set(minuteKey, minuteCount + 1, TimeSpan.FromMinutes(1));
        _cache.Set(hourKey, hourCount + 1, TimeSpan.FromHours(1));
        
        return new RateLimitResult
        {
            IsAllowed = true,
            RemainingRequests = Math.Min(
                RequestsPerMinute - minuteCount - 1,
                RequestsPerHour - hourCount - 1
            )
        };
    }
}
```

### Bulk Data Extraction Prevention

**Detect and block bulk extraction attempts:**

```csharp
public async Task<bool> IsBulkExtractionAttemptAsync(string query, ConversationSession session)
{
    // Check for patterns indicating bulk extraction
    var bulkPatterns = new[]
    {
        @"(list|show|give me) all",
        @"every (car|vehicle)",
        @"\d{2,}\s+(cars|vehicles|results)"  // "100 cars"
    };
    
    foreach (var pattern in bulkPatterns)
    {
        if (Regex.IsMatch(query, pattern, RegexOptions.IgnoreCase))
        {
            return true;
        }
    }
    
    // Check for rapid sequential queries (potential scraping)
    var recentMessages = session.Messages
        .Where(m => m.Timestamp > DateTime.UtcNow.AddMinutes(-5))
        .Count();
    
    if (recentMessages > 20)  // > 20 queries in 5 minutes
    {
        _logger.LogWarning(
            "Potential bulk extraction: {Count} queries in 5 minutes for session {SessionId}",
            recentMessages,
            session.SessionId
        );
        return true;
    }
    
    return false;
}
```

### Comprehensive Validation Pipeline

**Orchestrate all validations:**

```csharp
public async Task<ValidationResult> ValidateQueryAsync(string query, string sessionId)
{
    // 1. Length validation
    var lengthResult = await ValidateQueryLengthAsync(query);
    if (!lengthResult.IsValid) return lengthResult;
    
    // 2. Character validation
    var charResult = await ValidateCharactersAsync(query);
    if (!charResult.IsValid) return charResult;
    
    // 3. Prompt injection detection
    if (await ContainsPromptInjectionAsync(query))
    {
        return new ValidationResult
        {
            IsValid = false,
            ViolationType = SafetyViolationType.PromptInjection,
            Message = "Query contains potentially malicious content"
        };
    }
    
    // 4. Off-topic detection
    if (await IsOffTopicAsync(query))
    {
        return new ValidationResult
        {
            IsValid = false,
            ViolationType = SafetyViolationType.OffTopic,
            Message = "Query is not related to vehicle search. Please ask about cars or vehicles."
        };
    }
    
    // 5. Rate limiting
    var rateLimitResult = await CheckRateLimitAsync(sessionId);
    if (!rateLimitResult.IsAllowed)
    {
        return new ValidationResult
        {
            IsValid = false,
            ViolationType = SafetyViolationType.RateLimitExceeded,
            Message = $"Rate limit exceeded. Please try again in {rateLimitResult.RetryAfter.TotalSeconds} seconds."
        };
    }
    
    return new ValidationResult { IsValid = true };
}
```

### API Middleware

**Apply validation to all requests:**

```csharp
public class SafetyGuardrailMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ISafetyGuardrailService _safetyService;
    
    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/api/v1/search") ||
            context.Request.Path.StartsWithSegments("/api/v1/query"))
        {
            // Extract query from request
            var query = await ExtractQueryFromRequest(context.Request);
            var sessionId = context.Request.Headers["X-Session-Id"].FirstOrDefault() ?? "anonymous";
            
            // Validate
            var result = await _safetyService.ValidateQueryAsync(query, sessionId);
            
            if (!result.IsValid)
            {
                context.Response.StatusCode = 400;  // Bad Request
                await context.Response.WriteAsJsonAsync(new
                {
                    error = result.Message,
                    violationType = result.ViolationType?.ToString()
                });
                return;
            }
        }
        
        await _next(context);
    }
}
```

---

## Acceptance Criteria

### Functional Criteria

✅ **Input Validation:**
- [ ] Queries <2 chars rejected
- [ ] Queries >500 chars rejected
- [ ] Excessive special characters blocked
- [ ] SQL injection patterns blocked

✅ **Off-Topic Detection:**
- [ ] Non-vehicle queries flagged (≥90% accuracy)
- [ ] Vehicle queries allowed (≥95% accuracy)
- [ ] Edge cases handled

✅ **Prompt Injection:**
- [ ] All injection patterns detected
- [ ] Malicious queries blocked
- [ ] Attempts logged

✅ **Rate Limiting:**
- [ ] 10 requests/minute enforced
- [ ] 100 requests/hour enforced
- [ ] Retry-After header returned

✅ **Bulk Extraction:**
- [ ] "List all" queries blocked
- [ ] Rapid sequential queries detected
- [ ] Attempts logged

### Technical Criteria

✅ **Performance:**
- [ ] Validation <50ms
- [ ] No false positives (≥98%)
- [ ] <2% false negatives

---

## Testing Requirements

### Unit Tests

**Test Coverage:** ≥85%

**Test Cases:**
```csharp
[Fact]
public async Task ValidateQuery_TooLong_ReturnsInvalid()

[Fact]
public async Task ValidateQuery_SQLInjection_ReturnsInvalid()

[Fact]
public async Task IsOffTopic_WeatherQuery_ReturnsTrue()

[Fact]
public async Task IsOffTopic_VehicleQuery_ReturnsFalse()

[Fact]
public async Task ContainsPromptInjection_IgnoreInstructions_ReturnsTrue()

[Fact]
public async Task CheckRateLimit_ExceedsLimit_ReturnsFalse()

[Fact]
public async Task IsBulkExtraction_ListAll_ReturnsTrue()
```

### Integration Tests

- [ ] Test all validation scenarios
- [ ] Test rate limiting under load
- [ ] Test middleware integration
- [ ] Red team testing (adversarial inputs)

---

## Definition of Done

- [ ] Safety guardrail service implemented
- [ ] All validations working
- [ ] Off-topic detection functional (≥90%)
- [ ] Prompt injection detection functional
- [ ] Rate limiting enforced
- [ ] Middleware integrated
- [ ] All unit tests pass (≥85% coverage)
- [ ] Red team testing passed
- [ ] Documentation updated
- [ ] Code reviewed and approved

---

## Related Files

**Created/Modified:**
- `src/VehicleSearch.Infrastructure/Safety/SafetyGuardrailService.cs`
- `src/VehicleSearch.Infrastructure/Safety/RateLimitingService.cs`
- `src/VehicleSearch.Core/Interfaces/ISafetyGuardrailService.cs`
- `src/VehicleSearch.Core/Models/ValidationResult.cs`
- `src/VehicleSearch.Api/Middleware/SafetyGuardrailMiddleware.cs`
- `tests/VehicleSearch.Infrastructure.Tests/SafetyGuardrailServiceTests.cs`

**References:**
- FRD-006: Safety & Content Guardrails (FR-1, FR-2, FR-3, FR-4)
