namespace VehicleSearch.Core.Models;

/// <summary>
/// Represents an explainable relevance score with detailed breakdown.
/// </summary>
public class ExplainedScore
{
    /// <summary>
    /// Gets or sets the overall relevance score (0.0 to 1.0).
    /// </summary>
    public double Score { get; set; }

    /// <summary>
    /// Gets or sets a human-readable explanation of the match.
    /// </summary>
    public string Explanation { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the detailed score components.
    /// </summary>
    public List<ScoreComponent> Components { get; set; } = new();
}

/// <summary>
/// Represents a single component of the overall score.
/// </summary>
public class ScoreComponent
{
    /// <summary>
    /// Gets or sets the factor name (e.g., "Make Match", "Conceptual: reliable").
    /// </summary>
    public string Factor { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the score for this factor (0.0 to 1.0).
    /// </summary>
    public double Score { get; set; }

    /// <summary>
    /// Gets or sets the weight/importance of this factor.
    /// </summary>
    public double Weight { get; set; }

    /// <summary>
    /// Gets or sets the explanation for this score component.
    /// </summary>
    public string Reason { get; set; } = string.Empty;
}
