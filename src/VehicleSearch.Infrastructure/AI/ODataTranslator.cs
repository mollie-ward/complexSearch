using Microsoft.Extensions.Logging;
using VehicleSearch.Core.Models;
using System.Text;

namespace VehicleSearch.Infrastructure.AI;

/// <summary>
/// Translates composed queries to Azure Search OData filter syntax.
/// </summary>
public class ODataTranslator
{
    private readonly ILogger<ODataTranslator> _logger;

    public ODataTranslator(ILogger<ODataTranslator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Converts a composed query to an OData filter string.
    /// </summary>
    /// <param name="query">The composed query.</param>
    /// <returns>An OData filter string.</returns>
    public string ToODataFilter(ComposedQuery query)
    {
        if (query == null)
            throw new ArgumentNullException(nameof(query));

        if (query.ConstraintGroups == null || query.ConstraintGroups.Count == 0)
        {
            _logger.LogWarning("Query has no constraint groups");
            return string.Empty;
        }

        try
        {
            var groupFilters = new List<string>();

            foreach (var group in query.ConstraintGroups)
            {
                if (group.Constraints == null || group.Constraints.Count == 0)
                    continue;

                var constraintExpressions = group.Constraints
                    .Select(c => ToODataExpression(c))
                    .Where(expr => !string.IsNullOrWhiteSpace(expr))
                    .ToList();

                if (constraintExpressions.Count == 0)
                    continue;

                var operatorStr = group.Operator.ToString().ToLower();
                var groupFilter = constraintExpressions.Count == 1
                    ? constraintExpressions[0]
                    : $"({string.Join($" {operatorStr} ", constraintExpressions)})";

                groupFilters.Add(groupFilter);
            }

            if (groupFilters.Count == 0)
            {
                _logger.LogWarning("No valid constraint expressions generated");
                return string.Empty;
            }

            var groupOperatorStr = query.GroupOperator.ToString().ToLower();
            var result = groupFilters.Count == 1
                ? groupFilters[0]
                : string.Join($" {groupOperatorStr} ", groupFilters);

            _logger.LogInformation("Generated OData filter: {Filter}", result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate OData filter");
            throw;
        }
    }

    /// <summary>
    /// Converts a single constraint to an OData expression.
    /// </summary>
    /// <param name="constraint">The search constraint.</param>
    /// <returns>An OData expression string.</returns>
    private string ToODataExpression(SearchConstraint constraint)
    {
        if (constraint == null)
            throw new ArgumentNullException(nameof(constraint));

        try
        {
            return constraint.Operator switch
            {
                ConstraintOperator.Equals => FormatEqualsExpression(constraint),
                ConstraintOperator.NotEquals => FormatNotEqualsExpression(constraint),
                ConstraintOperator.GreaterThan => $"{constraint.FieldName} gt {FormatValue(constraint.Value)}",
                ConstraintOperator.GreaterThanOrEqual => $"{constraint.FieldName} ge {FormatValue(constraint.Value)}",
                ConstraintOperator.LessThan => $"{constraint.FieldName} lt {FormatValue(constraint.Value)}",
                ConstraintOperator.LessThanOrEqual => $"{constraint.FieldName} le {FormatValue(constraint.Value)}",
                ConstraintOperator.Between => FormatBetweenExpression(constraint),
                ConstraintOperator.Contains => FormatContainsExpression(constraint),
                ConstraintOperator.In => FormatInExpression(constraint),
                _ => throw new NotSupportedException($"Operator {constraint.Operator} is not supported")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to convert constraint to OData expression: {Constraint}", constraint.FieldName);
            throw;
        }
    }

    private string FormatEqualsExpression(SearchConstraint constraint)
    {
        var value = FormatValue(constraint.Value);
        return $"{constraint.FieldName} eq {value}";
    }

    private string FormatNotEqualsExpression(SearchConstraint constraint)
    {
        var value = FormatValue(constraint.Value);
        return $"{constraint.FieldName} ne {value}";
    }

    private string FormatBetweenExpression(SearchConstraint constraint)
    {
        if (constraint.Value is not object[] range || range.Length != 2)
        {
            throw new ArgumentException("Between operator requires an array of two values", nameof(constraint));
        }

        var min = FormatValue(range[0]);
        var max = FormatValue(range[1]);
        return $"({constraint.FieldName} ge {min} and {constraint.FieldName} le {max})";
    }

    private string FormatContainsExpression(SearchConstraint constraint)
    {
        // For string fields, use search.ismatch
        // For collection fields, use lambda expressions
        var value = constraint.Value?.ToString() ?? string.Empty;
        value = value.Replace("'", "''"); // Escape single quotes
        return $"search.ismatch('{value}', '{constraint.FieldName}')";
    }

    private string FormatInExpression(SearchConstraint constraint)
    {
        if (constraint.Value is not object[] values || values.Length == 0)
        {
            throw new ArgumentException("In operator requires an array of values", nameof(constraint));
        }

        var formattedValues = values.Select(v => FormatValue(v)).ToList();
        var valueList = string.Join(",", formattedValues.Select(v => v.Trim('\'')));
        return $"search.in({constraint.FieldName}, '{valueList}', ',')";
    }

    private string FormatValue(object? value)
    {
        if (value == null)
            return "null";

        return value switch
        {
            string s => $"'{s.Replace("'", "''")}'", // Escape single quotes
            bool b => b.ToString().ToLower(),
            int or long or short or byte => value.ToString()!,
            float or double or decimal => value.ToString()!,
            DateTime dt => $"'{dt:yyyy-MM-ddTHH:mm:ssZ}'",
            DateTimeOffset dto => $"'{dto:yyyy-MM-ddTHH:mm:ssZ}'",
            _ => $"'{value.ToString()?.Replace("'", "''")}'"
        };
    }
}
