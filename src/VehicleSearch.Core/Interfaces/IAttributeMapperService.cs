using VehicleSearch.Core.Models;

namespace VehicleSearch.Core.Interfaces;

/// <summary>
/// Service for mapping extracted entities to search constraints.
/// </summary>
public interface IAttributeMapperService
{
    /// <summary>
    /// Maps a parsed query with extracted entities to a structured search query with constraints.
    /// </summary>
    /// <param name="parsedQuery">The parsed query with extracted entities.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A mapped query with structured constraints.</returns>
    Task<MappedQuery> MapToSearchQueryAsync(ParsedQuery parsedQuery, CancellationToken cancellationToken = default);

    /// <summary>
    /// Parses a single entity into a search constraint.
    /// </summary>
    /// <param name="entity">The extracted entity.</param>
    /// <param name="context">Optional context from the original query (e.g., "under", "around").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A search constraint, or null if the entity cannot be mapped.</returns>
    Task<SearchConstraint?> ParseConstraintAsync(ExtractedEntity entity, string? context = null, CancellationToken cancellationToken = default);
}
