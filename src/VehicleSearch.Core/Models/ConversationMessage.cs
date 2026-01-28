using VehicleSearch.Core.Enums;

namespace VehicleSearch.Core.Models;

/// <summary>
/// Represents a single message in a conversation.
/// </summary>
public class ConversationMessage
{
    /// <summary>
    /// Gets or sets the unique message identifier.
    /// </summary>
    public string MessageId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets or sets the timestamp when the message was created.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the role of the message sender.
    /// </summary>
    public MessageRole Role { get; set; }

    /// <summary>
    /// Gets or sets the content of the message.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the parsed query data if the message contains a search query.
    /// </summary>
    public ParsedQuery? ParsedQuery { get; set; }

    /// <summary>
    /// Gets or sets the search results metadata if the message contains search results.
    /// </summary>
    public SearchResultsMetadata? Results { get; set; }
}

/// <summary>
/// Represents metadata about search results stored in a message.
/// </summary>
public class SearchResultsMetadata
{
    /// <summary>
    /// Gets or sets the total count of results.
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// Gets or sets the list of vehicle IDs in the results.
    /// </summary>
    public List<string> ResultIds { get; set; } = new();
}
