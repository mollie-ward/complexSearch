namespace VehicleSearch.Core.Models;

/// <summary>
/// Represents a structured search constraint derived from an extracted entity.
/// </summary>
public class SearchConstraint
{
    /// <summary>
    /// Gets or sets the target field name in the search index.
    /// </summary>
    public string FieldName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the comparison operator.
    /// </summary>
    public ConstraintOperator Operator { get; set; }

    /// <summary>
    /// Gets or sets the constraint value.
    /// For Between operator, this should be an array [min, max].
    /// For In operator, this should be an array of values.
    /// </summary>
    public object Value { get; set; } = null!;

    /// <summary>
    /// Gets or sets the constraint type.
    /// </summary>
    public ConstraintType Type { get; set; }
}
