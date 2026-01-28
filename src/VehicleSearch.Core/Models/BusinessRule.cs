using VehicleSearch.Core.Entities;

namespace VehicleSearch.Core.Models;

/// <summary>
/// Represents a business rule for score adjustment.
/// </summary>
public class BusinessRule
{
    /// <summary>
    /// Gets or sets the rule name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the condition function that determines if the rule applies.
    /// </summary>
    public Func<Vehicle, bool> Condition { get; set; } = _ => false;

    /// <summary>
    /// Gets or sets the score adjustment to apply when condition is met (-1.0 to 1.0).
    /// </summary>
    public double ScoreAdjustment { get; set; }
}
