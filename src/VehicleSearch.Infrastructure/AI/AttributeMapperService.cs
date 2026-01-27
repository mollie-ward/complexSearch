using Microsoft.Extensions.Logging;
using VehicleSearch.Core.Interfaces;
using VehicleSearch.Core.Models;

namespace VehicleSearch.Infrastructure.AI;

/// <summary>
/// Service for mapping extracted entities from natural language to search constraints.
/// </summary>
public class AttributeMapperService : IAttributeMapperService
{
    private readonly ILogger<AttributeMapperService> _logger;
    private readonly ConstraintParser _constraintParser;

    public AttributeMapperService(
        ILogger<AttributeMapperService> logger,
        ConstraintParser constraintParser)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _constraintParser = constraintParser ?? throw new ArgumentNullException(nameof(constraintParser));
    }

    /// <inheritdoc />
    public Task<MappedQuery> MapToSearchQueryAsync(ParsedQuery parsedQuery, CancellationToken cancellationToken = default)
    {
        if (parsedQuery == null)
        {
            throw new ArgumentNullException(nameof(parsedQuery));
        }

        _logger.LogInformation("Mapping parsed query with {EntityCount} entities to search constraints", 
            parsedQuery.Entities.Count);

        var mappedQuery = new MappedQuery
        {
            UnmappableTerms = new List<string>(parsedQuery.UnmappedTerms)
        };

        var allConstraints = new List<SearchConstraint>();

        // Extract context from original query for operator inference
        var context = parsedQuery.OriginalQuery;

        foreach (var entity in parsedQuery.Entities)
        {
            try
            {
                var constraints = _constraintParser.ParseEntity(entity, context);
                
                if (constraints.Any())
                {
                    allConstraints.AddRange(constraints);
                    _logger.LogDebug("Mapped entity {Type}={Value} to {Count} constraint(s)", 
                        entity.Type, entity.Value, constraints.Count);
                }
                else
                {
                    _logger.LogWarning("Could not map entity {Type}={Value} to any constraint", 
                        entity.Type, entity.Value);
                    mappedQuery.UnmappableTerms.Add(entity.Value);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error mapping entity {Type}={Value}", entity.Type, entity.Value);
                mappedQuery.UnmappableTerms.Add(entity.Value);
            }
        }

        mappedQuery.Constraints = allConstraints;

        // Build metadata
        mappedQuery.Metadata = new Dictionary<string, object>
        {
            ["totalConstraints"] = allConstraints.Count,
            ["exactMatches"] = allConstraints.Count(c => c.Type == ConstraintType.Exact),
            ["rangeFilters"] = allConstraints.Count(c => c.Type == ConstraintType.Range),
            ["semanticFilters"] = allConstraints.Count(c => c.Type == ConstraintType.Semantic),
            ["compositeFilters"] = allConstraints.Count(c => c.Type == ConstraintType.Composite)
        };

        _logger.LogInformation("Mapped query to {ConstraintCount} constraints ({ExactCount} exact, {RangeCount} range, {SemanticCount} semantic)",
            allConstraints.Count,
            mappedQuery.Metadata["exactMatches"],
            mappedQuery.Metadata["rangeFilters"],
            mappedQuery.Metadata["semanticFilters"]);

        return Task.FromResult(mappedQuery);
    }

    /// <inheritdoc />
    public Task<SearchConstraint?> ParseConstraintAsync(
        ExtractedEntity entity, 
        string? context = null, 
        CancellationToken cancellationToken = default)
    {
        if (entity == null)
        {
            throw new ArgumentNullException(nameof(entity));
        }

        var constraints = _constraintParser.ParseEntity(entity, context);
        
        // Return the first constraint, or null if none
        return Task.FromResult(constraints.FirstOrDefault());
    }
}
