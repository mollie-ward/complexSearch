namespace VehicleSearch.Core.Models;

/// <summary>
/// Represents a request for semantic search.
/// </summary>
public class SemanticSearchRequest
{
    /// <summary>
    /// Gets or sets the natural language query for semantic search.
    /// </summary>
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the maximum number of results to return.
    /// </summary>
    public int MaxResults { get; set; } = 10;

    /// <summary>
    /// Gets or sets optional filters to combine with semantic search.
    /// </summary>
    public List<SearchConstraint> Filters { get; set; } = new();
}
