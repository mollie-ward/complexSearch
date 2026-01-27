namespace VehicleSearch.Core.Models;

/// <summary>
/// Represents the result of a vehicle indexing operation.
/// </summary>
public class IndexingResult
{
    /// <summary>
    /// Gets or sets the total number of vehicles processed.
    /// </summary>
    public int TotalVehicles { get; set; }

    /// <summary>
    /// Gets or sets the number of vehicles successfully indexed.
    /// </summary>
    public int Succeeded { get; set; }

    /// <summary>
    /// Gets or sets the number of vehicles that failed to index.
    /// </summary>
    public int Failed { get; set; }

    /// <summary>
    /// Gets or sets the list of errors encountered during indexing.
    /// </summary>
    public List<IndexingError> Errors { get; set; } = new();

    /// <summary>
    /// Gets or sets the duration of the indexing operation.
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Gets or sets the number of embeddings generated.
    /// </summary>
    public int EmbeddingsGenerated { get; set; }

    /// <summary>
    /// Gets a value indicating whether the indexing was successful.
    /// </summary>
    public bool Success => Failed == 0 && Succeeded > 0;
}
