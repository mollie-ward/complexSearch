using VehicleSearch.Core.Models;

namespace VehicleSearch.Core.Interfaces;

/// <summary>
/// Service for extracting entities from user queries.
/// </summary>
public interface IEntityExtractor
{
    /// <summary>
    /// Extracts entities from a query.
    /// </summary>
    /// <param name="query">The user query to extract entities from.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The extracted entities.</returns>
    Task<IEnumerable<ExtractedEntity>> ExtractAsync(string query, CancellationToken cancellationToken = default);
}
