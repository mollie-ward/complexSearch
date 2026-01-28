using VehicleSearch.Core.Enums;

namespace VehicleSearch.Core.Models;

/// <summary>
/// Represents a security event that should be logged and tracked.
/// </summary>
public class SecurityEvent
{
    /// <summary>
    /// Gets or sets the unique event identifier.
    /// </summary>
    public string EventId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets or sets when the event occurred.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the session identifier associated with the event.
    /// </summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of security event.
    /// </summary>
    public EventType EventType { get; set; }

    /// <summary>
    /// Gets or sets a description of the event.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets additional metadata about the event.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}
