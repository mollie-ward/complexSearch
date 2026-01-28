namespace VehicleSearch.Core.Models;

/// <summary>
/// Factors used in result ranking.
/// </summary>
public enum RankingFactor
{
    /// <summary>
    /// Semantic relevance from vector similarity.
    /// </summary>
    SemanticRelevance,

    /// <summary>
    /// Count of exact constraint matches.
    /// </summary>
    ExactMatchCount,

    /// <summary>
    /// Price competitiveness relative to other results.
    /// </summary>
    PriceCompetitiveness,

    /// <summary>
    /// Vehicle condition based on mileage, service history, MOT, etc.
    /// </summary>
    VehicleCondition,

    /// <summary>
    /// Vehicle age/recency.
    /// </summary>
    Recency,

    /// <summary>
    /// Popularity or demand.
    /// </summary>
    Popularity,

    /// <summary>
    /// Location proximity to user.
    /// </summary>
    LocationProximity
}
