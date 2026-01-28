namespace VehicleSearch.Core.Enums;

/// <summary>
/// Types of security events that can be logged.
/// </summary>
public enum EventType
{
    /// <summary>
    /// Query validation failed due to safety checks.
    /// </summary>
    QueryValidationFailed,

    /// <summary>
    /// Rate limit was exceeded.
    /// </summary>
    RateLimitExceeded,

    /// <summary>
    /// Prompt injection pattern detected.
    /// </summary>
    PromptInjectionDetected,

    /// <summary>
    /// Off-topic query detected.
    /// </summary>
    OffTopicQuery,

    /// <summary>
    /// Bulk data extraction attempt detected.
    /// </summary>
    BulkExtractionAttempt,

    /// <summary>
    /// Session was blocked due to suspicious activity.
    /// </summary>
    SessionBlocked,

    /// <summary>
    /// Abnormal activity pattern detected.
    /// </summary>
    AbnormalActivity
}
