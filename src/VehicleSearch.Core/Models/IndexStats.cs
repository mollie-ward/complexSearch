namespace VehicleSearch.Core.Models;

/// <summary>
/// Represents statistics about the search index.
/// </summary>
public class IndexStats
{
    /// <summary>
    /// Gets or sets the total number of documents in the index.
    /// </summary>
    public long DocumentCount { get; set; }

    /// <summary>
    /// Gets or sets the storage size of the index.
    /// </summary>
    public string StorageSize { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the index name.
    /// </summary>
    public string IndexName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the last update timestamp.
    /// </summary>
    public DateTime? LastUpdated { get; set; }
}
