namespace VehicleSearch.Core.Models;

/// <summary>
/// Represents the breakdown of search scores from different approaches.
/// </summary>
public class SearchScoreBreakdown
{
    /// <summary>
    /// Gets or sets the exact match score component.
    /// </summary>
    public double ExactMatchScore { get; set; }

    /// <summary>
    /// Gets or sets the semantic similarity score component.
    /// </summary>
    public double SemanticScore { get; set; }

    /// <summary>
    /// Gets or sets the keyword/full-text search score component.
    /// </summary>
    public double KeywordScore { get; set; }

    /// <summary>
    /// Gets or sets the final combined relevance score.
    /// </summary>
    public double FinalScore { get; set; }
}
