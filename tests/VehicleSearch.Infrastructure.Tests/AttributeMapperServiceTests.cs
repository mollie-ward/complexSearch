using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using VehicleSearch.Core.Models;
using VehicleSearch.Infrastructure.AI;
using System.Text.Json;

namespace VehicleSearch.Infrastructure.Tests;

public class AttributeMapperServiceTests
{
    private readonly Mock<ILogger<AttributeMapperService>> _loggerMock;
    private readonly Mock<ILogger<ConstraintParser>> _parserLoggerMock;
    private readonly Mock<ILogger<OperatorInferenceService>> _opLoggerMock;
    private readonly AttributeMapperService _service;
    private readonly ConstraintParser _parser;

    public AttributeMapperServiceTests()
    {
        _loggerMock = new Mock<ILogger<AttributeMapperService>>();
        _parserLoggerMock = new Mock<ILogger<ConstraintParser>>();
        _opLoggerMock = new Mock<ILogger<OperatorInferenceService>>();

        var operatorInference = new OperatorInferenceService(_opLoggerMock.Object);

        var config = new QualitativeTermsConfig
        {
            Terms = new Dictionary<string, List<ConstraintDefinition>>
            {
                ["affordable"] = new List<ConstraintDefinition>
                {
                    new ConstraintDefinition
                    {
                        FieldName = "price",
                        Operator = "LessThanOrEqual",
                        Value = JsonDocument.Parse("15000").RootElement
                    }
                },
                ["family car"] = new List<ConstraintDefinition>
                {
                    new ConstraintDefinition
                    {
                        FieldName = "numberOfDoors",
                        Operator = "GreaterThanOrEqual",
                        Value = JsonDocument.Parse("5").RootElement
                    },
                    new ConstraintDefinition
                    {
                        FieldName = "bodyType",
                        Operator = "In",
                        Value = JsonDocument.Parse("[\"SUV\", \"MPV\", \"Estate\", \"Hatchback\"]").RootElement
                    }
                }
            }
        };

        var configOptions = Options.Create(config);
        _parser = new ConstraintParser(_parserLoggerMock.Object, operatorInference, configOptions);
        _service = new AttributeMapperService(_loggerMock.Object, _parser);
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => new AttributeMapperService(null!, _parser);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithNullParser_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => new AttributeMapperService(_loggerMock.Object, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task MapToSearchQueryAsync_WithNullParsedQuery_ThrowsArgumentNullException()
    {
        // Act
        Func<Task> act = async () => await _service.MapToSearchQueryAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task MapToSearchQueryAsync_WithEmptyEntities_ReturnsEmptyConstraints()
    {
        // Arrange
        var parsedQuery = new ParsedQuery
        {
            OriginalQuery = "test",
            Intent = QueryIntent.Search,
            Entities = new List<ExtractedEntity>()
        };

        // Act
        var result = await _service.MapToSearchQueryAsync(parsedQuery);

        // Assert
        result.Should().NotBeNull();
        result.Constraints.Should().BeEmpty();
        result.Metadata.Should().ContainKey("totalConstraints");
        result.Metadata["totalConstraints"].Should().Be(0);
    }

    [Fact]
    public async Task MapToSearchQueryAsync_WithMakeEntity_CreatesMakeConstraint()
    {
        // Arrange
        var parsedQuery = new ParsedQuery
        {
            OriginalQuery = "BMW",
            Intent = QueryIntent.Search,
            Entities = new List<ExtractedEntity>
            {
                new ExtractedEntity
                {
                    Type = EntityType.Make,
                    Value = "BMW",
                    Confidence = 0.9
                }
            }
        };

        // Act
        var result = await _service.MapToSearchQueryAsync(parsedQuery);

        // Assert
        result.Should().NotBeNull();
        result.Constraints.Should().HaveCount(1);
        result.Constraints[0].FieldName.Should().Be("make");
        result.Constraints[0].Operator.Should().Be(ConstraintOperator.Equals);
        result.Constraints[0].Value.Should().Be("BMW");
        result.Constraints[0].Type.Should().Be(ConstraintType.Exact);
    }

    [Fact]
    public async Task MapToSearchQueryAsync_WithPriceUnder20k_CreatesPriceConstraint()
    {
        // Arrange
        var parsedQuery = new ParsedQuery
        {
            OriginalQuery = "under £20k",
            Intent = QueryIntent.Search,
            Entities = new List<ExtractedEntity>
            {
                new ExtractedEntity
                {
                    Type = EntityType.Price,
                    Value = "20000",
                    Confidence = 0.9
                }
            }
        };

        // Act
        var result = await _service.MapToSearchQueryAsync(parsedQuery);

        // Assert
        result.Should().NotBeNull();
        result.Constraints.Should().HaveCount(1);
        result.Constraints[0].FieldName.Should().Be("price");
        result.Constraints[0].Operator.Should().Be(ConstraintOperator.LessThanOrEqual);
        result.Constraints[0].Value.Should().Be(20000.0);
        result.Constraints[0].Type.Should().Be(ConstraintType.Range);
    }

    [Fact]
    public async Task MapToSearchQueryAsync_WithMultipleEntities_CreatesAllConstraints()
    {
        // Arrange
        var parsedQuery = new ParsedQuery
        {
            OriginalQuery = "BMW under 20k in Manchester",
            Intent = QueryIntent.Search,
            Entities = new List<ExtractedEntity>
            {
                new ExtractedEntity
                {
                    Type = EntityType.Make,
                    Value = "BMW",
                    Confidence = 0.9
                },
                new ExtractedEntity
                {
                    Type = EntityType.Price,
                    Value = "20000",
                    Confidence = 0.9
                },
                new ExtractedEntity
                {
                    Type = EntityType.Location,
                    Value = "Manchester",
                    Confidence = 0.8
                }
            }
        };

        // Act
        var result = await _service.MapToSearchQueryAsync(parsedQuery);

        // Assert
        result.Should().NotBeNull();
        result.Constraints.Should().HaveCount(3);
        
        var makeConstraint = result.Constraints.FirstOrDefault(c => c.FieldName == "make");
        makeConstraint.Should().NotBeNull();
        makeConstraint!.Value.Should().Be("BMW");

        var priceConstraint = result.Constraints.FirstOrDefault(c => c.FieldName == "price");
        priceConstraint.Should().NotBeNull();
        priceConstraint!.Operator.Should().Be(ConstraintOperator.LessThanOrEqual);

        var locationConstraint = result.Constraints.FirstOrDefault(c => c.FieldName == "saleLocation");
        locationConstraint.Should().NotBeNull();
        locationConstraint!.Value.Should().Be("Manchester");
    }

    [Fact]
    public async Task MapToSearchQueryAsync_WithQualitativeTerm_CreatesMultipleConstraints()
    {
        // Arrange
        var parsedQuery = new ParsedQuery
        {
            OriginalQuery = "family car",
            Intent = QueryIntent.Search,
            Entities = new List<ExtractedEntity>
            {
                new ExtractedEntity
                {
                    Type = EntityType.QualitativeTerm,
                    Value = "family car",
                    Confidence = 0.8
                }
            }
        };

        // Act
        var result = await _service.MapToSearchQueryAsync(parsedQuery);

        // Assert
        result.Should().NotBeNull();
        result.Constraints.Should().HaveCount(2);
        
        var doorsConstraint = result.Constraints.FirstOrDefault(c => c.FieldName == "numberOfDoors");
        doorsConstraint.Should().NotBeNull();
        doorsConstraint!.Operator.Should().Be(ConstraintOperator.GreaterThanOrEqual);

        var bodyTypeConstraint = result.Constraints.FirstOrDefault(c => c.FieldName == "bodyType");
        bodyTypeConstraint.Should().NotBeNull();
        bodyTypeConstraint!.Operator.Should().Be(ConstraintOperator.In);
    }

    [Fact]
    public async Task MapToSearchQueryAsync_SetsMetadataCorrectly()
    {
        // Arrange
        var parsedQuery = new ParsedQuery
        {
            OriginalQuery = "BMW under 20k",
            Intent = QueryIntent.Search,
            Entities = new List<ExtractedEntity>
            {
                new ExtractedEntity
                {
                    Type = EntityType.Make,
                    Value = "BMW",
                    Confidence = 0.9
                },
                new ExtractedEntity
                {
                    Type = EntityType.Price,
                    Value = "20000",
                    Confidence = 0.9
                }
            }
        };

        // Act
        var result = await _service.MapToSearchQueryAsync(parsedQuery);

        // Assert
        result.Metadata.Should().ContainKey("totalConstraints");
        result.Metadata["totalConstraints"].Should().Be(2);
        
        result.Metadata.Should().ContainKey("exactMatches");
        result.Metadata["exactMatches"].Should().Be(1);
        
        result.Metadata.Should().ContainKey("rangeFilters");
        result.Metadata["rangeFilters"].Should().Be(1);
    }

    [Fact]
    public async Task MapToSearchQueryAsync_WithUnmappableTerms_AddsToUnmappableList()
    {
        // Arrange
        var parsedQuery = new ParsedQuery
        {
            OriginalQuery = "test",
            Intent = QueryIntent.Search,
            Entities = new List<ExtractedEntity>(),
            UnmappedTerms = new List<string> { "something" }
        };

        // Act
        var result = await _service.MapToSearchQueryAsync(parsedQuery);

        // Assert
        result.UnmappableTerms.Should().Contain("something");
    }

    [Fact]
    public async Task ParseConstraintAsync_WithNullEntity_ThrowsArgumentNullException()
    {
        // Act
        Func<Task> act = async () => await _service.ParseConstraintAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ParseConstraintAsync_WithValidEntity_ReturnsConstraint()
    {
        // Arrange
        var entity = new ExtractedEntity
        {
            Type = EntityType.Make,
            Value = "BMW",
            Confidence = 0.9
        };

        // Act
        var result = await _service.ParseConstraintAsync(entity);

        // Assert
        result.Should().NotBeNull();
        result!.FieldName.Should().Be("make");
        result.Operator.Should().Be(ConstraintOperator.Equals);
        result.Value.Should().Be("BMW");
    }

    [Fact]
    public async Task ParseConstraintAsync_WithContext_UsesContextForOperator()
    {
        // Arrange
        var entity = new ExtractedEntity
        {
            Type = EntityType.Price,
            Value = "20000",
            Confidence = 0.9
        };

        // Act
        var result = await _service.ParseConstraintAsync(entity, "under");

        // Assert
        result.Should().NotBeNull();
        result!.Operator.Should().Be(ConstraintOperator.LessThanOrEqual);
    }
}
