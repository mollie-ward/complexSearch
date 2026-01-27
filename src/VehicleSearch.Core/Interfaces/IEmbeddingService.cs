using VehicleSearch.Core.Entities;
using VehicleSearch.Core.Models;

namespace VehicleSearch.Core.Interfaces;

/// <summary>
/// Service interface for generating embeddings using Azure OpenAI.
/// </summary>
public interface IEmbeddingService
{
    /// <summary>
    /// Generates an embedding vector for the given text.
    /// </summary>
    /// <param name="text">The text to generate an embedding for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A float array representing the embedding vector.</returns>
    Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates embeddings for a batch of vehicles.
    /// </summary>
    /// <param name="vehicles">The vehicles to generate embeddings for.</param>
    /// <param name="batchSize">The number of vehicles to process in each batch.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of vehicle embeddings.</returns>
    Task<IEnumerable<VehicleEmbedding>> GenerateBatchEmbeddingsAsync(
        IEnumerable<Vehicle> vehicles,
        int batchSize = 100,
        CancellationToken cancellationToken = default);
}
