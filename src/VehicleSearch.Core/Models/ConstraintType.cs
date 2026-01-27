namespace VehicleSearch.Core.Models;

/// <summary>
/// Represents the type of search constraint.
/// </summary>
public enum ConstraintType
{
    /// <summary>
    /// Exact match constraint (e.g., make, model).
    /// </summary>
    Exact,

    /// <summary>
    /// Range filter constraint (e.g., price, mileage).
    /// </summary>
    Range,

    /// <summary>
    /// Semantic search constraint (qualitative terms).
    /// </summary>
    Semantic,

    /// <summary>
    /// Composite constraint (multiple fields).
    /// </summary>
    Composite
}
