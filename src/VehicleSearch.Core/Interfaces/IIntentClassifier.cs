using VehicleSearch.Core.Models;

namespace VehicleSearch.Core.Interfaces;

/// <summary>
/// Service for classifying the intent of user queries.
/// </summary>
public interface IIntentClassifier
{
    /// <summary>
    /// Classifies the intent of a query.
    /// </summary>
    /// <param name="query">The user query to classify.</param>
    /// <param name="context">Optional conversation context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A tuple containing the intent and confidence score.</returns>
    Task<(QueryIntent Intent, double Confidence)> ClassifyAsync(string query, ConversationContext? context = null, CancellationToken cancellationToken = default);
}
