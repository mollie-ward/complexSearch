using Microsoft.Extensions.Logging;
using VehicleSearch.Core.Models;
using System.Globalization;

namespace VehicleSearch.Infrastructure.AI;

/// <summary>
/// Detects and resolves conflicts in composed queries.
/// </summary>
public class ConflictResolver
{
    private readonly ILogger<ConflictResolver> _logger;

    public ConflictResolver(ILogger<ConflictResolver> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Detects conflicts within a composed query.
    /// </summary>
    /// <param name="query">The composed query to check.</param>
    /// <returns>List of detected conflicts.</returns>
    public List<string> DetectConflicts(ComposedQuery query)
    {
        if (query == null)
            throw new ArgumentNullException(nameof(query));

        var conflicts = new List<string>();

        foreach (var group in query.ConstraintGroups)
        {
            if (group.Operator == LogicalOperator.And)
            {
                conflicts.AddRange(DetectRangeConflicts(group.Constraints));
                conflicts.AddRange(DetectContradictoryValues(group.Constraints));
            }
        }

        return conflicts;
    }

    /// <summary>
    /// Resolves conflicts in a composed query.
    /// </summary>
    /// <param name="query">The query with conflicts.</param>
    /// <returns>A resolved query.</returns>
    public ComposedQuery ResolveConflicts(ComposedQuery query)
    {
        if (query == null)
            throw new ArgumentNullException(nameof(query));

        var resolved = new ComposedQuery
        {
            Type = query.Type,
            GroupOperator = query.GroupOperator,
            Warnings = new List<string>(query.Warnings)
        };

        foreach (var group in query.ConstraintGroups)
        {
            var resolvedGroup = ResolveGroupConflicts(group, resolved.Warnings);
            if (resolvedGroup.Constraints.Count > 0)
            {
                resolved.ConstraintGroups.Add(resolvedGroup);
            }
        }

        resolved.HasConflicts = resolved.Warnings.Any(w => w.Contains("conflict", StringComparison.OrdinalIgnoreCase));

        return resolved;
    }

    private ConstraintGroup ResolveGroupConflicts(ConstraintGroup group, List<string> warnings)
    {
        var resolvedGroup = new ConstraintGroup
        {
            Operator = group.Operator,
            Priority = group.Priority,
            Constraints = new List<SearchConstraint>(group.Constraints)
        };

        if (group.Operator == LogicalOperator.And)
        {
            // Merge overlapping ranges
            var rangeConstraints = resolvedGroup.Constraints
                .Where(c => IsRangeConstraint(c))
                .GroupBy(c => c.FieldName)
                .ToList();

            foreach (var fieldGroup in rangeConstraints)
            {
                var merged = MergeRangeConstraints(fieldGroup.ToList(), warnings);
                if (merged != null && merged.Any())
                {
                    // Remove original constraints
                    resolvedGroup.Constraints.RemoveAll(c => 
                        c.FieldName == fieldGroup.Key && IsRangeConstraint(c));
                    
                    // Add merged constraints
                    resolvedGroup.Constraints.AddRange(merged);
                }
            }
        }

        return resolvedGroup;
    }

    private List<string> DetectRangeConflicts(List<SearchConstraint> constraints)
    {
        var conflicts = new List<string>();
        var rangeConstraints = constraints
            .Where(c => IsRangeConstraint(c))
            .GroupBy(c => c.FieldName);

        foreach (var group in rangeConstraints)
        {
            var fieldName = group.Key;
            var rangeList = group.ToList();

            // Check for range inversions
            var minConstraints = rangeList.Where(c => 
                c.Operator == ConstraintOperator.GreaterThan || 
                c.Operator == ConstraintOperator.GreaterThanOrEqual).ToList();
            
            var maxConstraints = rangeList.Where(c => 
                c.Operator == ConstraintOperator.LessThan || 
                c.Operator == ConstraintOperator.LessThanOrEqual).ToList();

            foreach (var minConstraint in minConstraints)
            {
                foreach (var maxConstraint in maxConstraints)
                {
                    if (TryGetNumericValue(minConstraint.Value, out var minValue) &&
                        TryGetNumericValue(maxConstraint.Value, out var maxValue))
                    {
                        if (minValue > maxValue)
                        {
                            conflicts.Add($"Range inversion detected for {fieldName}: {minValue} > {maxValue}");
                        }
                    }
                }
            }
        }

        return conflicts;
    }

    private List<string> DetectContradictoryValues(List<SearchConstraint> constraints)
    {
        var conflicts = new List<string>();
        var exactConstraints = constraints
            .Where(c => c.Operator == ConstraintOperator.Equals)
            .GroupBy(c => c.FieldName);

        foreach (var group in exactConstraints)
        {
            var values = group.Select(c => c.Value?.ToString()).Distinct().ToList();
            if (values.Count > 1)
            {
                conflicts.Add($"Contradictory values for {group.Key}: {string.Join(", ", values)}");
            }
        }

        return conflicts;
    }

    private List<SearchConstraint>? MergeRangeConstraints(List<SearchConstraint> constraints, List<string> warnings)
    {
        if (constraints.Count <= 1)
            return constraints;

        var fieldName = constraints[0].FieldName;
        double? minValue = null;
        double? maxValue = null;
        bool hasMin = false;
        bool hasMax = false;
        bool minInclusive = false;
        bool maxInclusive = false;

        foreach (var constraint in constraints)
        {
            if (!TryGetNumericValue(constraint.Value, out var value))
                continue;

            switch (constraint.Operator)
            {
                case ConstraintOperator.GreaterThan:
                    if (!hasMin || value > minValue)
                    {
                        minValue = value;
                        minInclusive = false;
                        hasMin = true;
                    }
                    break;

                case ConstraintOperator.GreaterThanOrEqual:
                    if (!hasMin || value > minValue || (value == minValue && !minInclusive))
                    {
                        minValue = value;
                        minInclusive = true;
                        hasMin = true;
                    }
                    break;

                case ConstraintOperator.LessThan:
                    if (!hasMax || value < maxValue)
                    {
                        maxValue = value;
                        maxInclusive = false;
                        hasMax = true;
                    }
                    break;

                case ConstraintOperator.LessThanOrEqual:
                    if (!hasMax || value < maxValue || (value == maxValue && !maxInclusive))
                    {
                        maxValue = value;
                        maxInclusive = true;
                        hasMax = true;
                    }
                    break;

                case ConstraintOperator.Between:
                    if (constraint.Value is object[] range && range.Length == 2)
                    {
                        if (TryGetNumericValue(range[0], out var rangeMin) && 
                            TryGetNumericValue(range[1], out var rangeMax))
                        {
                            if (!hasMin || rangeMin > minValue)
                            {
                                minValue = rangeMin;
                                minInclusive = true;
                                hasMin = true;
                            }
                            if (!hasMax || rangeMax < maxValue)
                            {
                                maxValue = rangeMax;
                                maxInclusive = true;
                                hasMax = true;
                            }
                        }
                    }
                    break;
            }
        }

        // Check for impossible range
        if (hasMin && hasMax)
        {
            if (minValue > maxValue || (minValue == maxValue && (!minInclusive || !maxInclusive)))
            {
                warnings.Add($"Impossible range for {fieldName}: [{minValue}, {maxValue}]");
                return null;
            }
        }

        var merged = new List<SearchConstraint>();

        if (hasMin && hasMax)
        {
            // Create a Between constraint
            merged.Add(new SearchConstraint
            {
                FieldName = fieldName,
                Operator = ConstraintOperator.Between,
                Value = new object[] { minValue!, maxValue! },
                Type = constraints[0].Type
            });
            warnings.Add($"Merged overlapping ranges for {fieldName} into [{minValue}, {maxValue}]");
        }
        else if (hasMin)
        {
            merged.Add(new SearchConstraint
            {
                FieldName = fieldName,
                Operator = minInclusive ? ConstraintOperator.GreaterThanOrEqual : ConstraintOperator.GreaterThan,
                Value = minValue!,
                Type = constraints[0].Type
            });
        }
        else if (hasMax)
        {
            merged.Add(new SearchConstraint
            {
                FieldName = fieldName,
                Operator = maxInclusive ? ConstraintOperator.LessThanOrEqual : ConstraintOperator.LessThan,
                Value = maxValue!,
                Type = constraints[0].Type
            });
        }

        return merged;
    }

    private bool IsRangeConstraint(SearchConstraint constraint)
    {
        return constraint.Operator == ConstraintOperator.GreaterThan ||
               constraint.Operator == ConstraintOperator.GreaterThanOrEqual ||
               constraint.Operator == ConstraintOperator.LessThan ||
               constraint.Operator == ConstraintOperator.LessThanOrEqual ||
               constraint.Operator == ConstraintOperator.Between;
    }

    private bool TryGetNumericValue(object? value, out double result)
    {
        result = 0;

        if (value == null)
            return false;

        return value switch
        {
            int i => SetResult(i, out result),
            long l => SetResult(l, out result),
            float f => SetResult(f, out result),
            double d => SetResult(d, out result),
            decimal m => SetResult((double)m, out result),
            string s => double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out result),
            _ => false
        };

        static bool SetResult(double val, out double res)
        {
            res = val;
            return true;
        }
    }
}
