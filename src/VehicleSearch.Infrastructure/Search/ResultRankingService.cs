using System.Diagnostics;
using Microsoft.Extensions.Logging;
using VehicleSearch.Core.Entities;
using VehicleSearch.Core.Interfaces;
using VehicleSearch.Core.Models;

namespace VehicleSearch.Infrastructure.Search;

/// <summary>
/// Service for ranking and re-ranking search results.
/// </summary>
public class ResultRankingService : IResultRankingService
{
    private readonly ILogger<ResultRankingService> _logger;

    /// <summary>
    /// Default business rules for vehicle ranking.
    /// </summary>
    private static readonly List<BusinessRule> DefaultBusinessRules = new()
    {
        new BusinessRule
        {
            Name = "Boost Premium Makes",
            Condition = v => new[] { "BMW", "Mercedes-Benz", "Audi", "Porsche", "Jaguar", "Land Rover" }
                .Contains(v.Make, StringComparer.OrdinalIgnoreCase),
            ScoreAdjustment = 0.05
        },
        new BusinessRule
        {
            Name = "Penalize High Mileage",
            Condition = v => v.Mileage > 100000,
            ScoreAdjustment = -0.15
        },
        new BusinessRule
        {
            Name = "Boost Full Service History",
            Condition = v => v.ServiceHistoryPresent,
            ScoreAdjustment = 0.10
        },
        new BusinessRule
        {
            Name = "Penalize Accident Damage",
            Condition = v => v.Declarations.Any(d => 
                d.Contains("damage", StringComparison.OrdinalIgnoreCase) ||
                d.Contains("accident", StringComparison.OrdinalIgnoreCase)),
            ScoreAdjustment = -0.20
        },
        new BusinessRule
        {
            Name = "Boost Electric/Hybrid",
            Condition = v => new[] { "Electric", "Hybrid", "Plug-in Hybrid" }
                .Contains(v.FuelType, StringComparer.OrdinalIgnoreCase),
            ScoreAdjustment = 0.08
        },
        new BusinessRule
        {
            Name = "Penalize Near MOT Expiry",
            Condition = v => v.MotExpiryDate.HasValue && 
                (v.MotExpiryDate.Value - DateTime.UtcNow).TotalDays < 30,
            ScoreAdjustment = -0.10
        }
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="ResultRankingService"/> class.
    /// </summary>
    public ResultRankingService(ILogger<ResultRankingService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public Task<List<VehicleResult>> RankResultsAsync(
        List<VehicleResult> results,
        ComposedQuery query,
        CancellationToken cancellationToken = default)
    {
        var strategy = new RerankingStrategy
        {
            Approach = RerankingApproach.WeightedScore,
            FactorWeights = new Dictionary<RankingFactor, double>
            {
                [RankingFactor.SemanticRelevance] = 0.40,
                [RankingFactor.ExactMatchCount] = 0.25,
                [RankingFactor.PriceCompetitiveness] = 0.15,
                [RankingFactor.VehicleCondition] = 0.10,
                [RankingFactor.Recency] = 0.10
            },
            BusinessRules = DefaultBusinessRules,
            ApplyDiversity = true,
            MaxPerMake = 3,
            MaxPerModel = 2
        };

        return RerankResultsAsync(results, strategy, query, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<VehicleResult>> RerankResultsAsync(
        List<VehicleResult> results,
        RerankingStrategy strategy,
        ComposedQuery query,
        CancellationToken cancellationToken = default)
    {
        if (results == null || !results.Any())
        {
            return new List<VehicleResult>();
        }

        if (strategy == null)
        {
            throw new ArgumentNullException(nameof(strategy));
        }

        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation(
            "Re-ranking {Count} results using {Approach} approach",
            results.Count,
            strategy.Approach);

        try
        {
            // Validate weights sum to 1.0
            if (strategy.FactorWeights.Any())
            {
                var totalWeight = strategy.FactorWeights.Values.Sum();
                if (Math.Abs(totalWeight - 1.0) > 0.001)
                {
                    _logger.LogWarning(
                        "Factor weights sum to {Total}, normalizing to 1.0",
                        totalWeight);
                    
                    // Normalize weights
                    var normalizedWeights = new Dictionary<RankingFactor, double>();
                    foreach (var kvp in strategy.FactorWeights)
                    {
                        normalizedWeights[kvp.Key] = kvp.Value / totalWeight;
                    }
                    strategy.FactorWeights = normalizedWeights;
                }
            }

            // Apply weighted scoring
            var rankedResults = await ApplyWeightedScoringAsync(
                results,
                strategy.FactorWeights,
                query,
                cancellationToken);

            // Apply business rules
            if (strategy.BusinessRules.Any())
            {
                rankedResults = await ApplyBusinessRulesAsync(
                    rankedResults,
                    strategy.BusinessRules,
                    cancellationToken);
            }

            // Apply diversity enhancement if requested
            if (strategy.ApplyDiversity)
            {
                rankedResults = ApplyDiversityEnhancement(
                    rankedResults,
                    strategy.MaxPerMake,
                    strategy.MaxPerModel);
            }
            else
            {
                // Sort by score if not applying diversity
                rankedResults = rankedResults
                    .OrderByDescending(r => r.Score)
                    .ToList();
            }

            stopwatch.Stop();
            _logger.LogInformation(
                "Re-ranking completed in {Duration}ms, {Count} results",
                stopwatch.ElapsedMilliseconds,
                rankedResults.Count);

            return rankedResults;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during re-ranking");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<double> ComputeBusinessScoreAsync(
        VehicleResult result,
        ComposedQuery query,
        CancellationToken cancellationToken = default)
    {
        if (result == null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        var scores = new Dictionary<RankingFactor, double>
        {
            [RankingFactor.SemanticRelevance] = result.ScoreBreakdown?.SemanticScore ?? result.Score,
            [RankingFactor.ExactMatchCount] = ComputeExactMatchScore(result.Vehicle, query),
            [RankingFactor.PriceCompetitiveness] = 0.5, // Neutral without context
            [RankingFactor.VehicleCondition] = ComputeConditionScore(result.Vehicle),
            [RankingFactor.Recency] = ComputeRecencyScore(result.Vehicle)
        };

        // Default weights
        var weights = new Dictionary<RankingFactor, double>
        {
            [RankingFactor.SemanticRelevance] = 0.40,
            [RankingFactor.ExactMatchCount] = 0.25,
            [RankingFactor.PriceCompetitiveness] = 0.15,
            [RankingFactor.VehicleCondition] = 0.10,
            [RankingFactor.Recency] = 0.10
        };

        var weightedScore = weights.Sum(kvp => scores[kvp.Key] * kvp.Value);

        // Apply business rules
        var adjustment = 0.0;
        foreach (var rule in DefaultBusinessRules)
        {
            if (rule.Condition(result.Vehicle))
            {
                adjustment += rule.ScoreAdjustment;
            }
        }

        return await Task.FromResult(Math.Clamp(weightedScore + adjustment, 0.0, 1.0));
    }

    /// <summary>
    /// Applies weighted scoring to results.
    /// </summary>
    private async Task<List<VehicleResult>> ApplyWeightedScoringAsync(
        List<VehicleResult> results,
        Dictionary<RankingFactor, double> factorWeights,
        ComposedQuery query,
        CancellationToken cancellationToken)
    {
        foreach (var result in results)
        {
            var scores = new Dictionary<RankingFactor, double>();

            // Compute each factor score
            foreach (var factor in factorWeights.Keys)
            {
                scores[factor] = factor switch
                {
                    RankingFactor.SemanticRelevance => result.ScoreBreakdown?.SemanticScore ?? result.Score,
                    RankingFactor.ExactMatchCount => ComputeExactMatchScore(result.Vehicle, query),
                    RankingFactor.PriceCompetitiveness => ComputePriceScore(result.Vehicle, results),
                    RankingFactor.VehicleCondition => ComputeConditionScore(result.Vehicle),
                    RankingFactor.Recency => ComputeRecencyScore(result.Vehicle),
                    _ => 0.5 // Default neutral score
                };
            }

            // Calculate weighted average
            var finalScore = factorWeights.Sum(kvp => scores[kvp.Key] * kvp.Value);
            result.Score = finalScore;

            // Update score breakdown if present
            if (result.ScoreBreakdown != null)
            {
                result.ScoreBreakdown.FinalScore = finalScore;
            }

            _logger.LogDebug(
                "Vehicle {VehicleId} scored {Score:F3} (Semantic: {Semantic:F3}, Exact: {Exact:F3}, Price: {Price:F3}, Condition: {Condition:F3}, Recency: {Recency:F3})",
                result.Vehicle.Id,
                finalScore,
                scores.GetValueOrDefault(RankingFactor.SemanticRelevance, 0),
                scores.GetValueOrDefault(RankingFactor.ExactMatchCount, 0),
                scores.GetValueOrDefault(RankingFactor.PriceCompetitiveness, 0),
                scores.GetValueOrDefault(RankingFactor.VehicleCondition, 0),
                scores.GetValueOrDefault(RankingFactor.Recency, 0));
        }

        return await Task.FromResult(results);
    }

    /// <summary>
    /// Applies business rules to adjust scores.
    /// </summary>
    private async Task<List<VehicleResult>> ApplyBusinessRulesAsync(
        List<VehicleResult> results,
        List<BusinessRule> rules,
        CancellationToken cancellationToken)
    {
        foreach (var result in results)
        {
            var adjustment = 0.0;
            var appliedRules = new List<string>();

            foreach (var rule in rules)
            {
                if (rule.Condition(result.Vehicle))
                {
                    adjustment += rule.ScoreAdjustment;
                    appliedRules.Add(rule.Name);
                }
            }

            if (appliedRules.Any())
            {
                _logger.LogDebug(
                    "Applied rules to {VehicleId}: {Rules} (adjustment: {Adjustment:F3})",
                    result.Vehicle.Id,
                    string.Join(", ", appliedRules),
                    adjustment);
            }

            // Apply adjustment and clamp to valid range
            result.Score = Math.Clamp(result.Score + adjustment, 0.0, 1.0);
        }

        return await Task.FromResult(results);
    }

    /// <summary>
    /// Applies diversity enhancement to results.
    /// </summary>
    private List<VehicleResult> ApplyDiversityEnhancement(
        List<VehicleResult> results,
        int maxPerMake,
        int maxPerModel)
    {
        var diverse = new List<VehicleResult>();
        var makeCount = new Dictionary<string, int>();
        var modelCount = new Dictionary<string, int>();

        foreach (var result in results.OrderByDescending(r => r.Score))
        {
            var make = result.Vehicle.Make;
            var model = result.Vehicle.Model;
            var modelKey = $"{make}:{model}";

            var currentMakeCount = makeCount.GetValueOrDefault(make, 0);
            var currentModelCount = modelCount.GetValueOrDefault(modelKey, 0);

            if (currentMakeCount < maxPerMake && currentModelCount < maxPerModel)
            {
                diverse.Add(result);
                makeCount[make] = currentMakeCount + 1;
                modelCount[modelKey] = currentModelCount + 1;
            }
        }

        _logger.LogDebug(
            "Diversity enhancement reduced results from {Original} to {Diverse}",
            results.Count,
            diverse.Count);

        return diverse;
    }

    /// <summary>
    /// Computes exact match score based on constraint satisfaction.
    /// </summary>
    private double ComputeExactMatchScore(Vehicle vehicle, ComposedQuery query)
    {
        if (query == null || !query.ConstraintGroups.Any())
        {
            return 0.5; // Neutral if no constraints
        }

        var exactConstraints = query.ConstraintGroups
            .SelectMany(g => g.Constraints)
            .Where(c => c.Type == ConstraintType.Exact || c.Type == ConstraintType.Range)
            .ToList();

        if (!exactConstraints.Any())
        {
            return 0.5; // Neutral if no exact constraints
        }

        var matchCount = exactConstraints.Count(c => MatchesConstraint(vehicle, c));
        return (double)matchCount / exactConstraints.Count;
    }

    /// <summary>
    /// Checks if a vehicle matches a constraint.
    /// </summary>
    private bool MatchesConstraint(Vehicle vehicle, SearchConstraint constraint)
    {
        try
        {
            var value = GetPropertyValue(vehicle, constraint.FieldName);
            if (value == null)
            {
                return false;
            }

            return constraint.Operator switch
            {
                ConstraintOperator.Equals => value.Equals(constraint.Value),
                ConstraintOperator.LessThanOrEqual => Convert.ToDouble(value) <= Convert.ToDouble(constraint.Value),
                ConstraintOperator.GreaterThanOrEqual => Convert.ToDouble(value) >= Convert.ToDouble(constraint.Value),
                ConstraintOperator.Contains => value.ToString()?.Contains(constraint.Value?.ToString() ?? "", StringComparison.OrdinalIgnoreCase) == true,
                _ => false
            };
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets property value from vehicle using reflection.
    /// </summary>
    private object? GetPropertyValue(Vehicle vehicle, string propertyName)
    {
        var property = typeof(Vehicle).GetProperty(propertyName);
        return property?.GetValue(vehicle);
    }

    /// <summary>
    /// Computes price competitiveness score.
    /// </summary>
    private double ComputePriceScore(Vehicle vehicle, List<VehicleResult> allResults)
    {
        if (!allResults.Any() || allResults.Count == 1)
        {
            return 0.5; // Neutral if no comparison
        }

        var prices = allResults.Select(r => r.Vehicle.Price).ToList();
        var minPrice = prices.Min();
        var maxPrice = prices.Max();

        if (maxPrice == minPrice)
        {
            return 0.5; // All same price
        }

        // Lower price = higher score (inverted normalization)
        return 1.0 - (double)((vehicle.Price - minPrice) / (maxPrice - minPrice));
    }

    /// <summary>
    /// Computes vehicle condition score.
    /// </summary>
    private double ComputeConditionScore(Vehicle vehicle)
    {
        var score = 0.0;

        // Service history (30%)
        if (vehicle.ServiceHistoryPresent)
        {
            score += 0.3;
        }

        // Mileage (20%)
        if (vehicle.Mileage < 50000)
        {
            score += 0.2;
        }
        else if (vehicle.Mileage < 80000)
        {
            score += 0.1;
        }

        // MOT validity (20%)
        if (vehicle.MotExpiryDate.HasValue)
        {
            var daysRemaining = (vehicle.MotExpiryDate.Value - DateTime.UtcNow).TotalDays;
            if (daysRemaining > 90)
            {
                score += 0.2;
            }
            else if (daysRemaining > 30)
            {
                score += 0.1;
            }
        }

        // Service count (20%)
        if (vehicle.NumberOfServices.HasValue)
        {
            if (vehicle.NumberOfServices.Value >= 5)
            {
                score += 0.2;
            }
            else if (vehicle.NumberOfServices.Value >= 3)
            {
                score += 0.1;
            }
        }

        // No damage declarations (10%)
        var hasDamage = vehicle.Declarations.Any(d =>
            d.Contains("damage", StringComparison.OrdinalIgnoreCase) ||
            d.Contains("accident", StringComparison.OrdinalIgnoreCase));
        
        if (!hasDamage)
        {
            score += 0.1;
        }

        return Math.Min(1.0, score);
    }

    /// <summary>
    /// Computes recency score based on vehicle age.
    /// </summary>
    private double ComputeRecencyScore(Vehicle vehicle)
    {
        if (!vehicle.RegistrationDate.HasValue)
        {
            return 0.5; // Neutral if date unknown
        }

        var age = DateTime.UtcNow.Year - vehicle.RegistrationDate.Value.Year;

        return age switch
        {
            <= 1 => 1.0,   // Less than 1 year
            <= 3 => 0.8,   // 1-3 years
            <= 5 => 0.6,   // 3-5 years
            <= 10 => 0.4,  // 5-10 years
            _ => 0.2       // Over 10 years
        };
    }
}
