using VehicleSearch.Core.Enums;

namespace VehicleSearch.Core.Models;

/// <summary>
/// Represents a suspicious pattern detected in user behavior.
/// </summary>
public class SuspiciousPattern
{
    /// <summary>
    /// Gets or sets the type of suspicious pattern.
    /// </summary>
    public PatternType PatternType { get; set; }

    /// <summary>
    /// Gets or sets the severity score (0.0 to 1.0).
    /// </summary>
    public double Severity { get; set; }

    /// <summary>
    /// Gets or sets a description of the detected pattern.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets when the pattern was detected.
    /// </summary>
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets additional metadata about the pattern.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}
