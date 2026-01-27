namespace VehicleSearch.Core.Interfaces;

/// <summary>
/// Interface for knowledge base service operations.
/// </summary>
public interface IKnowledgeBaseService
{
    /// <summary>
    /// Retrieves knowledge base information.
    /// </summary>
    /// <param name="query">The query string.</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>Knowledge base response.</returns>
    Task<string> GetKnowledgeAsync(string query, CancellationToken cancellationToken = default);
}
