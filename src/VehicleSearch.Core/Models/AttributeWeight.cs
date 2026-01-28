namespace VehicleSearch.Core.Models;

/// <summary>
/// Represents a weighted attribute criterion for conceptual mapping.
/// </summary>
public class AttributeWeight
{
    /// <summary>
    /// Gets or sets the attribute/field name (e.g., "mileage", "fuelType").
    /// </summary>
    public string Attribute { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the importance weight (0.0 to 1.0).
    /// </summary>
    public double Weight { get; set; }

    /// <summary>
    /// Gets or sets the target value for comparison.
    /// </summary>
    public object? TargetValue { get; set; }

    /// <summary>
    /// Gets or sets the comparison type: "less", "greater", "equals", "in", "contains".
    /// </summary>
    public string ComparisonType { get; set; } = string.Empty;
}
