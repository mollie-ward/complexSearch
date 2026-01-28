using Microsoft.Extensions.Logging;
using VehicleSearch.Core.Interfaces;
using VehicleSearch.Core.Models;

namespace VehicleSearch.Infrastructure.AI;

/// <summary>
/// Service for composing complex search queries from mapped constraints.
/// </summary>
public class QueryComposerService : IQueryComposerService
{
    private readonly ILogger<QueryComposerService> _logger;
    private readonly ConflictResolver _conflictResolver;
    private readonly ODataTranslator _odataTranslator;

    public QueryComposerService(
        ILogger<QueryComposerService> logger,
        ConflictResolver conflictResolver,
        ODataTranslator odataTranslator)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _conflictResolver = conflictResolver ?? throw new ArgumentNullException(nameof(conflictResolver));
        _odataTranslator = odataTranslator ?? throw new ArgumentNullException(nameof(odataTranslator));
    }

    /// <inheritdoc/>
    public Task<ComposedQuery> ComposeQueryAsync(MappedQuery mappedQuery, CancellationToken cancellationToken = default)
    {
        if (mappedQuery == null)
            throw new ArgumentNullException(nameof(mappedQuery));

        if (mappedQuery.Constraints == null || mappedQuery.Constraints.Count == 0)
        {
            _logger.LogWarning("MappedQuery has no constraints");
            return Task.FromResult(new ComposedQuery
            {
                Type = QueryType.Simple,
                GroupOperator = LogicalOperator.And,
                Warnings = new List<string> { "No constraints provided" }
            });
        }

        try
        {
            // Determine query type
            var queryType = DetermineQueryType(mappedQuery.Constraints);

            // Detect OR operators in constraints (check metadata if available)
            var hasOrOperator = DetectOrOperator(mappedQuery);

            // Group constraints by priority and type
            var constraintGroups = GroupConstraints(mappedQuery.Constraints, hasOrOperator);

            // Create composed query
            var composedQuery = new ComposedQuery
            {
                Type = queryType,
                ConstraintGroups = constraintGroups,
                GroupOperator = hasOrOperator ? LogicalOperator.Or : LogicalOperator.And,
                Warnings = new List<string>()
            };

            // Detect conflicts
            var conflicts = _conflictResolver.DetectConflicts(composedQuery);
            if (conflicts.Any())
            {
                composedQuery.HasConflicts = true;
                composedQuery.Warnings.AddRange(conflicts);
                _logger.LogWarning("Query has {Count} conflicts", conflicts.Count);
            }

            // Generate OData filter
            composedQuery.ODataFilter = _odataTranslator.ToODataFilter(composedQuery);

            _logger.LogInformation("Composed {Type} query with {Groups} constraint groups", 
                queryType, constraintGroups.Count);

            return Task.FromResult(composedQuery);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to compose query");
            throw;
        }
    }

    /// <inheritdoc/>
    public Task<bool> ValidateQueryAsync(ComposedQuery query, CancellationToken cancellationToken = default)
    {
        if (query == null)
            throw new ArgumentNullException(nameof(query));

        try
        {
            // Check for conflicts
            var conflicts = _conflictResolver.DetectConflicts(query);
            
            // Check for critical conflicts (range inversions, contradictory values)
            var hasCriticalConflicts = conflicts.Any(c => 
                c.Contains("inversion", StringComparison.OrdinalIgnoreCase) ||
                c.Contains("contradictory", StringComparison.OrdinalIgnoreCase));

            if (hasCriticalConflicts)
            {
                _logger.LogWarning("Query validation failed: critical conflicts detected");
                return Task.FromResult(false);
            }

            // Validate OData filter can be generated
            try
            {
                var odataFilter = _odataTranslator.ToODataFilter(query);
                if (string.IsNullOrWhiteSpace(odataFilter))
                {
                    _logger.LogWarning("Query validation failed: empty OData filter");
                    return Task.FromResult(false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Query validation failed: OData generation error");
                return Task.FromResult(false);
            }

            _logger.LogInformation("Query validation passed");
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Query validation error");
            throw;
        }
    }

    /// <inheritdoc/>
    public Task<ComposedQuery> ResolveConflictsAsync(ComposedQuery query, CancellationToken cancellationToken = default)
    {
        if (query == null)
            throw new ArgumentNullException(nameof(query));

        try
        {
            var resolved = _conflictResolver.ResolveConflicts(query);
            
            // Regenerate OData filter after resolution
            resolved.ODataFilter = _odataTranslator.ToODataFilter(resolved);

            _logger.LogInformation("Resolved query conflicts. Warnings: {Count}", resolved.Warnings.Count);
            
            return Task.FromResult(resolved);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resolve conflicts");
            throw;
        }
    }

    private QueryType DetermineQueryType(List<SearchConstraint> constraints)
    {
        if (constraints.Count == 1)
            return QueryType.Simple;

        var hasExact = constraints.Any(c => c.Type == ConstraintType.Exact);
        var hasRange = constraints.Any(c => c.Type == ConstraintType.Range);
        var hasSemantic = constraints.Any(c => c.Type == ConstraintType.Semantic);
        var hasComposite = constraints.Any(c => c.Type == ConstraintType.Composite);

        // MultiModal: requires different search strategies (semantic + exact/range)
        if (hasSemantic && (hasExact || hasRange))
            return QueryType.MultiModal;

        // Complex: mixed constraint types including composites
        if (hasComposite || (hasExact && hasRange && constraints.Count > 3))
            return QueryType.Complex;

        // Filtered: multiple exact/range constraints
        if ((hasExact || hasRange) && constraints.Count >= 2)
            return QueryType.Filtered;

        return QueryType.Simple;
    }

    private bool DetectOrOperator(MappedQuery mappedQuery)
    {
        // Check metadata for OR indicator
        if (mappedQuery.Metadata != null && 
            mappedQuery.Metadata.TryGetValue("hasOrOperator", out var hasOr))
        {
            if (hasOr is bool boolValue)
                return boolValue;
            if (hasOr is string strValue && bool.TryParse(strValue, out var parsedValue))
                return parsedValue;
        }

        // Check for OR keywords in unmappable terms
        if (mappedQuery.UnmappableTerms != null)
        {
            var orKeywords = new[] { "or", "either", "alternatively" };
            return mappedQuery.UnmappableTerms.Any(term => 
                orKeywords.Contains(term.ToLower()));
        }

        return false;
    }

    private List<ConstraintGroup> GroupConstraints(List<SearchConstraint> constraints, bool hasOrOperator)
    {
        var groups = new List<ConstraintGroup>();

        if (hasOrOperator)
        {
            // For OR queries, try to group by field or type
            var fieldGroups = constraints.GroupBy(c => c.FieldName);
            
            foreach (var fieldGroup in fieldGroups)
            {
                groups.Add(new ConstraintGroup
                {
                    Constraints = fieldGroup.ToList(),
                    Operator = LogicalOperator.Or,
                    Priority = GetPriority(fieldGroup.First())
                });
            }
        }
        else
        {
            // For AND queries, group by priority
            var highPriority = new List<SearchConstraint>();
            var mediumPriority = new List<SearchConstraint>();
            var lowPriority = new List<SearchConstraint>();

            foreach (var constraint in constraints)
            {
                var priority = GetPriority(constraint);
                if (priority >= 0.8)
                    highPriority.Add(constraint);
                else if (priority >= 0.5)
                    mediumPriority.Add(constraint);
                else
                    lowPriority.Add(constraint);
            }

            if (highPriority.Any())
            {
                groups.Add(new ConstraintGroup
                {
                    Constraints = highPriority,
                    Operator = LogicalOperator.And,
                    Priority = 1.0
                });
            }

            if (mediumPriority.Any())
            {
                groups.Add(new ConstraintGroup
                {
                    Constraints = mediumPriority,
                    Operator = LogicalOperator.And,
                    Priority = 0.6
                });
            }

            if (lowPriority.Any())
            {
                groups.Add(new ConstraintGroup
                {
                    Constraints = lowPriority,
                    Operator = LogicalOperator.And,
                    Priority = 0.3
                });
            }
        }

        // If no groups were created, create a default group
        if (groups.Count == 0 && constraints.Any())
        {
            groups.Add(new ConstraintGroup
            {
                Constraints = constraints,
                Operator = LogicalOperator.And,
                Priority = 1.0
            });
        }

        return groups;
    }

    private double GetPriority(SearchConstraint constraint)
    {
        // High priority: exact matches on key fields
        if (constraint.Type == ConstraintType.Exact && 
            (constraint.FieldName == "make" || constraint.FieldName == "model"))
        {
            return 1.0;
        }

        // High priority: explicit exact matches
        if (constraint.Operator == ConstraintOperator.Equals)
        {
            return 0.9;
        }

        // Medium priority: range filters
        if (constraint.Type == ConstraintType.Range)
        {
            return 0.6;
        }

        // Low priority: semantic/qualitative terms
        if (constraint.Type == ConstraintType.Semantic)
        {
            return 0.3;
        }

        // Default medium priority
        return 0.5;
    }
}
