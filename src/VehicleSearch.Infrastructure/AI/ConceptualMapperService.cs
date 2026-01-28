using Microsoft.Extensions.Logging;
using VehicleSearch.Core.Entities;
using VehicleSearch.Core.Interfaces;
using VehicleSearch.Core.Models;

namespace VehicleSearch.Infrastructure.AI;

/// <summary>
/// Service for mapping qualitative concepts to measurable attributes and computing similarity scores.
/// </summary>
public class ConceptualMapperService : IConceptualMapperService
{
    private readonly ILogger<ConceptualMapperService> _logger;
    private readonly SimilarityScorer _scorer;

    public ConceptualMapperService(
        ILogger<ConceptualMapperService> logger,
        SimilarityScorer scorer)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _scorer = scorer ?? throw new ArgumentNullException(nameof(scorer));
    }

    /// <summary>
    /// Maps a qualitative concept to measurable vehicle attributes.
    /// </summary>
    public Task<ConceptualMapping?> MapConceptToAttributesAsync(string concept)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(concept);

        _logger.LogDebug("Mapping concept '{Concept}' to attributes", concept);

        if (ConceptMappings.Mappings.TryGetValue(concept, out var mapping))
        {
            _logger.LogInformation("Successfully mapped concept '{Concept}' to {AttributeCount} attributes",
                concept, mapping.AttributeWeights.Count);
            return Task.FromResult<ConceptualMapping?>(mapping);
        }

        _logger.LogWarning("No mapping found for concept '{Concept}'", concept);
        return Task.FromResult<ConceptualMapping?>(null);
    }

    /// <summary>
    /// Computes a multi-factor similarity score between a vehicle and a concept.
    /// </summary>
    public Task<SimilarityScore> ComputeSimilarityAsync(Vehicle vehicle, ConceptualMapping concept)
    {
        ArgumentNullException.ThrowIfNull(vehicle);
        ArgumentNullException.ThrowIfNull(concept);

        _logger.LogDebug("Computing similarity for vehicle {VehicleId} against concept {Concept}",
            vehicle.Id, concept.Concept);

        var score = _scorer.ComputeScore(vehicle, concept);

        _logger.LogInformation(
            "Computed similarity score {Score:F3} for vehicle {VehicleId} against concept {Concept} " +
            "({Matching} matching, {Mismatching} mismatching)",
            score.OverallScore, vehicle.Id, concept.Concept,
            score.MatchingAttributes.Count, score.MismatchingAttributes.Count);

        return Task.FromResult(score);
    }

    /// <summary>
    /// Generates an explainable relevance score for a vehicle against a parsed query.
    /// </summary>
    public async Task<ExplainedScore> ExplainRelevanceAsync(Vehicle vehicle, ParsedQuery query)
    {
        ArgumentNullException.ThrowIfNull(vehicle);
        ArgumentNullException.ThrowIfNull(query);

        _logger.LogDebug("Generating explanation for vehicle {VehicleId} against query '{Query}'",
            vehicle.Id, query.OriginalQuery);

        var components = new List<ScoreComponent>();
        double totalScore = 0.0;
        double totalWeight = 0.0;

        // 1. Check for exact match factors (weight 0.4 each)
        var exactMatchFactors = await ComputeExactMatchFactors(vehicle, query);
        components.AddRange(exactMatchFactors);

        // 2. Check for conceptual factors (weight 0.3 each)
        var conceptualFactors = await ComputeConceptualFactors(vehicle, query);
        components.AddRange(conceptualFactors);

        // 3. Semantic similarity is not yet implemented
        // When implemented, it should have a weight of 0.3
        // For now, we don't include it to avoid skewing scores with placeholder data

        // Calculate overall score
        foreach (var component in components)
        {
            totalScore += component.Score * component.Weight;
            totalWeight += component.Weight;
        }

        var finalScore = totalWeight > 0 ? totalScore / totalWeight : 0.0;

        // Generate human-readable explanation
        var explanation = GenerateExplanation(vehicle, query, components, finalScore);

        _logger.LogInformation(
            "Generated explanation for vehicle {VehicleId}: score={Score:F3}, components={ComponentCount}",
            vehicle.Id, finalScore, components.Count);

        return new ExplainedScore
        {
            Score = finalScore,
            Explanation = explanation,
            Components = components
        };
    }

    /// <summary>
    /// Computes exact match factors (make, model, price, location, features).
    /// </summary>
    private Task<List<ScoreComponent>> ComputeExactMatchFactors(Vehicle vehicle, ParsedQuery query)
    {
        var factors = new List<ScoreComponent>();

        foreach (var entity in query.Entities)
        {
            ScoreComponent? component = entity.Type switch
            {
                EntityType.Make when vehicle.Make.Equals(entity.Value, StringComparison.OrdinalIgnoreCase) =>
                    new ScoreComponent
                    {
                        Factor = "Make Match",
                        Score = 1.0,
                        Weight = 0.4,
                        Reason = $"Exact match for {entity.Value}"
                    },

                EntityType.Model when vehicle.Model.Equals(entity.Value, StringComparison.OrdinalIgnoreCase) =>
                    new ScoreComponent
                    {
                        Factor = "Model Match",
                        Score = 1.0,
                        Weight = 0.4,
                        Reason = $"Exact match for {entity.Value}"
                    },

                EntityType.Price => ComputePriceMatchScore(vehicle, entity),

                EntityType.Location when vehicle.SaleLocation.Equals(entity.Value, StringComparison.OrdinalIgnoreCase) =>
                    new ScoreComponent
                    {
                        Factor = "Location Match",
                        Score = 1.0,
                        Weight = 0.4,
                        Reason = $"Located in {entity.Value}"
                    },

                _ => null
            };

            if (component != null)
            {
                factors.Add(component);
            }
        }

        return Task.FromResult(factors);
    }

    /// <summary>
    /// Computes price match score based on entity value.
    /// </summary>
    private ScoreComponent? ComputePriceMatchScore(Vehicle vehicle, ExtractedEntity entity)
    {
        if (!double.TryParse(entity.Value, out var targetPrice) || targetPrice <= 0)
            return null;

        // Simple score based on price proximity
        var priceDiff = Math.Abs((double)vehicle.Price - targetPrice);
        var score = priceDiff < 1000 ? 1.0 : Math.Max(0.0, 1.0 - (priceDiff / targetPrice));

        return new ScoreComponent
        {
            Factor = "Price Match",
            Score = score,
            Weight = 0.4,
            Reason = $"Price £{vehicle.Price:N0} near target £{targetPrice:N0}"
        };
    }

    /// <summary>
    /// Computes conceptual factors for qualitative terms.
    /// </summary>
    private async Task<List<ScoreComponent>> ComputeConceptualFactors(Vehicle vehicle, ParsedQuery query)
    {
        var factors = new List<ScoreComponent>();

        var qualitativeEntities = query.Entities
            .Where(e => e.Type == EntityType.QualitativeTerm)
            .ToList();

        foreach (var entity in qualitativeEntities)
        {
            var mapping = await MapConceptToAttributesAsync(entity.Value);
            if (mapping == null)
                continue;

            var similarityScore = await ComputeSimilarityAsync(vehicle, mapping);

            var matchStrength = similarityScore.OverallScore switch
            {
                >= 0.8 => "Strongly",
                >= 0.5 => "Partially",
                _ => "Weakly"
            };

            var matchingAttrs = string.Join(", ", similarityScore.MatchingAttributes);
            var reason = similarityScore.MatchingAttributes.Any()
                ? $"{matchStrength} matches '{entity.Value}' criteria: {matchingAttrs}"
                : $"{matchStrength} matches '{entity.Value}' criteria";

            factors.Add(new ScoreComponent
            {
                Factor = $"Conceptual: {entity.Value}",
                Score = similarityScore.OverallScore,
                Weight = 0.3,
                Reason = reason
            });
        }

        return factors;
    }

    /// <summary>
    /// Generates a human-readable explanation of the match.
    /// </summary>
    private string GenerateExplanation(Vehicle vehicle, ParsedQuery query, 
        List<ScoreComponent> components, double score)
    {
        var qualitativeTerms = query.Entities
            .Where(e => e.Type == EntityType.QualitativeTerm)
            .Select(e => e.Value)
            .ToList();

        var exactMatches = query.Entities
            .Where(e => e.Type == EntityType.Make || e.Type == EntityType.Model)
            .Select(e => e.Value)
            .ToList();

        var priceEntity = query.Entities.FirstOrDefault(e => e.Type == EntityType.Price);

        var parts = new List<string>();

        var scoreQuality = score switch
        {
            >= 0.8 => "strongly matches",
            >= 0.5 => "matches",
            _ => "partially matches"
        };

        parts.Add($"This vehicle {scoreQuality} your search");

        if (exactMatches.Any())
        {
            parts.Add($"for a {string.Join(" ", exactMatches)}");
        }

        if (priceEntity != null && double.TryParse(priceEntity.Value, out var price))
        {
            parts.Add($"around £{price:N0}");
        }

        if (qualitativeTerms.Any())
        {
            parts.Add($"with {string.Join(", ", qualitativeTerms)} characteristics");
        }

        return string.Join(" ", parts).Trim() + ".";
    }
}
