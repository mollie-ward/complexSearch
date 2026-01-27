namespace VehicleSearch.Core.Models;

/// <summary>
/// Represents a parsed user query with intent and extracted entities.
/// </summary>
public class ParsedQuery
{
    /// <summary>
    /// Gets or sets the original user query.
    /// </summary>
    public string OriginalQuery { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the classified intent.
    /// </summary>
    public QueryIntent Intent { get; set; }

    /// <summary>
    /// Gets or sets the extracted entities.
    /// </summary>
    public List<ExtractedEntity> Entities { get; set; } = new();

    /// <summary>
    /// Gets or sets the overall confidence score (0.0 to 1.0).
    /// </summary>
    public double ConfidenceScore { get; set; }

    /// <summary>
    /// Gets or sets terms that could not be mapped to entities.
    /// </summary>
    public List<string> UnmappedTerms { get; set; } = new();
}
