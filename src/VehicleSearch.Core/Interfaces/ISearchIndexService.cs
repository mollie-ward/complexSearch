namespace VehicleSearch.Core.Interfaces;

/// <summary>
/// Service for managing Azure AI Search index operations.
/// </summary>
public interface ISearchIndexService
{
    /// <summary>
    /// Creates the search index with the full schema.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task CreateIndexAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the search index.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task DeleteIndexAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the search index exists.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the index exists, false otherwise.</returns>
    Task<bool> IndexExistsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the search index schema.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task UpdateIndexSchemaAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current status of the search index.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Index status information including document count and storage size.</returns>
    Task<IndexStatus> GetIndexStatusAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents the status of a search index.
/// </summary>
public record IndexStatus
{
    /// <summary>
    /// Gets whether the index exists.
    /// </summary>
    public bool Exists { get; init; }

    /// <summary>
    /// Gets the index name.
    /// </summary>
    public string IndexName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the document count in the index.
    /// </summary>
    public long DocumentCount { get; init; }

    /// <summary>
    /// Gets the storage size of the index.
    /// </summary>
    public string StorageSize { get; init; } = string.Empty;
}
