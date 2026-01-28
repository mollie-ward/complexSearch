using Microsoft.Extensions.Logging;
using VehicleSearch.Core.Entities;
using VehicleSearch.Core.Models;

namespace VehicleSearch.Infrastructure.AI;

/// <summary>
/// Computes multi-factor similarity scores between vehicles and conceptual mappings.
/// </summary>
public class SimilarityScorer
{
    private readonly ILogger<SimilarityScorer> _logger;
    private const double MaxDescriptionBoost = 0.5;
    private const double PositiveIndicatorBoost = 0.05;
    private const double NegativeIndicatorPenalty = 0.10;

    public SimilarityScorer(ILogger<SimilarityScorer> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Computes the similarity score between a vehicle and a conceptual mapping.
    /// </summary>
    public SimilarityScore ComputeScore(Vehicle vehicle, ConceptualMapping concept)
    {
        ArgumentNullException.ThrowIfNull(vehicle);
        ArgumentNullException.ThrowIfNull(concept);

        var componentScores = new Dictionary<string, double>();
        var matchingAttributes = new List<string>();
        var mismatchingAttributes = new List<string>();
        double totalWeightedScore = 0.0;

        // Compute score for each attribute
        foreach (var attributeWeight in concept.AttributeWeights)
        {
            var attributeScore = ComputeAttributeScore(vehicle, attributeWeight);
            componentScores[attributeWeight.Attribute] = attributeScore;

            // Track matching/mismatching attributes
            if (attributeScore >= 0.5)
            {
                matchingAttributes.Add(attributeWeight.Attribute);
            }
            else
            {
                mismatchingAttributes.Add(attributeWeight.Attribute);
            }

            // Apply weight
            totalWeightedScore += attributeScore * attributeWeight.Weight;
        }

        // Compute description boost
        var descriptionBoost = ComputeDescriptionBoost(vehicle.Description, concept);

        // Calculate final score with boost
        var finalScore = Math.Clamp(totalWeightedScore + descriptionBoost, 0.0, 1.0);

        _logger.LogDebug(
            "Computed similarity score for vehicle {VehicleId} against concept {Concept}: base={BaseScore:F3}, boost={Boost:F3}, final={FinalScore:F3}",
            vehicle.Id, concept.Concept, totalWeightedScore, descriptionBoost, finalScore);

        return new SimilarityScore
        {
            OverallScore = finalScore,
            ComponentScores = componentScores,
            MatchingAttributes = matchingAttributes,
            MismatchingAttributes = mismatchingAttributes,
            DescriptionBoost = descriptionBoost
        };
    }

    /// <summary>
    /// Computes the score for a single attribute based on comparison type.
    /// </summary>
    private double ComputeAttributeScore(Vehicle vehicle, AttributeWeight attributeWeight)
    {
        var actualValue = GetAttributeValue(vehicle, attributeWeight.Attribute);
        
        if (actualValue == null)
        {
            _logger.LogDebug("Attribute {Attribute} not found on vehicle {VehicleId}", 
                attributeWeight.Attribute, vehicle.Id);
            return 0.0;
        }

        return attributeWeight.ComparisonType.ToLowerInvariant() switch
        {
            "less" => ComputeLessThanScore(actualValue, attributeWeight.TargetValue),
            "greater" => ComputeGreaterThanScore(actualValue, attributeWeight.TargetValue),
            "lessorequal" => ComputeLessOrEqualScore(actualValue, attributeWeight.TargetValue),
            "greaterorequal" => ComputeGreaterOrEqualScore(actualValue, attributeWeight.TargetValue),
            "equals" => ComputeEqualsScore(actualValue, attributeWeight.TargetValue),
            "in" => ComputeInScore(actualValue, attributeWeight.TargetValue),
            "contains" => ComputeContainsScore(actualValue, attributeWeight.TargetValue),
            "containsany" => ComputeContainsAnyScore(actualValue, attributeWeight.TargetValue),
            _ => 0.0
        };
    }

    /// <summary>
    /// Linear decay scoring for "less than" comparisons.
    /// ≤70% of target = 1.0, >130% = 0.2
    /// </summary>
    private double ComputeLessThanScore(object? actual, object? target)
    {
        if (!TryConvertToDouble(actual, out var actualValue) || 
            !TryConvertToDouble(target, out var targetValue))
        {
            return 0.0;
        }

        if (actualValue <= targetValue * 0.7)
            return 1.0;
        
        if (actualValue >= targetValue * 1.3)
            return 0.2;

        // Linear interpolation between 70% and 130%
        var ratio = actualValue / targetValue;
        return 1.0 - ((ratio - 0.7) / (1.3 - 0.7)) * 0.8;
    }

    /// <summary>
    /// Inverse of "less than" scoring for "greater than" comparisons.
    /// </summary>
    private double ComputeGreaterThanScore(object? actual, object? target)
    {
        if (!TryConvertToDouble(actual, out var actualValue) || 
            !TryConvertToDouble(target, out var targetValue))
        {
            return 0.0;
        }

        if (actualValue >= targetValue * 1.3)
            return 1.0;
        
        if (actualValue <= targetValue * 0.7)
            return 0.2;

        // Linear interpolation between 70% and 130%
        var ratio = actualValue / targetValue;
        return 0.2 + ((ratio - 0.7) / (1.3 - 0.7)) * 0.8;
    }

    /// <summary>
    /// Scoring for "less than or equal" comparisons.
    /// </summary>
    private double ComputeLessOrEqualScore(object? actual, object? target)
    {
        if (!TryConvertToDouble(actual, out var actualValue) || 
            !TryConvertToDouble(target, out var targetValue))
        {
            return 0.0;
        }

        return actualValue <= targetValue ? 1.0 : 0.2;
    }

    /// <summary>
    /// Scoring for "greater than or equal" comparisons.
    /// </summary>
    private double ComputeGreaterOrEqualScore(object? actual, object? target)
    {
        if (!TryConvertToDouble(actual, out var actualValue) || 
            !TryConvertToDouble(target, out var targetValue))
        {
            return 0.0;
        }

        return actualValue >= targetValue ? 1.0 : 0.2;
    }

    /// <summary>
    /// Binary scoring for equality comparisons.
    /// </summary>
    private double ComputeEqualsScore(object? actual, object? target)
    {
        if (actual == null || target == null)
            return 0.0;

        return actual.ToString()?.Equals(target.ToString(), StringComparison.OrdinalIgnoreCase) == true ? 1.0 : 0.0;
    }

    /// <summary>
    /// Check if value is in an array.
    /// </summary>
    private double ComputeInScore(object? actual, object? target)
    {
        if (actual == null || target == null)
            return 0.0;

        if (target is not Array targetArray)
            return 0.0;

        var actualStr = actual.ToString();
        foreach (var item in targetArray)
        {
            if (item?.ToString()?.Equals(actualStr, StringComparison.OrdinalIgnoreCase) == true)
                return 1.0;
        }

        return 0.0;
    }

    /// <summary>
    /// Check if string contains a value.
    /// </summary>
    private double ComputeContainsScore(object? actual, object? target)
    {
        if (actual == null || target == null)
            return 0.0;

        var actualStr = actual.ToString() ?? string.Empty;
        var targetStr = target.ToString() ?? string.Empty;

        return actualStr.Contains(targetStr, StringComparison.OrdinalIgnoreCase) ? 1.0 : 0.0;
    }

    /// <summary>
    /// Check if array or string contains any of the target values.
    /// </summary>
    private double ComputeContainsAnyScore(object? actual, object? target)
    {
        if (actual == null || target == null)
            return 0.0;

        if (target is not Array targetArray)
            return 0.0;

        // If actual is an array (e.g., Features)
        if (actual is Array actualArray)
        {
            foreach (var actualItem in actualArray)
            {
                var actualStr = actualItem?.ToString()?.ToLowerInvariant();
                foreach (var targetItem in targetArray)
                {
                    var targetStr = targetItem?.ToString()?.ToLowerInvariant();
                    if (!string.IsNullOrEmpty(actualStr) && !string.IsNullOrEmpty(targetStr) &&
                        actualStr.Contains(targetStr))
                    {
                        return 1.0;
                    }
                }
            }
        }
        else
        {
            // If actual is a string
            var actualStr = actual.ToString()?.ToLowerInvariant() ?? string.Empty;
            foreach (var targetItem in targetArray)
            {
                var targetStr = targetItem?.ToString()?.ToLowerInvariant();
                if (!string.IsNullOrEmpty(targetStr) && actualStr.Contains(targetStr))
                {
                    return 1.0;
                }
            }
        }

        return 0.0;
    }

    /// <summary>
    /// Computes description boost based on positive/negative indicators.
    /// +0.05 per positive indicator, -0.10 per negative indicator, max ±0.5
    /// </summary>
    private double ComputeDescriptionBoost(string description, ConceptualMapping concept)
    {
        if (string.IsNullOrWhiteSpace(description))
            return 0.0;

        var descriptionLower = description.ToLowerInvariant();
        double boost = 0.0;

        // Add boost for positive indicators
        foreach (var indicator in concept.PositiveIndicators)
        {
            if (descriptionLower.Contains(indicator.ToLowerInvariant()))
            {
                boost += PositiveIndicatorBoost;
            }
        }

        // Subtract for negative indicators
        foreach (var indicator in concept.NegativeIndicators)
        {
            if (descriptionLower.Contains(indicator.ToLowerInvariant()))
            {
                boost -= NegativeIndicatorPenalty;
            }
        }

        // Clamp to ±0.5
        return Math.Clamp(boost, -MaxDescriptionBoost, MaxDescriptionBoost);
    }

    /// <summary>
    /// Gets the value of an attribute from a vehicle.
    /// </summary>
    private object? GetAttributeValue(Vehicle vehicle, string attributeName)
    {
        return attributeName.ToLowerInvariant() switch
        {
            "mileage" => vehicle.Mileage,
            "price" => vehicle.Price,
            "enginesize" => vehicle.EngineSize,
            "numberofdoors" => vehicle.NumberOfDoors,
            "fueltype" => vehicle.FuelType,
            "bodytype" => vehicle.BodyType,
            "transmissiontype" => vehicle.TransmissionType,
            "make" => vehicle.Make,
            "model" => vehicle.Model,
            "features" => vehicle.Features.ToArray(),
            "description" => vehicle.Description,
            "servicehistorypresent" => vehicle.ServiceHistoryPresent,
            "motexpirydate" => vehicle.MotExpiryDate != null 
                // Calculate days until expiry (positive if in future, negative if expired)
                // Note: This uses DateTime.UtcNow for consistency. In production, consider using ISystemClock
                ? (vehicle.MotExpiryDate.Value.Date - DateTime.UtcNow.Date).Days
                : (object?)null,
            _ => null
        };
    }

    /// <summary>
    /// Tries to convert an object to a double for numeric comparisons.
    /// </summary>
    private bool TryConvertToDouble(object? value, out double result)
    {
        result = 0.0;

        if (value == null)
            return false;

        return value switch
        {
            double d => SetResult(d, out result),
            int i => SetResult(i, out result),
            long l => SetResult(l, out result),
            float f => SetResult(f, out result),
            decimal dec => SetResult((double)dec, out result),
            _ => double.TryParse(value.ToString(), out result)
        };

        static bool SetResult(double d, out double res)
        {
            res = d;
            return true;
        }
    }
}
