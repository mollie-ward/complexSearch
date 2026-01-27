namespace VehicleSearch.Core.Models;

/// <summary>
/// Represents a query that has been mapped from natural language entities to structured search constraints.
/// </summary>
public class MappedQuery
{
    /// <summary>
    /// Gets or sets the list of search constraints.
    /// </summary>
    public List<SearchConstraint> Constraints { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of terms that could not be mapped to constraints.
    /// </summary>
    public List<string> UnmappableTerms { get; set; } = new();

    /// <summary>
    /// Gets or sets metadata about the mapping operation.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}
