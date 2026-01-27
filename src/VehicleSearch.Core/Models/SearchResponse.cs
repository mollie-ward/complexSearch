namespace VehicleSearch.Core.Models;

/// <summary>
/// Represents a search response containing vehicle results.
/// </summary>
public class SearchResponse
{
    /// <summary>
    /// Gets or sets the list of vehicle results.
    /// </summary>
    public List<VehicleResult> Results { get; set; } = new();

    /// <summary>
    /// Gets or sets the total number of results found.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Gets or sets the time taken to execute the search in milliseconds.
    /// </summary>
    public long ExecutionTimeMs { get; set; }
}
