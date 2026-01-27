namespace VehicleSearch.Core.Models;

/// <summary>
/// Represents the intent of a user query.
/// </summary>
public enum QueryIntent
{
    /// <summary>
    /// User wants to find vehicles.
    /// </summary>
    Search,

    /// <summary>
    /// User wants to modify previous search results.
    /// </summary>
    Refine,

    /// <summary>
    /// User wants to compare vehicles.
    /// </summary>
    Compare,

    /// <summary>
    /// User is asking for information.
    /// </summary>
    Information,

    /// <summary>
    /// Query not related to vehicles.
    /// </summary>
    OffTopic
}
