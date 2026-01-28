namespace VehicleSearch.Core.Models;

/// <summary>
/// Represents the results from a search operation.
/// </summary>
public class SearchResults
{
    /// <summary>
    /// Gets or sets the list of vehicle results.
    /// </summary>
    public List<VehicleResult> Results { get; set; } = new();

    /// <summary>
    /// Gets or sets the total count of matching results.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Gets or sets the search strategy that was used.
    /// </summary>
    public SearchStrategy Strategy { get; set; } = null!;

    /// <summary>
    /// Gets or sets the total duration of the search operation.
    /// </summary>
    public TimeSpan SearchDuration { get; set; }

    /// <summary>
    /// Gets or sets additional metadata about the search.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}
