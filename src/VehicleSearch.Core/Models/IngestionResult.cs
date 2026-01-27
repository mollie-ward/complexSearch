namespace VehicleSearch.Core.Models;

/// <summary>
/// Represents the result of a data ingestion operation.
/// </summary>
public class IngestionResult
{
    /// <summary>
    /// Gets or sets the total number of rows processed.
    /// </summary>
    public int TotalRows { get; set; }

    /// <summary>
    /// Gets or sets the number of valid rows.
    /// </summary>
    public int ValidRows { get; set; }

    /// <summary>
    /// Gets or sets the number of invalid rows.
    /// </summary>
    public int InvalidRows { get; set; }

    /// <summary>
    /// Gets or sets the processing time in milliseconds.
    /// </summary>
    public long ProcessingTimeMs { get; set; }

    /// <summary>
    /// Gets or sets the list of errors encountered during ingestion.
    /// </summary>
    public List<ValidationError> Errors { get; set; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether the ingestion was successful.
    /// </summary>
    public bool Success => InvalidRows == 0 || ValidRows > 0;

    /// <summary>
    /// Gets or sets the timestamp when the ingestion completed.
    /// </summary>
    public DateTime CompletedAt { get; set; }
}
