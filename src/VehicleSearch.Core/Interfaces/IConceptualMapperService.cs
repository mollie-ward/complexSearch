using VehicleSearch.Core.Entities;
using VehicleSearch.Core.Models;

namespace VehicleSearch.Core.Interfaces;

/// <summary>
/// Service for mapping qualitative concepts to measurable attributes and computing similarity scores.
/// </summary>
public interface IConceptualMapperService
{
    /// <summary>
    /// Maps a qualitative concept (e.g., "reliable", "economical") to measurable vehicle attributes.
    /// </summary>
    /// <param name="concept">The qualitative concept to map.</param>
    /// <returns>The conceptual mapping with weighted attributes and indicators.</returns>
    Task<ConceptualMapping?> MapConceptToAttributesAsync(string concept);

    /// <summary>
    /// Computes a multi-factor similarity score between a vehicle and a concept.
    /// </summary>
    /// <param name="vehicle">The vehicle to score.</param>
    /// <param name="concept">The conceptual mapping to score against.</param>
    /// <returns>The detailed similarity score.</returns>
    Task<SimilarityScore> ComputeSimilarityAsync(Vehicle vehicle, ConceptualMapping concept);

    /// <summary>
    /// Generates an explainable relevance score for a vehicle against a parsed query.
    /// </summary>
    /// <param name="vehicle">The vehicle to explain.</param>
    /// <param name="query">The parsed search query.</param>
    /// <returns>The explained score with detailed breakdown.</returns>
    Task<ExplainedScore> ExplainRelevanceAsync(Vehicle vehicle, ParsedQuery query);
}
