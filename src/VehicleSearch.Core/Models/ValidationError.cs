namespace VehicleSearch.Core.Models;

/// <summary>
/// Represents a validation error for a specific field.
/// </summary>
public class ValidationError
{
    /// <summary>
    /// Gets or sets the row number where the error occurred.
    /// </summary>
    public int RowNumber { get; set; }

    /// <summary>
    /// Gets or sets the field name that has the error.
    /// </summary>
    public string FieldName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the invalid value.
    /// </summary>
    public string? Value { get; set; }
}
