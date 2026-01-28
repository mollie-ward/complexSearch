using VehicleSearch.Core.Models;

namespace VehicleSearch.Core.Interfaces;

/// <summary>
/// Service for composing complex search queries from mapped constraints.
/// </summary>
public interface IQueryComposerService
{
    /// <summary>
    /// Composes a complex query from mapped constraints.
    /// </summary>
    /// <param name="mappedQuery">The mapped query with constraints.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A composed query with grouped constraints.</returns>
    Task<ComposedQuery> ComposeQueryAsync(MappedQuery mappedQuery, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a composed query for conflicts and invalid constraints.
    /// </summary>
    /// <param name="query">The composed query to validate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the query is valid; otherwise, false.</returns>
    Task<bool> ValidateQueryAsync(ComposedQuery query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves conflicts in a composed query.
    /// </summary>
    /// <param name="query">The composed query with conflicts.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A resolved query with conflicts addressed.</returns>
    Task<ComposedQuery> ResolveConflictsAsync(ComposedQuery query, CancellationToken cancellationToken = default);
}
