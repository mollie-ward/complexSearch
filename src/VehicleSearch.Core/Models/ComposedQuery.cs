namespace VehicleSearch.Core.Models;

/// <summary>
/// Represents a composed query with grouped constraints and metadata.
/// </summary>
public class ComposedQuery
{
    /// <summary>
    /// Gets or sets the type of the composed query.
    /// </summary>
    public QueryType Type { get; set; }

    /// <summary>
    /// Gets or sets the list of constraint groups.
    /// </summary>
    public List<ConstraintGroup> ConstraintGroups { get; set; } = new();

    /// <summary>
    /// Gets or sets the logical operator combining constraint groups.
    /// </summary>
    public LogicalOperator GroupOperator { get; set; }

    /// <summary>
    /// Gets or sets warnings generated during composition.
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// Gets or sets whether the query has conflicting constraints.
    /// </summary>
    public bool HasConflicts { get; set; }

    /// <summary>
    /// Gets or sets the OData filter string for Azure Search.
    /// </summary>
    public string? ODataFilter { get; set; }
}
