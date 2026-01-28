using Microsoft.Extensions.Logging;
using VehicleSearch.Core.Interfaces;
using VehicleSearch.Core.Models;

namespace VehicleSearch.Infrastructure.AI;

/// <summary>
/// Helper service for refining queries by combining new constraints with previous ones.
/// </summary>
public class QueryRefiner
{
    private readonly ILogger<QueryRefiner> _logger;
    private readonly IAttributeMapperService _attributeMapper;
    private readonly IQueryComposerService _queryComposer;

    /// <summary>
    /// Initializes a new instance of the <see cref="QueryRefiner"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="attributeMapper">The attribute mapper service.</param>
    /// <param name="queryComposer">The query composer service.</param>
    public QueryRefiner(
        ILogger<QueryRefiner> logger,
        IAttributeMapperService attributeMapper,
        IQueryComposerService queryComposer)
    {
        _logger = logger;
        _attributeMapper = attributeMapper;
        _queryComposer = queryComposer;
    }

    /// <summary>
    /// Refines a query by merging new constraints with previous ones.
    /// </summary>
    /// <param name="newQuery">The new parsed query.</param>
    /// <param name="searchState">The search state with active filters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A composed query with merged constraints.</returns>
    public async Task<ComposedQuery> RefineQueryAsync(
        ParsedQuery newQuery,
        SearchState searchState,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Refining query: '{Query}' with {Count} active filters", 
            newQuery.OriginalQuery, searchState.ActiveFilters.Count);

        // Map the new query to constraints
        var mappedQuery = await _attributeMapper.MapToSearchQueryAsync(newQuery, cancellationToken);

        // Merge with previous constraints
        var mergedConstraints = MergeConstraints(mappedQuery.Constraints, searchState.ActiveFilters);

        // Create a new mapped query with merged constraints
        var refinedMappedQuery = new MappedQuery
        {
            Constraints = mergedConstraints,
            UnmappableTerms = mappedQuery.UnmappableTerms,
            Metadata = mappedQuery.Metadata
        };

        // Compose the final query
        var composedQuery = await _queryComposer.ComposeQueryAsync(refinedMappedQuery, cancellationToken);

        _logger.LogInformation("Query refined successfully. Total constraints: {Count}", 
            composedQuery.ConstraintGroups.Sum(g => g.Constraints.Count));

        return composedQuery;
    }

    /// <summary>
    /// Merges new constraints with previous active filters.
    /// </summary>
    /// <param name="newConstraints">New constraints from the current query.</param>
    /// <param name="activeFilters">Active filters from search state.</param>
    /// <returns>List of merged constraints.</returns>
    private List<SearchConstraint> MergeConstraints(
        List<SearchConstraint> newConstraints,
        Dictionary<string, SearchConstraint> activeFilters)
    {
        // Create a dictionary to track constraints by field name
        var constraintDict = new Dictionary<string, SearchConstraint>();

        // Start with previous constraints
        foreach (var (fieldName, constraint) in activeFilters)
        {
            constraintDict[fieldName] = constraint;
        }

        // Add/update with new constraints (last value wins)
        foreach (var constraint in newConstraints)
        {
            if (constraintDict.ContainsKey(constraint.FieldName))
            {
                _logger.LogDebug("Updating constraint for field '{FieldName}': {OldValue} -> {NewValue}", 
                    constraint.FieldName, 
                    constraintDict[constraint.FieldName].Value, 
                    constraint.Value);
            }
            else
            {
                _logger.LogDebug("Adding new constraint for field '{FieldName}': {Value}", 
                    constraint.FieldName, constraint.Value);
            }

            constraintDict[constraint.FieldName] = constraint;
        }

        return constraintDict.Values.ToList();
    }
}
