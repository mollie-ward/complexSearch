namespace VehicleSearch.Core.Models;

/// <summary>
/// Represents information about a blocked session.
/// </summary>
public class SessionBlockInfo
{
    /// <summary>
    /// Gets or sets the session identifier.
    /// </summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets when the session was blocked.
    /// </summary>
    public DateTime BlockedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the duration of the block.
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Gets or sets when the block expires.
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets the reason for blocking.
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Gets whether the block is still active.
    /// </summary>
    public bool IsActive => DateTime.UtcNow < ExpiresAt;

    /// <summary>
    /// Gets the remaining time before the block expires.
    /// </summary>
    public TimeSpan RemainingTime => ExpiresAt > DateTime.UtcNow 
        ? ExpiresAt - DateTime.UtcNow 
        : TimeSpan.Zero;
}
