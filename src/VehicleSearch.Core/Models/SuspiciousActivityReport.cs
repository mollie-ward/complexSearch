using VehicleSearch.Core.Enums;

namespace VehicleSearch.Core.Models;

/// <summary>
/// Represents the result of suspicious activity detection for a session.
/// </summary>
public class SuspiciousActivityReport
{
    /// <summary>
    /// Gets or sets the session identifier.
    /// </summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of detected suspicious patterns.
    /// </summary>
    public List<SuspiciousPattern> DetectedPatterns { get; set; } = new();

    /// <summary>
    /// Gets or sets the calculated risk level.
    /// </summary>
    public RiskLevel RiskLevel { get; set; }

    /// <summary>
    /// Gets or sets the maximum severity score from all patterns.
    /// </summary>
    public double MaxSeverity { get; set; }

    /// <summary>
    /// Gets or sets recommendations for handling the session.
    /// </summary>
    public string Recommendation { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets when the analysis was performed.
    /// </summary>
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets whether any suspicious patterns were detected.
    /// </summary>
    public bool HasSuspiciousActivity => DetectedPatterns.Any();
}
