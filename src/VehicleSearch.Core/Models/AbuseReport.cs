using VehicleSearch.Core.Enums;

namespace VehicleSearch.Core.Models;

/// <summary>
/// Represents a comprehensive abuse report for a session over a time window.
/// </summary>
public class AbuseReport
{
    /// <summary>
    /// Gets or sets the session identifier.
    /// </summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the start time of the analysis window.
    /// </summary>
    public DateTime WindowStart { get; set; }

    /// <summary>
    /// Gets or sets the end time of the analysis window.
    /// </summary>
    public DateTime WindowEnd { get; set; }

    /// <summary>
    /// Gets or sets the total number of queries in the time window.
    /// </summary>
    public int TotalQueries { get; set; }

    /// <summary>
    /// Gets or sets the number of off-topic queries detected.
    /// </summary>
    public int OffTopicQueryCount { get; set; }

    /// <summary>
    /// Gets or sets the number of prompt injection attempts.
    /// </summary>
    public int PromptInjectionAttempts { get; set; }

    /// <summary>
    /// Gets or sets the number of rate limit violations.
    /// </summary>
    public int RateLimitViolations { get; set; }

    /// <summary>
    /// Gets or sets the list of suspicious patterns detected.
    /// </summary>
    public List<SuspiciousPattern> SuspiciousPatterns { get; set; } = new();

    /// <summary>
    /// Gets or sets the calculated risk level.
    /// </summary>
    public RiskLevel RiskLevel { get; set; }

    /// <summary>
    /// Gets or sets recommendations for handling this session.
    /// </summary>
    public string Recommendation { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets when the report was generated.
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}
