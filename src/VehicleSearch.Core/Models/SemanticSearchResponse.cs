namespace VehicleSearch.Core.Models;

/// <summary>
/// Represents the response from a semantic search operation.
/// </summary>
public class SemanticSearchResponse
{
    /// <summary>
    /// Gets or sets the list of vehicle matches.
    /// </summary>
    public List<VehicleMatch> Matches { get; set; } = new();

    /// <summary>
    /// Gets or sets the average similarity score across all matches.
    /// </summary>
    public double AverageScore { get; set; }

    /// <summary>
    /// Gets or sets the search duration.
    /// </summary>
    public TimeSpan SearchDuration { get; set; }
}
