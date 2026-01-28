using VehicleSearch.Core.Models;

namespace VehicleSearch.Core.Interfaces;

/// <summary>
/// Service interface for ranking and re-ranking search results.
/// </summary>
public interface IResultRankingService
{
    /// <summary>
    /// Ranks search results using a default strategy.
    /// </summary>
    /// <param name="results">The results to rank.</param>
    /// <param name="query">The composed query for context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Ranked results.</returns>
    Task<List<VehicleResult>> RankResultsAsync(
        List<VehicleResult> results,
        ComposedQuery query,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Re-ranks results using a specific strategy.
    /// </summary>
    /// <param name="results">The results to re-rank.</param>
    /// <param name="strategy">The re-ranking strategy to use.</param>
    /// <param name="query">The composed query for context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Re-ranked results.</returns>
    Task<List<VehicleResult>> RerankResultsAsync(
        List<VehicleResult> results,
        RerankingStrategy strategy,
        ComposedQuery query,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Computes a business-adjusted score for a vehicle.
    /// </summary>
    /// <param name="vehicle">The vehicle to score.</param>
    /// <param name="query">The composed query for context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The business score (0-1).</returns>
    Task<double> ComputeBusinessScoreAsync(
        VehicleResult result,
        ComposedQuery query,
        CancellationToken cancellationToken = default);
}
