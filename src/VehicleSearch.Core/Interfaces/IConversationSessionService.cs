using VehicleSearch.Core.Models;

namespace VehicleSearch.Core.Interfaces;

/// <summary>
/// Service for managing conversation sessions.
/// </summary>
public interface IConversationSessionService
{
    /// <summary>
    /// Creates a new conversation session.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The newly created conversation session.</returns>
    Task<ConversationSession> CreateSessionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves an existing conversation session by ID.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The conversation session.</returns>
    /// <exception cref="Exceptions.SessionNotFoundException">Thrown when the session is not found.</exception>
    Task<ConversationSession> GetSessionAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a message to an existing conversation session.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="message">The message to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="Exceptions.SessionNotFoundException">Thrown when the session is not found.</exception>
    Task AddMessageAsync(string sessionId, ConversationMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the search state for a conversation session.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="state">The new search state.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="Exceptions.SessionNotFoundException">Thrown when the session is not found.</exception>
    Task UpdateSearchStateAsync(string sessionId, SearchState state, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the conversation history for a session.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="maxMessages">Maximum number of messages to retrieve (default: 10).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The conversation history.</returns>
    /// <exception cref="Exceptions.SessionNotFoundException">Thrown when the session is not found.</exception>
    Task<ConversationHistory> GetHistoryAsync(string sessionId, int maxMessages = 10, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears/deletes a conversation session.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ClearSessionAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a session exists.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the session exists, false otherwise.</returns>
    Task<bool> SessionExistsAsync(string sessionId, CancellationToken cancellationToken = default);
}
