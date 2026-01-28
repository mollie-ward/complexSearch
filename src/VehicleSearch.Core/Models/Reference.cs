namespace VehicleSearch.Core.Models;

/// <summary>
/// Represents a reference found in a query.
/// </summary>
public class Reference
{
    /// <summary>
    /// Gets or sets the original reference text (e.g., "it", "cheaper").
    /// </summary>
    public string ReferenceText { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of reference.
    /// </summary>
    public ReferenceType Type { get; set; }

    /// <summary>
    /// Gets or sets the resolved value (vehicle ID, constraint value, etc.).
    /// </summary>
    public object? ResolvedValue { get; set; }

    /// <summary>
    /// Gets or sets the position in the query where the reference was found.
    /// </summary>
    public int Position { get; set; }
}
