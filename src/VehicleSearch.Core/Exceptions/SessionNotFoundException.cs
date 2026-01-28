namespace VehicleSearch.Core.Exceptions;

/// <summary>
/// Exception thrown when a conversation session is not found.
/// </summary>
public class SessionNotFoundException : Exception
{
    /// <summary>
    /// Gets the session identifier that was not found.
    /// </summary>
    public string SessionId { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SessionNotFoundException"/> class.
    /// </summary>
    /// <param name="sessionId">The session identifier that was not found.</param>
    public SessionNotFoundException(string sessionId)
        : base($"Session with ID '{sessionId}' was not found or has expired.")
    {
        SessionId = sessionId;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SessionNotFoundException"/> class.
    /// </summary>
    /// <param name="sessionId">The session identifier that was not found.</param>
    /// <param name="innerException">The inner exception.</param>
    public SessionNotFoundException(string sessionId, Exception innerException)
        : base($"Session with ID '{sessionId}' was not found or has expired.", innerException)
    {
        SessionId = sessionId;
    }
}
