namespace VehicleSearch.Core.Models;

/// <summary>
/// Represents a search request for vehicles.
/// </summary>
public class SearchRequest
{
    /// <summary>
    /// Gets or sets the search query string.
    /// </summary>
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the maximum number of results to return.
    /// </summary>
    public int MaxResults { get; set; } = 10;

    /// <summary>
    /// Gets or sets optional filters for the search.
    /// </summary>
    public Dictionary<string, string> Filters { get; set; } = new();
}
