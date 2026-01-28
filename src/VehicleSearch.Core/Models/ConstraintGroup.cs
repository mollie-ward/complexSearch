namespace VehicleSearch.Core.Models;

/// <summary>
/// Represents a group of constraints combined with a logical operator.
/// </summary>
public class ConstraintGroup
{
    /// <summary>
    /// Gets or sets the list of constraints in this group.
    /// </summary>
    public List<SearchConstraint> Constraints { get; set; } = new();

    /// <summary>
    /// Gets or sets the logical operator combining constraints within this group.
    /// </summary>
    public LogicalOperator Operator { get; set; }

    /// <summary>
    /// Gets or sets the priority of this constraint group.
    /// Higher priority groups are less likely to be relaxed.
    /// </summary>
    public double Priority { get; set; }
}
