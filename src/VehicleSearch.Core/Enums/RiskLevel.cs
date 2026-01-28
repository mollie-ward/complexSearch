namespace VehicleSearch.Core.Enums;

/// <summary>
/// Risk levels for session activity assessment.
/// </summary>
public enum RiskLevel
{
    /// <summary>
    /// Low risk - normal operation.
    /// </summary>
    Low,

    /// <summary>
    /// Medium risk - increased monitoring recommended.
    /// </summary>
    Medium,

    /// <summary>
    /// High risk - restrict access and monitor closely.
    /// </summary>
    High,

    /// <summary>
    /// Critical risk - block session immediately.
    /// </summary>
    Critical
}
