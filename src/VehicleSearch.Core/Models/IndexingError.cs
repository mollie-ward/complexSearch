namespace VehicleSearch.Core.Models;

/// <summary>
/// Represents an error that occurred during indexing.
/// </summary>
public class IndexingError
{
    /// <summary>
    /// Gets or sets the vehicle ID that failed to index.
    /// </summary>
    public string VehicleId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the exception type if available.
    /// </summary>
    public string? ExceptionType { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the error occurred.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
