namespace VehicleSearch.Core.Models;

/// <summary>
/// Represents a query with resolved references.
/// </summary>
public class ResolvedQuery
{
    /// <summary>
    /// Gets or sets the original user query.
    /// </summary>
    public string OriginalQuery { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the query with references replaced.
    /// </summary>
    public string ResolvedQueryText { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of resolved references.
    /// </summary>
    public List<Reference> ResolvedReferences { get; set; } = new();

    /// <summary>
    /// Gets or sets a dictionary of resolved constraint values.
    /// </summary>
    public Dictionary<string, object> ResolvedValues { get; set; } = new();

    /// <summary>
    /// Gets or sets a flag indicating if there are unresolved references.
    /// </summary>
    public bool HasUnresolvedReferences { get; set; }

    /// <summary>
    /// Gets or sets a message for unresolved references (e.g., for clarification).
    /// </summary>
    public string? UnresolvedMessage { get; set; }
}
