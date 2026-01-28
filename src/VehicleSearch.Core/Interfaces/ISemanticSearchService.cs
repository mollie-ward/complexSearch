using VehicleSearch.Core.Models;

namespace VehicleSearch.Core.Interfaces;

/// <summary>
/// Service interface for semantic search operations.
/// </summary>
public interface ISemanticSearchService
{
    /// <summary>
    /// Performs a semantic search using vector embeddings.
    /// </summary>
    /// <param name="request">The semantic search request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A semantic search response with matching vehicles and scores.</returns>
    Task<SemanticSearchResponse> SearchAsync(SemanticSearchRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches by embedding vector directly.
    /// </summary>
    /// <param name="embedding">The embedding vector to search with.</param>
    /// <param name="maxResults">Maximum number of results to return.</param>
    /// <param name="filters">Optional filters to apply.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of vehicle matches.</returns>
    Task<List<VehicleMatch>> SearchByEmbeddingAsync(
        float[] embedding,
        int maxResults,
        List<SearchConstraint>? filters = null,
        CancellationToken cancellationToken = default);
}
