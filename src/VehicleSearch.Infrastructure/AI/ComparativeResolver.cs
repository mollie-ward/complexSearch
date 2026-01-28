using Microsoft.Extensions.Logging;
using VehicleSearch.Core.Models;

namespace VehicleSearch.Infrastructure.AI;

/// <summary>
/// Helper service for resolving comparative terms against previous constraints.
/// </summary>
public class ComparativeResolver
{
    private readonly ILogger<ComparativeResolver> _logger;
    private const double DefaultPercentageChange = 0.10; // 10% change

    // Map comparative terms to field names and direction
    private static readonly Dictionary<string, (string FieldName, bool Increase)> ComparativeTerms = new()
    {
        ["cheaper"] = ("price", false),
        ["less expensive"] = ("price", false),
        ["more expensive"] = ("price", true),
        ["pricier"] = ("price", true),
        ["lower mileage"] = ("mileage", false),
        ["less mileage"] = ("mileage", false),
        ["higher mileage"] = ("mileage", true),
        ["more mileage"] = ("mileage", true),
        ["newer"] = ("registrationDate", true),
        ["older"] = ("registrationDate", false),
        ["bigger"] = ("size", true),
        ["larger"] = ("size", true),
        ["smaller"] = ("size", false)
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="ComparativeResolver"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public ComparativeResolver(ILogger<ComparativeResolver> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Resolves comparative terms against previous constraints.
    /// </summary>
    /// <param name="query">The query containing comparative terms.</param>
    /// <param name="activeFilters">The active filters from search state.</param>
    /// <returns>Dictionary of resolved constraints.</returns>
    public Dictionary<string, SearchConstraint> ResolveComparatives(string query, Dictionary<string, SearchConstraint> activeFilters)
    {
        var resolvedConstraints = new Dictionary<string, SearchConstraint>();
        var queryLower = query.ToLowerInvariant();

        foreach (var (term, (fieldName, increase)) in ComparativeTerms)
        {
            if (!queryLower.Contains(term))
                continue;

            _logger.LogDebug("Found comparative term '{Term}' for field '{FieldName}'", term, fieldName);

            // Check if there's a previous constraint for this field
            if (!activeFilters.TryGetValue(fieldName, out var previousConstraint))
            {
                _logger.LogDebug("No previous constraint found for field '{FieldName}', cannot resolve comparative", fieldName);
                continue;
            }

            // Resolve the comparative based on the previous constraint
            var resolvedConstraint = ApplyComparative(previousConstraint, increase, fieldName);
            if (resolvedConstraint != null)
            {
                resolvedConstraints[fieldName] = resolvedConstraint;
                _logger.LogInformation("Resolved comparative '{Term}': {FieldName} {Operator} {Value}", 
                    term, fieldName, resolvedConstraint.Operator, resolvedConstraint.Value);
            }
        }

        return resolvedConstraints;
    }

    /// <summary>
    /// Applies a comparative adjustment to a constraint.
    /// </summary>
    private SearchConstraint? ApplyComparative(SearchConstraint previousConstraint, bool increase, string fieldName)
    {
        // Extract the comparison value from the previous constraint
        object? baseValue = ExtractComparisonValue(previousConstraint);
        if (baseValue == null)
        {
            _logger.LogWarning("Could not extract comparison value from previous constraint for field '{FieldName}'", fieldName);
            return null;
        }

        // Apply the percentage change
        object? newValue = ApplyPercentageChange(baseValue, increase);
        if (newValue == null)
        {
            _logger.LogWarning("Could not apply percentage change to value {Value} for field '{FieldName}'", baseValue, fieldName);
            return null;
        }

        // Determine the new operator
        var newOperator = increase ? ConstraintOperator.GreaterThan : ConstraintOperator.LessThan;

        return new SearchConstraint
        {
            FieldName = fieldName,
            Operator = newOperator,
            Value = newValue,
            Type = previousConstraint.Type
        };
    }

    /// <summary>
    /// Extracts the comparison value from a constraint.
    /// </summary>
    private object? ExtractComparisonValue(SearchConstraint constraint)
    {
        return constraint.Operator switch
        {
            ConstraintOperator.Equals => constraint.Value,
            ConstraintOperator.LessThan => constraint.Value,
            ConstraintOperator.LessThanOrEqual => constraint.Value,
            ConstraintOperator.GreaterThan => constraint.Value,
            ConstraintOperator.GreaterThanOrEqual => constraint.Value,
            ConstraintOperator.Between when constraint.Value is object[] arr && arr.Length == 2 => arr[1], // Use upper bound
            _ => constraint.Value
        };
    }

    /// <summary>
    /// Applies a percentage change to a value.
    /// </summary>
    private object? ApplyPercentageChange(object baseValue, bool increase)
    {
        if (baseValue is int intValue)
        {
            var change = (int)(intValue * DefaultPercentageChange);
            return increase ? intValue + change : intValue - change;
        }

        if (baseValue is long longValue)
        {
            var change = (long)(longValue * DefaultPercentageChange);
            return increase ? longValue + change : longValue - change;
        }

        if (baseValue is double doubleValue)
        {
            var change = doubleValue * DefaultPercentageChange;
            return increase ? doubleValue + change : doubleValue - change;
        }

        if (baseValue is decimal decimalValue)
        {
            var change = decimalValue * (decimal)DefaultPercentageChange;
            return increase ? decimalValue + change : decimalValue - change;
        }

        if (baseValue is DateTime dateValue)
        {
            // For dates, add/subtract 1 year instead of percentage
            return increase ? dateValue.AddYears(1) : dateValue.AddYears(-1);
        }

        return null;
    }
}
