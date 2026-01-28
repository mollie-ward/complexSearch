using VehicleSearch.Core.Models;

namespace VehicleSearch.Core.Interfaces;

/// <summary>
/// Interface for abuse monitoring, suspicious activity detection, and session blocking.
/// </summary>
public interface IAbuseMonitoringService
{
    /// <summary>
    /// Detects suspicious activity patterns for a session.
    /// </summary>
    /// <param name="sessionId">The session identifier to analyze.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A report containing detected patterns and risk assessment.</returns>
    Task<SuspiciousActivityReport> DetectSuspiciousActivityAsync(
        string sessionId, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs a security event.
    /// </summary>
    /// <param name="securityEvent">The security event to log.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task LogSecurityEventAsync(
        SecurityEvent securityEvent, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Blocks a session for a specified duration.
    /// </summary>
    /// <param name="sessionId">The session identifier to block.</param>
    /// <param name="duration">How long to block the session.</param>
    /// <param name="reason">The reason for blocking.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task BlockSessionAsync(
        string sessionId, 
        TimeSpan duration, 
        string reason, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Unblocks a session.
    /// </summary>
    /// <param name="sessionId">The session identifier to unblock.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UnblockSessionAsync(
        string sessionId, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a session is currently blocked.
    /// </summary>
    /// <param name="sessionId">The session identifier to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the session is blocked, otherwise false.</returns>
    Task<bool> IsSessionBlockedAsync(
        string sessionId, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets information about a blocked session.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Block information if the session is blocked, otherwise null.</returns>
    Task<SessionBlockInfo?> GetBlockInfoAsync(
        string sessionId, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a comprehensive abuse report for a session.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="timeWindow">The time window to analyze (default: 1 hour).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A comprehensive abuse report.</returns>
    Task<AbuseReport> GenerateAbuseReportAsync(
        string sessionId, 
        TimeSpan? timeWindow = null, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tracks a query for abuse monitoring.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="query">The query text.</param>
    /// <param name="resultCount">Number of results returned (for bulk extraction detection).</param>
    /// <param name="wasOffTopic">Whether the query was off-topic.</param>
    /// <param name="hadPromptInjection">Whether the query had prompt injection.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task TrackQueryAsync(
        string sessionId, 
        string query, 
        int resultCount = 0,
        bool wasOffTopic = false,
        bool hadPromptInjection = false,
        CancellationToken cancellationToken = default);
}
