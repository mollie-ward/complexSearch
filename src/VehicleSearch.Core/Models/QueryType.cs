namespace VehicleSearch.Core.Models;

/// <summary>
/// Represents the type of composed query based on complexity.
/// </summary>
public enum QueryType
{
    /// <summary>
    /// Single constraint query (e.g., "BMW cars").
    /// </summary>
    Simple,

    /// <summary>
    /// Multiple exact/range constraints (e.g., "BMW under £20k").
    /// </summary>
    Filtered,

    /// <summary>
    /// Mixed exact + semantic + range (e.g., "Economical BMW under £20k").
    /// </summary>
    Complex,

    /// <summary>
    /// Requires multiple search strategies (e.g., "Reliable BMW with parking sensors").
    /// </summary>
    MultiModal
}
