using VehicleSearch.Core.Enums;

namespace VehicleSearch.Core.Models;

/// <summary>
/// Represents the result of a safety validation operation.
/// </summary>
public class SafetyValidationResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the validation was successful.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Gets or sets the list of validation errors.
    /// </summary>
    public List<ValidationError> Errors { get; set; } = new();

    /// <summary>
    /// Gets or sets the type of safety violation detected, if any.
    /// </summary>
    public SafetyViolationType? ViolationType { get; set; }

    /// <summary>
    /// Gets or sets a user-friendly message describing the validation result.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets the total number of errors.
    /// </summary>
    public int ErrorCount => Errors.Count;
}
