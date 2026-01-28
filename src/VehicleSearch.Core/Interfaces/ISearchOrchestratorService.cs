using VehicleSearch.Core.Models;

namespace VehicleSearch.Core.Interfaces;

/// <summary>
/// Service interface for orchestrating search operations across multiple strategies.
/// </summary>
public interface ISearchOrchestratorService
{
    /// <summary>
    /// Determines the optimal search strategy based on query characteristics.
    /// </summary>
    /// <param name="query">The composed query to analyze.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The recommended search strategy.</returns>
    Task<SearchStrategy> DetermineStrategyAsync(ComposedQuery query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a search using the specified strategy.
    /// </summary>
    /// <param name="query">The composed query to execute.</param>
    /// <param name="strategy">The search strategy to use.</param>
    /// <param name="maxResults">Maximum number of results to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Search results with matching vehicles.</returns>
    Task<SearchResults> ExecuteSearchAsync(
        ComposedQuery query,
        SearchStrategy strategy,
        int maxResults = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a hybrid search combining exact and semantic approaches.
    /// </summary>
    /// <param name="query">The composed query to execute.</param>
    /// <param name="maxResults">Maximum number of results to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Search results with matching vehicles.</returns>
    Task<SearchResults> ExecuteHybridSearchAsync(
        ComposedQuery query,
        int maxResults = 10,
        CancellationToken cancellationToken = default);
}
