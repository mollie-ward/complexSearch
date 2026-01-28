using System.Collections.Concurrent;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using VehicleSearch.Core.Exceptions;
using VehicleSearch.Core.Interfaces;
using VehicleSearch.Core.Models;

namespace VehicleSearch.Infrastructure.Session;

/// <summary>
/// In-memory implementation of conversation session service.
/// Uses ConcurrentDictionary for thread-safe operations.
/// </summary>
public class InMemoryConversationSessionService : IConversationSessionService
{
    private readonly ConcurrentDictionary<string, ConversationSession> _sessions;
    private readonly ILogger<InMemoryConversationSessionService> _logger;
    private readonly int _sessionTimeoutHours;
    private readonly int _maxMessagesPerSession;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryConversationSessionService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="configuration">The configuration.</param>
    public InMemoryConversationSessionService(
        ILogger<InMemoryConversationSessionService> logger,
        IConfiguration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _sessions = new ConcurrentDictionary<string, ConversationSession>();
        _sessionTimeoutHours = configuration.GetValue<int>("ConversationSession:SessionTimeoutHours", 4);
        _maxMessagesPerSession = configuration.GetValue<int>("ConversationSession:MaxMessagesPerSession", 100);

        _logger.LogInformation("InMemoryConversationSessionService initialized with timeout: {TimeoutHours}h, max messages: {MaxMessages}",
            _sessionTimeoutHours, _maxMessagesPerSession);
    }

    /// <inheritdoc/>
    public Task<ConversationSession> CreateSessionAsync(CancellationToken cancellationToken = default)
    {
        var session = new ConversationSession
        {
            SessionId = Guid.NewGuid().ToString(),
            CreatedAt = DateTime.UtcNow,
            LastAccessedAt = DateTime.UtcNow,
            Messages = new List<ConversationMessage>(),
            CurrentSearchState = null,
            Metadata = new Dictionary<string, object>()
        };

        if (_sessions.TryAdd(session.SessionId, session))
        {
            _logger.LogInformation("Created new session: {SessionId}", session.SessionId);
            return Task.FromResult(session);
        }

        // Extremely unlikely, but handle collision
        _logger.LogWarning("Session ID collision detected, retrying...");
        return CreateSessionAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public Task<ConversationSession> GetSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            throw new ArgumentException("Session ID cannot be null or empty.", nameof(sessionId));
        }

        if (!_sessions.TryGetValue(sessionId, out var session))
        {
            _logger.LogWarning("Session not found: {SessionId}", sessionId);
            throw new SessionNotFoundException(sessionId);
        }

        // Check if session has expired
        var inactiveTime = DateTime.UtcNow - session.LastAccessedAt;
        if (inactiveTime.TotalHours > _sessionTimeoutHours)
        {
            _logger.LogWarning("Session expired: {SessionId}, inactive for {InactiveHours}h", sessionId, inactiveTime.TotalHours);
            _sessions.TryRemove(sessionId, out _);
            throw new SessionNotFoundException(sessionId);
        }

        // Update last accessed time
        session.LastAccessedAt = DateTime.UtcNow;
        _logger.LogDebug("Retrieved session: {SessionId}, message count: {MessageCount}", sessionId, session.MessageCount);

        return Task.FromResult(session);
    }

    /// <inheritdoc/>
    public async Task AddMessageAsync(string sessionId, ConversationMessage message, CancellationToken cancellationToken = default)
    {
        if (message == null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        var session = await GetSessionAsync(sessionId, cancellationToken);

        // Enforce max messages limit
        if (session.Messages.Count >= _maxMessagesPerSession)
        {
            _logger.LogWarning("Session {SessionId} reached max message limit ({MaxMessages}), removing oldest message",
                sessionId, _maxMessagesPerSession);
            session.Messages.RemoveAt(0);
        }

        // Ensure message has timestamp
        if (message.Timestamp == default)
        {
            message.Timestamp = DateTime.UtcNow;
        }

        session.Messages.Add(message);
        session.LastAccessedAt = DateTime.UtcNow;

        _logger.LogDebug("Added message to session {SessionId}: {MessageId} ({Role})",
            sessionId, message.MessageId, message.Role);
    }

    /// <inheritdoc/>
    public async Task UpdateSearchStateAsync(string sessionId, SearchState state, CancellationToken cancellationToken = default)
    {
        if (state == null)
        {
            throw new ArgumentNullException(nameof(state));
        }

        var session = await GetSessionAsync(sessionId, cancellationToken);
        session.CurrentSearchState = state;
        session.LastAccessedAt = DateTime.UtcNow;

        _logger.LogDebug("Updated search state for session {SessionId}", sessionId);
    }

    /// <inheritdoc/>
    public async Task<ConversationHistory> GetHistoryAsync(string sessionId, int maxMessages = 10, CancellationToken cancellationToken = default)
    {
        var session = await GetSessionAsync(sessionId, cancellationToken);

        var messages = session.Messages
            .OrderByDescending(m => m.Timestamp)
            .Take(maxMessages)
            .OrderBy(m => m.Timestamp)
            .ToList();

        _logger.LogDebug("Retrieved {MessageCount} messages from session {SessionId} (total: {TotalMessages})",
            messages.Count, sessionId, session.MessageCount);

        return new ConversationHistory
        {
            SessionId = sessionId,
            Messages = messages,
            TotalMessages = session.MessageCount
        };
    }

    /// <inheritdoc/>
    public Task ClearSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            throw new ArgumentException("Session ID cannot be null or empty.", nameof(sessionId));
        }

        if (_sessions.TryRemove(sessionId, out _))
        {
            _logger.LogInformation("Cleared session: {SessionId}", sessionId);
        }
        else
        {
            _logger.LogWarning("Attempted to clear non-existent session: {SessionId}", sessionId);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<bool> SessionExistsAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return Task.FromResult(false);
        }

        var exists = _sessions.ContainsKey(sessionId);
        
        // Check expiration if session exists
        if (exists && _sessions.TryGetValue(sessionId, out var session))
        {
            var inactiveTime = DateTime.UtcNow - session.LastAccessedAt;
            if (inactiveTime.TotalHours > _sessionTimeoutHours)
            {
                _sessions.TryRemove(sessionId, out _);
                return Task.FromResult(false);
            }
        }

        return Task.FromResult(exists);
    }

    /// <summary>
    /// Removes expired sessions from memory.
    /// Called by the background cleanup service.
    /// </summary>
    /// <returns>Number of sessions removed.</returns>
    public int CleanupExpiredSessions()
    {
        var cutoffTime = DateTime.UtcNow.AddHours(-_sessionTimeoutHours);
        var expiredSessions = _sessions
            .Where(kvp => kvp.Value.LastAccessedAt < cutoffTime)
            .Select(kvp => kvp.Key)
            .ToList();

        var removedCount = 0;
        foreach (var sessionId in expiredSessions)
        {
            if (_sessions.TryRemove(sessionId, out _))
            {
                removedCount++;
            }
        }

        if (removedCount > 0)
        {
            _logger.LogInformation("Cleaned up {Count} expired sessions", removedCount);
        }

        return removedCount;
    }
}
