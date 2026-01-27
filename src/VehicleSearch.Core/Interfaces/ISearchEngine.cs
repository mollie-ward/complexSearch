using VehicleSearch.Core.Models;

namespace VehicleSearch.Core.Interfaces;

/// <summary>
/// Interface for vehicle search engine operations.
/// </summary>
public interface ISearchEngine
{
    /// <summary>
    /// Searches for vehicles based on the provided search request.
    /// </summary>
    /// <param name="request">The search request parameters.</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>A search response containing matching vehicles.</returns>
    Task<SearchResponse> SearchAsync(SearchRequest request, CancellationToken cancellationToken = default);
}
