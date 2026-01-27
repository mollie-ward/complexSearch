namespace VehicleSearch.Core.Models;

/// <summary>
/// Represents the context of a conversation session.
/// </summary>
public class ConversationContext
{
    /// <summary>
    /// Gets or sets the unique session identifier.
    /// </summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the conversation history.
    /// </summary>
    public List<string> History { get; set; } = new();

    /// <summary>
    /// Gets or sets the user preferences or context data.
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();
}
