using Microsoft.Extensions.Logging;
using VehicleSearch.Core.Models;

namespace VehicleSearch.Infrastructure.AI;

/// <summary>
/// Service for inferring constraint operators from context keywords.
/// </summary>
public class OperatorInferenceService
{
    private readonly ILogger<OperatorInferenceService> _logger;

    // Keywords mapped to operators
    private static readonly Dictionary<string, ConstraintOperator> KeywordOperatorMap = new(StringComparer.OrdinalIgnoreCase)
    {
        // LessThan / LessThanOrEqual
        ["under"] = ConstraintOperator.LessThanOrEqual,
        ["below"] = ConstraintOperator.LessThanOrEqual,
        ["up to"] = ConstraintOperator.LessThanOrEqual,
        ["less than"] = ConstraintOperator.LessThan,
        ["fewer than"] = ConstraintOperator.LessThan,
        
        // GreaterThan / GreaterThanOrEqual
        ["over"] = ConstraintOperator.GreaterThanOrEqual,
        ["above"] = ConstraintOperator.GreaterThanOrEqual,
        ["at least"] = ConstraintOperator.GreaterThanOrEqual,
        ["more than"] = ConstraintOperator.GreaterThan,
        ["greater than"] = ConstraintOperator.GreaterThan,
        
        // Between
        ["between"] = ConstraintOperator.Between,
        ["from"] = ConstraintOperator.Between,
        
        // Approximate (maps to Between with ±10%)
        ["around"] = ConstraintOperator.Between,
        ["about"] = ConstraintOperator.Between,
        ["approximately"] = ConstraintOperator.Between,
        ["roughly"] = ConstraintOperator.Between,
        
        // Exact
        ["exactly"] = ConstraintOperator.Equals,
        ["is"] = ConstraintOperator.Equals
    };

    public OperatorInferenceService(ILogger<OperatorInferenceService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Infers the constraint operator from context keywords.
    /// </summary>
    /// <param name="context">The context string containing keywords (e.g., "under", "between").</param>
    /// <param name="defaultOperator">The default operator if no keyword is found.</param>
    /// <returns>The inferred constraint operator.</returns>
    public ConstraintOperator InferOperator(string? context, ConstraintOperator defaultOperator = ConstraintOperator.Equals)
    {
        if (string.IsNullOrWhiteSpace(context))
        {
            return defaultOperator;
        }

        var contextLower = context.ToLowerInvariant();

        foreach (var (keyword, op) in KeywordOperatorMap)
        {
            if (contextLower.Contains(keyword))
            {
                _logger.LogDebug("Inferred operator {Operator} from context keyword '{Keyword}'", op, keyword);
                return op;
            }
        }

        _logger.LogDebug("No operator keyword found in context '{Context}', using default {Operator}", context, defaultOperator);
        return defaultOperator;
    }

    /// <summary>
    /// Checks if the context indicates an approximate value (±10%).
    /// </summary>
    /// <param name="context">The context string.</param>
    /// <returns>True if the context indicates an approximate value.</returns>
    public bool IsApproximate(string? context)
    {
        if (string.IsNullOrWhiteSpace(context))
        {
            return false;
        }

        var contextLower = context.ToLowerInvariant();
        return contextLower.Contains("around") || 
               contextLower.Contains("about") || 
               contextLower.Contains("approximately") || 
               contextLower.Contains("roughly");
    }
}
