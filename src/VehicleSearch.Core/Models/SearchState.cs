namespace VehicleSearch.Core.Models;

/// <summary>
/// Represents the search state within a conversation session.
/// </summary>
public class SearchState
{
    /// <summary>
    /// Gets or sets the most recent query string.
    /// </summary>
    public string? LastQuery { get; set; }

    /// <summary>
    /// Gets or sets the IDs of the last search results (not full vehicle objects).
    /// </summary>
    public List<string> LastResultIds { get; set; } = new();

    /// <summary>
    /// Gets or sets the currently active search constraints.
    /// </summary>
    public Dictionary<string, SearchConstraint> ActiveFilters { get; set; } = new();

    /// <summary>
    /// Gets or sets the IDs of vehicles the user has viewed.
    /// </summary>
    public List<string> ViewedVehicleIds { get; set; } = new();

    /// <summary>
    /// Gets or sets the timestamp of the last search.
    /// </summary>
    public DateTime? LastSearchTime { get; set; }
}
