namespace VehicleSearch.Core.Models;

/// <summary>
/// Represents a vehicle embedding with its vector representation.
/// </summary>
public class VehicleEmbedding
{
    /// <summary>
    /// Gets or sets the vehicle identifier.
    /// </summary>
    public string VehicleId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the embedding vector.
    /// </summary>
    public float[] Vector { get; set; } = Array.Empty<float>();

    /// <summary>
    /// Gets or sets the number of dimensions in the vector.
    /// </summary>
    public int Dimensions { get; set; }
}
