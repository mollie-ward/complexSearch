using VehicleSearch.Core.Entities;

namespace VehicleSearch.Core.Models;

/// <summary>
/// Represents a single vehicle search result with relevance score.
/// </summary>
public class VehicleResult
{
    /// <summary>
    /// Gets or sets the vehicle entity.
    /// </summary>
    public Vehicle Vehicle { get; set; } = new();

    /// <summary>
    /// Gets or sets the relevance score for this result.
    /// </summary>
    public double Score { get; set; }

    /// <summary>
    /// Gets or sets the detailed score breakdown from different search approaches.
    /// </summary>
    public SearchScoreBreakdown? ScoreBreakdown { get; set; }

    /// <summary>
    /// Gets or sets highlights or snippets from the search.
    /// </summary>
    public List<string> Highlights { get; set; } = new();
}
