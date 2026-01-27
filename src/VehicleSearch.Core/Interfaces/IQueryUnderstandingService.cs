using VehicleSearch.Core.Models;

namespace VehicleSearch.Core.Interfaces;

/// <summary>
/// Service for understanding and parsing natural language queries.
/// </summary>
public interface IQueryUnderstandingService
{
    /// <summary>
    /// Parses a query to extract intent and entities.
    /// </summary>
    /// <param name="query">The user query to parse.</param>
    /// <param name="context">Optional conversation context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The parsed query with intent and entities.</returns>
    Task<ParsedQuery> ParseQueryAsync(string query, ConversationContext? context = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Classifies the intent of a query.
    /// </summary>
    /// <param name="query">The user query to classify.</param>
    /// <param name="context">Optional conversation context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The classified intent.</returns>
    Task<QueryIntent> ClassifyIntentAsync(string query, ConversationContext? context = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Extracts entities from a query.
    /// </summary>
    /// <param name="query">The user query to extract entities from.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The extracted entities.</returns>
    Task<IEnumerable<ExtractedEntity>> ExtractEntitiesAsync(string query, CancellationToken cancellationToken = default);
}
