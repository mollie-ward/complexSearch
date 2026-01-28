using VehicleSearch.Core.Models;

namespace VehicleSearch.Core.Interfaces;

/// <summary>
/// Interface for safety guardrail operations including input validation,
/// abuse prevention, and content filtering.
/// </summary>
public interface ISafetyGuardrailService
{
    /// <summary>
    /// Validates a query through the complete safety pipeline.
    /// </summary>
    /// <param name="query">The user query to validate.</param>
    /// <param name="sessionId">The session identifier for rate limiting.</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>A SafetyValidationResult indicating whether the query is safe.</returns>
    Task<SafetyValidationResult> ValidateQueryAsync(string query, string sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a query is off-topic (not related to vehicles).
    /// </summary>
    /// <param name="query">The query to check.</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>True if the query is off-topic, otherwise false.</returns>
    Task<bool> IsOffTopicAsync(string query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a query contains potential prompt injection patterns.
    /// </summary>
    /// <param name="query">The query to check.</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>True if prompt injection is detected, otherwise false.</returns>
    Task<bool> ContainsPromptInjectionAsync(string query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a query contains inappropriate content.
    /// </summary>
    /// <param name="query">The query to check.</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>True if inappropriate content is detected, otherwise false.</returns>
    Task<bool> ContainsInappropriateContentAsync(string query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks the rate limit for a session.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>A RateLimitResult indicating whether the request is allowed.</returns>
    Task<RateLimitResult> CheckRateLimitAsync(string sessionId, CancellationToken cancellationToken = default);
}
