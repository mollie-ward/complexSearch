namespace VehicleSearch.Core.Enums;

/// <summary>
/// Types of safety violations that can be detected.
/// </summary>
public enum SafetyViolationType
{
    /// <summary>
    /// Query is not related to vehicles.
    /// </summary>
    OffTopic,

    /// <summary>
    /// Query contains potential prompt injection patterns.
    /// </summary>
    PromptInjection,

    /// <summary>
    /// Query contains inappropriate content.
    /// </summary>
    InappropriateContent,

    /// <summary>
    /// Query exceeds maximum allowed length.
    /// </summary>
    ExcessiveLength,

    /// <summary>
    /// Query contains invalid or potentially malicious characters.
    /// </summary>
    InvalidCharacters,

    /// <summary>
    /// Rate limit has been exceeded.
    /// </summary>
    RateLimitExceeded,

    /// <summary>
    /// Potential bulk data extraction attempt detected.
    /// </summary>
    BulkExtraction
}
