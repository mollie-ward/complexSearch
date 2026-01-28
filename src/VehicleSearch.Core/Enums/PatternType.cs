namespace VehicleSearch.Core.Enums;

/// <summary>
/// Types of suspicious patterns that can be detected in user behavior.
/// </summary>
public enum PatternType
{
    /// <summary>
    /// Multiple rapid requests in a short time window.
    /// </summary>
    RapidRequests,

    /// <summary>
    /// Same query repeated multiple times.
    /// </summary>
    RepeatedQueries,

    /// <summary>
    /// High ratio of off-topic queries.
    /// </summary>
    OffTopicFlood,

    /// <summary>
    /// Multiple prompt injection attempts detected.
    /// </summary>
    PromptInjectionAttempts,

    /// <summary>
    /// Multiple requests returning large result sets (bulk extraction).
    /// </summary>
    BulkExtraction
}
