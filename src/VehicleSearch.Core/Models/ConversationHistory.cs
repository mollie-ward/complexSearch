namespace VehicleSearch.Core.Models;

/// <summary>
/// Represents the conversation history response for API.
/// </summary>
public class ConversationHistory
{
    /// <summary>
    /// Gets or sets the session identifier.
    /// </summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of messages in the history.
    /// </summary>
    public List<ConversationMessage> Messages { get; set; } = new();

    /// <summary>
    /// Gets or sets the total number of messages in the session.
    /// </summary>
    public int TotalMessages { get; set; }
}
