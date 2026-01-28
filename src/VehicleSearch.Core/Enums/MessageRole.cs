namespace VehicleSearch.Core.Enums;

/// <summary>
/// Represents the role of a message in a conversation.
/// </summary>
public enum MessageRole
{
    /// <summary>
    /// Message from the user.
    /// </summary>
    User,

    /// <summary>
    /// Message from the assistant/system.
    /// </summary>
    Assistant,

    /// <summary>
    /// System message (e.g., initialization, configuration).
    /// </summary>
    System
}
