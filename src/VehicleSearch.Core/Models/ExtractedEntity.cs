namespace VehicleSearch.Core.Models;

/// <summary>
/// Represents an entity extracted from a user query.
/// </summary>
public class ExtractedEntity
{
    /// <summary>
    /// Gets or sets the type of entity.
    /// </summary>
    public EntityType Type { get; set; }

    /// <summary>
    /// Gets or sets the extracted value.
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the confidence score (0.0 to 1.0).
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    /// Gets or sets the start position in the original query.
    /// </summary>
    public int StartPosition { get; set; }

    /// <summary>
    /// Gets or sets the end position in the original query.
    /// </summary>
    public int EndPosition { get; set; }
}
