namespace VehicleSearch.Core.Models;

/// <summary>
/// Approach used for re-ranking results.
/// </summary>
public enum RerankingApproach
{
    /// <summary>
    /// Weighted combination of ranking factors.
    /// </summary>
    WeightedScore,

    /// <summary>
    /// Machine learning-based ranking (future enhancement).
    /// </summary>
    LearningToRank,

    /// <summary>
    /// Rule-based reordering using business rules.
    /// </summary>
    BusinessRules,

    /// <summary>
    /// Combination of multiple approaches.
    /// </summary>
    Hybrid
}
