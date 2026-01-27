using VehicleSearch.Core.Models;

namespace VehicleSearch.Core.Interfaces;

/// <summary>
/// Interface for conversation service operations.
/// </summary>
public interface IConversationService
{
    /// <summary>
    /// Processes a conversation request and maintains context.
    /// </summary>
    /// <param name="context">The conversation context.</param>
    /// <param name="message">The user message.</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>The response message.</returns>
    Task<string> ProcessMessageAsync(ConversationContext context, string message, CancellationToken cancellationToken = default);
}
