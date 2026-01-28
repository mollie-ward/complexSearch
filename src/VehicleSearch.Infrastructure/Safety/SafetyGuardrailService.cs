using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using VehicleSearch.Core.Enums;
using VehicleSearch.Core.Interfaces;
using VehicleSearch.Core.Models;

namespace VehicleSearch.Infrastructure.Safety;

/// <summary>
/// Service for comprehensive input validation, safety guardrails, and abuse prevention.
/// </summary>
public class SafetyGuardrailService : ISafetyGuardrailService
{
    private readonly ILogger<SafetyGuardrailService> _logger;
    private readonly IMemoryCache _cache;
    private const int MaxLength = 500;
    private const int MinLength = 2;
    private const int RequestsPerMinute = 10;
    private const int RequestsPerHour = 100;

    // Vehicle-related keywords (positive indicators)
    private static readonly string[] VehicleKeywords = new[]
    {
        "car", "vehicle", "bmw", "audi", "mercedes", "toyota", "ford", "honda", "nissan",
        "suv", "sedan", "hatchback", "estate", "coupe", "convertible", "truck", "van",
        "mileage", "engine", "transmission", "petrol", "diesel", "electric",
        "features", "leather", "navigation", "parking", "automatic", "manual",
        "horsepower", "mpg", "warranty", "km", "miles", "used", "new", "driving"
    };

    // Weak vehicle keywords that can appear in non-vehicle contexts
    private static readonly string[] WeakVehicleKeywords = new[]
    {
        "price", "show", "find", "get"
    };

    // Off-topic keywords (negative indicators)
    private static readonly string[] OffTopicKeywords = new[]
    {
        "weather", "news", "recipe", "movie", "music", "song", "album", "pizza",
        "sports", "football", "basketball", "politics", "election",
        "stock", "crypto", "bitcoin", "ethereum", "investment",
        "restaurant", "hotel", "vacation", "flight", "travel", "make", "cook"
    };

    // SQL injection patterns
    private static readonly Regex[] SqlPatterns = new[]
    {
        new Regex(@"(\bOR\b|\bAND\b)\s*\d+\s*=\s*\d+", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"';\s*--", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"\bUNION\s+SELECT\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"\bDROP\s+TABLE\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"\bINSERT\s+INTO\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"\bDELETE\s+FROM\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"\bEXEC\s*\(", RegexOptions.IgnoreCase | RegexOptions.Compiled)
    };

    // Prompt injection patterns (excluding bulk data patterns which are handled separately)
    private static readonly Regex[] InjectionPatterns = new[]
    {
        // System prompt overrides
        new Regex(@"ignore\s+.*\s*instructions?", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"ignore\s+.*\s*prompts?", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"you\s+are\s+now", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"new\s+instructions?", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"disregard.*instructions?", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        
        // Role manipulation
        new Regex(@"\bact\s+as\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"pretend\s+(you\s+are|to\s+be)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"\broleplay\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        
        // Information extraction
        new Regex(@"show\s+me\s+(your|the)\s+(system\s+prompt|instructions?)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"what\s+are\s+your\s+(rules?|guidelines?|instructions?)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"reveal.*prompt", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        
        // Jailbreak attempts
        new Regex(@"\bDAN\s+mode\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"developer\s+mode", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"\bjailbreak\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        
        // Database/system extraction
        new Regex(@"dump\s+(database|index)", RegexOptions.IgnoreCase | RegexOptions.Compiled)
    };

    // Bulk extraction patterns (data extraction attempts)
    private static readonly Regex[] BulkPatterns = new[]
    {
        new Regex(@"(list|show(\s+me)?|give\s+me)\s+all\s+(vehicles?|cars?|data)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"(list|show(\s+me)?)\s+all\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"give\s+me\s+(everything|all(\s+the)?\s+data)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"every\s+(car|vehicle)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"\d{2,}\s+(cars?|vehicles?|results?)", RegexOptions.IgnoreCase | RegexOptions.Compiled)
    };

    public SafetyGuardrailService(ILogger<SafetyGuardrailService> logger, IMemoryCache cache)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    /// <inheritdoc/>
    public async Task<SafetyValidationResult> ValidateQueryAsync(string query, string sessionId, CancellationToken cancellationToken = default)
    {
        // 1. Length validation
        var lengthResult = ValidateQueryLength(query);
        if (!lengthResult.IsValid)
        {
            return lengthResult;
        }

        // 2. Character validation
        var charResult = ValidateCharacters(query);
        if (!charResult.IsValid)
        {
            return charResult;
        }

        // 3. Bulk extraction detection (check before prompt injection to avoid false positives)
        if (IsBulkExtractionAttempt(query))
        {
            var safeQuery = query.Length > 50 ? query.Substring(0, 50) + "..." : query;
            _logger.LogWarning("Bulk extraction attempt detected in query: {Query}", safeQuery);
            return new SafetyValidationResult
            {
                IsValid = false,
                ViolationType = SafetyViolationType.BulkExtraction,
                Message = "This query appears to be attempting bulk data extraction. Please refine your search criteria."
            };
        }

        // 4. Prompt injection detection
        if (await ContainsPromptInjectionAsync(query, cancellationToken))
        {
            var safeQuery = query.Length > 50 ? query.Substring(0, 50) + "..." : query;
            _logger.LogWarning("Prompt injection detected in query: {Query}", safeQuery);
            return new SafetyValidationResult
            {
                IsValid = false,
                ViolationType = SafetyViolationType.PromptInjection,
                Message = "Query contains potentially malicious content and cannot be processed."
            };
        }

        // 5. Off-topic detection
        if (await IsOffTopicAsync(query, cancellationToken))
        {
            return new SafetyValidationResult
            {
                IsValid = false,
                ViolationType = SafetyViolationType.OffTopic,
                Message = "Query is not related to vehicle search. Please ask about cars or vehicles."
            };
        }

        // 6. Rate limiting
        var rateLimitResult = await CheckRateLimitAsync(sessionId, cancellationToken);
        if (!rateLimitResult.IsAllowed)
        {
            return new SafetyValidationResult
            {
                IsValid = false,
                ViolationType = SafetyViolationType.RateLimitExceeded,
                Message = $"Rate limit exceeded. Please try again in {rateLimitResult.RetryAfter.TotalSeconds:F0} seconds."
            };
        }

        return new SafetyValidationResult { IsValid = true };
    }

    /// <inheritdoc/>
    public Task<bool> IsOffTopicAsync(string query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Task.FromResult(false);
        }

        var lowerQuery = query.ToLower();

        // Check for vehicle-related keywords (use word boundaries to avoid partial matches)
        var hasVehicleKeyword = VehicleKeywords.Any(k => 
            Regex.IsMatch(lowerQuery, $@"\b{Regex.Escape(k)}\b", RegexOptions.IgnoreCase));
        var hasWeakVehicleKeyword = WeakVehicleKeywords.Any(k => 
            Regex.IsMatch(lowerQuery, $@"\b{Regex.Escape(k)}\b", RegexOptions.IgnoreCase));

        // Check for off-topic keywords (use word boundaries)
        var hasOffTopicKeyword = OffTopicKeywords.Any(k => 
            Regex.IsMatch(lowerQuery, $@"\b{Regex.Escape(k)}\b", RegexOptions.IgnoreCase));

        // If has off-topic keywords and no strong vehicle keywords, it's likely off-topic
        // Weak keywords don't count when there are off-topic keywords present
        if (hasOffTopicKeyword && !hasVehicleKeyword)
        {
            return Task.FromResult(true);
        }

        // If query is long and has no vehicle keywords at all, consider it potentially off-topic
        if (query.Length > 20 && !hasVehicleKeyword && !hasWeakVehicleKeyword)
        {
            // Check if query contains any numbers (often indicates vehicle specs)
            var hasNumbers = query.Any(char.IsDigit);
            if (!hasNumbers)
            {
                // Could enhance with LLM check here for edge cases
                // For now, allow it through to avoid false positives
                return Task.FromResult(false);
            }
        }

        return Task.FromResult(false);
    }

    /// <inheritdoc/>
    public Task<bool> ContainsPromptInjectionAsync(string query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Task.FromResult(false);
        }

        foreach (var pattern in InjectionPatterns)
        {
            if (pattern.IsMatch(query))
            {
                _logger.LogWarning("Potential prompt injection detected with pattern matching");
                return Task.FromResult(true);
            }
        }

        return Task.FromResult(false);
    }

    /// <inheritdoc/>
    public Task<bool> ContainsInappropriateContentAsync(string query, CancellationToken cancellationToken = default)
    {
        // This is a placeholder for content moderation
        // In production, integrate with Azure Content Safety or similar service
        return Task.FromResult(false);
    }

    /// <inheritdoc/>
    public Task<RateLimitResult> CheckRateLimitAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            sessionId = "anonymous";
        }

        var minuteKey = $"ratelimit:minute:{sessionId}";
        var hourKey = $"ratelimit:hour:{sessionId}";

        // Use GetOrAdd with lock to prevent race conditions
        var minuteCount = _cache.GetOrCreate<int>(minuteKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
            return 0;
        });

        var hourCount = _cache.GetOrCreate<int>(hourKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
            return 0;
        });

        if (minuteCount >= RequestsPerMinute)
        {
            _logger.LogWarning("Rate limit exceeded for session {SessionId}: {Count} requests/minute", sessionId, minuteCount);
            return Task.FromResult(new RateLimitResult
            {
                IsAllowed = false,
                RemainingRequests = 0,
                RetryAfter = TimeSpan.FromSeconds(60)
            });
        }

        if (hourCount >= RequestsPerHour)
        {
            _logger.LogWarning("Rate limit exceeded for session {SessionId}: {Count} requests/hour", sessionId, hourCount);
            return Task.FromResult(new RateLimitResult
            {
                IsAllowed = false,
                RemainingRequests = 0,
                RetryAfter = TimeSpan.FromHours(1)
            });
        }

        // Increment counters atomically
        // Note: For high-concurrency scenarios, consider using IDistributedCache with Redis
        lock (_cache)
        {
            _cache.Set(minuteKey, minuteCount + 1, TimeSpan.FromMinutes(1));
            _cache.Set(hourKey, hourCount + 1, TimeSpan.FromHours(1));
        }

        return Task.FromResult(new RateLimitResult
        {
            IsAllowed = true,
            RemainingRequests = Math.Min(
                RequestsPerMinute - minuteCount - 1,
                RequestsPerHour - hourCount - 1
            )
        });
    }

    private SafetyValidationResult ValidateQueryLength(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return new SafetyValidationResult
            {
                IsValid = false,
                Errors = new List<ValidationError>
                {
                    new()
                    {
                        FieldName = "query",
                        Message = "Query cannot be empty or contain only whitespace"
                    }
                },
                Message = "Query cannot be empty or contain only whitespace"
            };
        }

        if (query.Length > MaxLength)
        {
            return new SafetyValidationResult
            {
                IsValid = false,
                ViolationType = SafetyViolationType.ExcessiveLength,
                Errors = new List<ValidationError>
                {
                    new()
                    {
                        FieldName = "query",
                        Message = $"Query exceeds maximum length of {MaxLength} characters"
                    }
                },
                Message = $"Query exceeds maximum length of {MaxLength} characters"
            };
        }

        if (query.Length < MinLength)
        {
            return new SafetyValidationResult
            {
                IsValid = false,
                Errors = new List<ValidationError>
                {
                    new()
                    {
                        FieldName = "query",
                        Message = $"Query must be at least {MinLength} characters"
                    }
                },
                Message = $"Query must be at least {MinLength} characters"
            };
        }

        return new SafetyValidationResult { IsValid = true };
    }

    private SafetyValidationResult ValidateCharacters(string query)
    {
        // Block SQL injection patterns
        foreach (var pattern in SqlPatterns)
        {
            if (pattern.IsMatch(query))
            {
                var safeQuery = query.Length > 50 ? query.Substring(0, 50) + "..." : query;
                _logger.LogWarning("SQL injection pattern detected in query: {Query}", safeQuery);
                return new SafetyValidationResult
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
            _logger.LogWarning("Excessive special characters detected: {Count}/{Total}", specialCharCount, query.Length);
            return new SafetyValidationResult
            {
                IsValid = false,
                ViolationType = SafetyViolationType.InvalidCharacters,
                Message = "Query contains excessive special characters"
            };
        }

        return new SafetyValidationResult { IsValid = true };
    }

    private bool IsBulkExtractionAttempt(string query)
    {
        foreach (var pattern in BulkPatterns)
        {
            if (pattern.IsMatch(query))
            {
                return true;
            }
        }

        return false;
    }
}
