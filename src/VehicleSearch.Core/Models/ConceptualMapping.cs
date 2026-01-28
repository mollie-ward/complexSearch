namespace VehicleSearch.Core.Models;

/// <summary>
/// Represents a mapping from a qualitative concept to measurable attributes.
/// </summary>
public class ConceptualMapping
{
    /// <summary>
    /// Gets or sets the concept name (e.g., "reliable", "economical").
    /// </summary>
    public string Concept { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the weighted attribute criteria for this concept.
    /// </summary>
    public List<AttributeWeight> AttributeWeights { get; set; } = new();

    /// <summary>
    /// Gets or sets positive text indicators that boost the score.
    /// </summary>
    public List<string> PositiveIndicators { get; set; } = new();

    /// <summary>
    /// Gets or sets negative text indicators that reduce the score.
    /// </summary>
    public List<string> NegativeIndicators { get; set; } = new();
}
