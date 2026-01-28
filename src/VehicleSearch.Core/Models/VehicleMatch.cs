using VehicleSearch.Core.Entities;

namespace VehicleSearch.Core.Models;

/// <summary>
/// Represents a vehicle match from semantic search with similarity score.
/// </summary>
public class VehicleMatch
{
    /// <summary>
    /// Gets or sets the vehicle ID.
    /// </summary>
    public string VehicleId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the matched vehicle details.
    /// </summary>
    public Vehicle Vehicle { get; set; } = null!;

    /// <summary>
    /// Gets or sets the similarity score (cosine similarity, 0-1 range).
    /// </summary>
    public double SimilarityScore { get; set; }

    /// <summary>
    /// Gets or sets the normalized score (0-100 range).
    /// </summary>
    public int NormalizedScore { get; set; }
}
