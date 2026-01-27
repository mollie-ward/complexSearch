namespace VehicleSearch.Core.Models;

/// <summary>
/// Represents the comparison operator for a search constraint.
/// </summary>
public enum ConstraintOperator
{
    /// <summary>
    /// Equals (=).
    /// </summary>
    Equals,

    /// <summary>
    /// Not equals (!=).
    /// </summary>
    NotEquals,

    /// <summary>
    /// Greater than (>).
    /// </summary>
    GreaterThan,

    /// <summary>
    /// Greater than or equal (>=).
    /// </summary>
    GreaterThanOrEqual,

    /// <summary>
    /// Less than (<).
    /// </summary>
    LessThan,

    /// <summary>
    /// Less than or equal (<=).
    /// </summary>
    LessThanOrEqual,

    /// <summary>
    /// Between (value1 <= x <= value2).
    /// </summary>
    Between,

    /// <summary>
    /// Contains (for string and array fields).
    /// </summary>
    Contains,

    /// <summary>
    /// In (value in set).
    /// </summary>
    In
}
