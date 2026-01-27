using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VehicleSearch.Core.Models;
using System.Text.Json;

namespace VehicleSearch.Infrastructure.AI;

/// <summary>
/// Configuration for qualitative term defaults.
/// </summary>
public class QualitativeTermsConfig
{
    /// <summary>
    /// Gets or sets the qualitative term mappings.
    /// Key: qualitative term (e.g., "affordable", "economical")
    /// Value: array of constraint definitions
    /// </summary>
    public Dictionary<string, List<ConstraintDefinition>> Terms { get; set; } = new();
}

/// <summary>
/// Represents a constraint definition from configuration.
/// </summary>
public class ConstraintDefinition
{
    public string FieldName { get; set; } = string.Empty;
    public string Operator { get; set; } = string.Empty;
    public JsonElement Value { get; set; }
}

/// <summary>
/// Service for parsing entity values into search constraints.
/// </summary>
public class ConstraintParser
{
    private readonly ILogger<ConstraintParser> _logger;
    private readonly OperatorInferenceService _operatorInference;
    private readonly QualitativeTermsConfig _qualitativeConfig;

    // Entity type to field name mapping
    private static readonly Dictionary<EntityType, string> EntityFieldMap = new()
    {
        [EntityType.Make] = "make",
        [EntityType.Model] = "model",
        [EntityType.Derivative] = "derivative",
        [EntityType.Price] = "price",
        [EntityType.PriceRange] = "price",
        [EntityType.Mileage] = "mileage",
        [EntityType.EngineSize] = "engineSize",
        [EntityType.FuelType] = "fuelType",
        [EntityType.Transmission] = "transmissionType",
        [EntityType.BodyType] = "bodyType",
        [EntityType.Colour] = "colour",
        [EntityType.Feature] = "features",
        [EntityType.Location] = "saleLocation",
        [EntityType.Year] = "registrationDate"
    };

    public ConstraintParser(
        ILogger<ConstraintParser> logger,
        OperatorInferenceService operatorInference,
        IOptions<QualitativeTermsConfig> qualitativeConfig)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _operatorInference = operatorInference ?? throw new ArgumentNullException(nameof(operatorInference));
        _qualitativeConfig = qualitativeConfig?.Value ?? new QualitativeTermsConfig(); // Use empty config if not provided
    }

    /// <summary>
    /// Parses an entity into a search constraint or multiple constraints for qualitative terms.
    /// </summary>
    public List<SearchConstraint> ParseEntity(ExtractedEntity entity, string? context)
    {
        if (entity == null)
        {
            throw new ArgumentNullException(nameof(entity));
        }

        _logger.LogDebug("Parsing entity: Type={Type}, Value={Value}, Context={Context}", 
            entity.Type, entity.Value, context);

        return entity.Type switch
        {
            EntityType.QualitativeTerm => ParseQualitativeTerm(entity, context),
            EntityType.Price => ParsePrice(entity, context),
            EntityType.PriceRange => ParsePriceRange(entity),
            EntityType.Mileage => ParseMileage(entity, context),
            EntityType.Make => ParseExact(entity, ConstraintType.Exact),
            EntityType.Model => ParseModel(entity),
            EntityType.Feature => ParseFeature(entity),
            EntityType.Location => ParseExact(entity, ConstraintType.Exact),
            EntityType.FuelType => ParseExact(entity, ConstraintType.Exact),
            EntityType.Transmission => ParseExact(entity, ConstraintType.Exact),
            EntityType.BodyType => ParseExact(entity, ConstraintType.Exact),
            EntityType.Colour => ParseExact(entity, ConstraintType.Exact),
            EntityType.Year => ParseYear(entity, context),
            _ => new List<SearchConstraint>()
        };
    }

    private List<SearchConstraint> ParseQualitativeTerm(ExtractedEntity entity, string? context)
    {
        var termLower = entity.Value.ToLowerInvariant();

        // Try to find in configured qualitative terms
        if (_qualitativeConfig.Terms.TryGetValue(termLower, out var definitions))
        {
            var constraints = new List<SearchConstraint>();
            foreach (var def in definitions)
            {
                var constraint = new SearchConstraint
                {
                    FieldName = def.FieldName,
                    Operator = ParseOperatorString(def.Operator),
                    Value = ParseJsonValue(def.Value),
                    Type = ConstraintType.Semantic
                };
                constraints.Add(constraint);
            }
            
            _logger.LogDebug("Mapped qualitative term '{Term}' to {Count} constraints", termLower, constraints.Count);
            return constraints;
        }

        _logger.LogWarning("No mapping found for qualitative term '{Term}'", termLower);
        return new List<SearchConstraint>();
    }

    private List<SearchConstraint> ParsePrice(ExtractedEntity entity, string? context)
    {
        if (!double.TryParse(entity.Value, out var price))
        {
            _logger.LogWarning("Failed to parse price value: {Value}", entity.Value);
            return new List<SearchConstraint>();
        }

        // Check if approximate (around, about)
        if (_operatorInference.IsApproximate(context))
        {
            var margin = price * 0.1; // ±10%
            return new List<SearchConstraint>
            {
                new SearchConstraint
                {
                    FieldName = "price",
                    Operator = ConstraintOperator.Between,
                    Value = new[] { price - margin, price + margin },
                    Type = ConstraintType.Range
                }
            };
        }

        // Infer operator from context
        var op = _operatorInference.InferOperator(context, ConstraintOperator.Equals);

        return new List<SearchConstraint>
        {
            new SearchConstraint
            {
                FieldName = "price",
                Operator = op,
                Value = price,
                Type = ConstraintType.Range
            }
        };
    }

    private List<SearchConstraint> ParsePriceRange(ExtractedEntity entity)
    {
        // Expected format: "15000-25000" or "15000,25000"
        var parts = entity.Value.Split(new[] { '-', ',' }, StringSplitOptions.RemoveEmptyEntries);
        
        if (parts.Length != 2 || 
            !double.TryParse(parts[0].Trim(), out var min) || 
            !double.TryParse(parts[1].Trim(), out var max))
        {
            _logger.LogWarning("Failed to parse price range: {Value}", entity.Value);
            return new List<SearchConstraint>();
        }

        return new List<SearchConstraint>
        {
            new SearchConstraint
            {
                FieldName = "price",
                Operator = ConstraintOperator.Between,
                Value = new[] { min, max },
                Type = ConstraintType.Range
            }
        };
    }

    private List<SearchConstraint> ParseMileage(ExtractedEntity entity, string? context)
    {
        if (!int.TryParse(entity.Value, out var mileage))
        {
            _logger.LogWarning("Failed to parse mileage value: {Value}", entity.Value);
            return new List<SearchConstraint>();
        }

        var op = _operatorInference.InferOperator(context, ConstraintOperator.Equals);

        return new List<SearchConstraint>
        {
            new SearchConstraint
            {
                FieldName = "mileage",
                Operator = op,
                Value = mileage,
                Type = ConstraintType.Range
            }
        };
    }

    private List<SearchConstraint> ParseExact(ExtractedEntity entity, ConstraintType type)
    {
        if (!EntityFieldMap.TryGetValue(entity.Type, out var fieldName))
        {
            _logger.LogWarning("No field mapping for entity type: {Type}", entity.Type);
            return new List<SearchConstraint>();
        }

        return new List<SearchConstraint>
        {
            new SearchConstraint
            {
                FieldName = fieldName,
                Operator = ConstraintOperator.Equals,
                Value = entity.Value,
                Type = type
            }
        };
    }

    private List<SearchConstraint> ParseModel(ExtractedEntity entity)
    {
        // Model uses fuzzy matching with Contains operator
        return new List<SearchConstraint>
        {
            new SearchConstraint
            {
                FieldName = "model",
                Operator = ConstraintOperator.Contains,
                Value = entity.Value,
                Type = ConstraintType.Exact
            }
        };
    }

    private List<SearchConstraint> ParseFeature(ExtractedEntity entity)
    {
        // Features use Contains operator for array field
        return new List<SearchConstraint>
        {
            new SearchConstraint
            {
                FieldName = "features",
                Operator = ConstraintOperator.Contains,
                Value = entity.Value,
                Type = ConstraintType.Exact
            }
        };
    }

    private List<SearchConstraint> ParseYear(ExtractedEntity entity, string? context)
    {
        if (!int.TryParse(entity.Value, out var year))
        {
            _logger.LogWarning("Failed to parse year value: {Value}", entity.Value);
            return new List<SearchConstraint>();
        }

        // Convert year to date
        var date = new DateTimeOffset(year, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var op = _operatorInference.InferOperator(context, ConstraintOperator.GreaterThanOrEqual);

        return new List<SearchConstraint>
        {
            new SearchConstraint
            {
                FieldName = "registrationDate",
                Operator = op,
                Value = date,
                Type = ConstraintType.Range
            }
        };
    }

    private ConstraintOperator ParseOperatorString(string operatorStr)
    {
        return operatorStr.ToLowerInvariant() switch
        {
            "equals" => ConstraintOperator.Equals,
            "notequals" => ConstraintOperator.NotEquals,
            "greaterthan" => ConstraintOperator.GreaterThan,
            "greaterthanorequal" => ConstraintOperator.GreaterThanOrEqual,
            "lessthan" => ConstraintOperator.LessThan,
            "lessthanorequal" => ConstraintOperator.LessThanOrEqual,
            "between" => ConstraintOperator.Between,
            "contains" => ConstraintOperator.Contains,
            "in" => ConstraintOperator.In,
            _ => ConstraintOperator.Equals
        };
    }

    private object ParseJsonValue(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.Number => value.TryGetInt32(out var intVal) ? intVal : value.GetDouble(),
            JsonValueKind.String => value.GetString() ?? string.Empty,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Array => value.EnumerateArray().Select(e => ParseJsonValue(e)).ToArray(),
            _ => value.ToString()
        };
    }
}
