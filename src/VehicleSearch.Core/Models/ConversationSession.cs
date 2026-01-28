namespace VehicleSearch.Core.Models;

/// <summary>
/// Represents a conversation session with its history and state.
/// </summary>
public class ConversationSession
{
    /// <summary>
    /// Gets or sets the unique session identifier.
    /// </summary>
    public string SessionId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets or sets the timestamp when the session was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the timestamp when the session was last accessed.
    /// </summary>
    public DateTime LastAccessedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the list of conversation messages.
    /// </summary>
    public List<ConversationMessage> Messages { get; set; } = new();

    /// <summary>
    /// Gets or sets the current search state.
    /// </summary>
    public SearchState? CurrentSearchState { get; set; }

    /// <summary>
    /// Gets or sets additional metadata for the session.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Gets the total number of messages in the session.
    /// </summary>
    public int MessageCount => Messages.Count;
}
