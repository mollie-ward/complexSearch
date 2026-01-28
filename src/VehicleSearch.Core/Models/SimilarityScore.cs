namespace VehicleSearch.Core.Models;

/// <summary>
/// Represents the similarity score between a vehicle and a concept.
/// </summary>
public class SimilarityScore
{
    /// <summary>
    /// Gets or sets the overall similarity score (0.0 to 1.0).
    /// </summary>
    public double OverallScore { get; set; }

    /// <summary>
    /// Gets or sets individual scores for each attribute.
    /// </summary>
    public Dictionary<string, double> ComponentScores { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of attributes that matched the criteria.
    /// </summary>
    public List<string> MatchingAttributes { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of attributes that did not match the criteria.
    /// </summary>
    public List<string> MismatchingAttributes { get; set; } = new();

    /// <summary>
    /// Gets or sets the description boost applied (can be negative).
    /// </summary>
    public double DescriptionBoost { get; set; }
}
