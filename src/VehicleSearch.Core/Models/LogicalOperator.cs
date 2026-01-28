namespace VehicleSearch.Core.Models;

/// <summary>
/// Represents logical operators for combining constraints.
/// </summary>
public enum LogicalOperator
{
    /// <summary>
    /// All constraints must be satisfied (conjunction).
    /// </summary>
    And,

    /// <summary>
    /// At least one constraint must be satisfied (disjunction).
    /// </summary>
    Or
}
