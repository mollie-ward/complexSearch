using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using VehicleSearch.Core.Models;
using VehicleSearch.Infrastructure.AI;
using System.Text.Json;

namespace VehicleSearch.Infrastructure.Tests;

public class ConstraintParserTests
{
    private readonly Mock<ILogger<ConstraintParser>> _loggerMock;
    private readonly Mock<ILogger<OperatorInferenceService>> _opLoggerMock;
    private readonly OperatorInferenceService _operatorInference;
    private readonly QualitativeTermsConfig _config;
    private readonly ConstraintParser _parser;

    public ConstraintParserTests()
    {
        _loggerMock = new Mock<ILogger<ConstraintParser>>();
        _opLoggerMock = new Mock<ILogger<OperatorInferenceService>>();
        _operatorInference = new OperatorInferenceService(_opLoggerMock.Object);

        _config = new QualitativeTermsConfig
        {
            Terms = new Dictionary<string, List<ConstraintDefinition>>
            {
                ["cheap"] = new List<ConstraintDefinition>
                {
                    new ConstraintDefinition
                    {
                        FieldName = "price",
                        Operator = "LessThanOrEqual",
                        Value = JsonDocument.Parse("12000").RootElement
                    }
                },
                ["economical"] = new List<ConstraintDefinition>
                {
                    new ConstraintDefinition
                    {
                        FieldName = "engineSize",
                        Operator = "LessThanOrEqual",
                        Value = JsonDocument.Parse("2.0").RootElement
                    },
                    new ConstraintDefinition
                    {
                        FieldName = "fuelType",
                        Operator = "In",
                        Value = JsonDocument.Parse("[\"Electric\", \"Hybrid\"]").RootElement
                    }
                },
                ["low mileage"] = new List<ConstraintDefinition>
                {
                    new ConstraintDefinition
                    {
                        FieldName = "mileage",
                        Operator = "LessThanOrEqual",
                        Value = JsonDocument.Parse("30000").RootElement
                    }
                }
            }
        };

        var configOptions = Options.Create(_config);
        _parser = new ConstraintParser(_loggerMock.Object, _operatorInference, configOptions);
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var configOptions = Options.Create(_config);

        // Act
        Action act = () => new ConstraintParser(null!, _operatorInference, configOptions);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ParseEntity_WithNullEntity_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => _parser.ParseEntity(null!, null);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ParseEntity_Make_CreatesExactConstraint()
    {
        // Arrange
        var entity = new ExtractedEntity
        {
            Type = EntityType.Make,
            Value = "BMW",
            Confidence = 0.9
        };

        // Act
        var result = _parser.ParseEntity(entity, null);

        // Assert
        result.Should().HaveCount(1);
        result[0].FieldName.Should().Be("make");
        result[0].Operator.Should().Be(ConstraintOperator.Equals);
        result[0].Value.Should().Be("BMW");
        result[0].Type.Should().Be(ConstraintType.Exact);
    }

    [Fact]
    public void ParseEntity_Model_CreatesContainsConstraint()
    {
        // Arrange
        var entity = new ExtractedEntity
        {
            Type = EntityType.Model,
            Value = "3 series",
            Confidence = 0.9
        };

        // Act
        var result = _parser.ParseEntity(entity, null);

        // Assert
        result.Should().HaveCount(1);
        result[0].FieldName.Should().Be("model");
        result[0].Operator.Should().Be(ConstraintOperator.Contains);
        result[0].Value.Should().Be("3 series");
        result[0].Type.Should().Be(ConstraintType.Exact);
    }

    [Fact]
    public void ParseEntity_Feature_CreatesContainsConstraint()
    {
        // Arrange
        var entity = new ExtractedEntity
        {
            Type = EntityType.Feature,
            Value = "leather",
            Confidence = 0.9
        };

        // Act
        var result = _parser.ParseEntity(entity, null);

        // Assert
        result.Should().HaveCount(1);
        result[0].FieldName.Should().Be("features");
        result[0].Operator.Should().Be(ConstraintOperator.Contains);
        result[0].Value.Should().Be("leather");
    }

    [Fact]
    public void ParseEntity_Price_Under20k_CreatesLessThanOrEqualConstraint()
    {
        // Arrange
        var entity = new ExtractedEntity
        {
            Type = EntityType.Price,
            Value = "20000",
            Confidence = 0.9
        };

        // Act
        var result = _parser.ParseEntity(entity, "under");

        // Assert
        result.Should().HaveCount(1);
        result[0].FieldName.Should().Be("price");
        result[0].Operator.Should().Be(ConstraintOperator.LessThanOrEqual);
        result[0].Value.Should().Be(20000.0);
        result[0].Type.Should().Be(ConstraintType.Range);
    }

    [Fact]
    public void ParseEntity_Price_Around20k_CreatesBetweenConstraint()
    {
        // Arrange
        var entity = new ExtractedEntity
        {
            Type = EntityType.Price,
            Value = "20000",
            Confidence = 0.9
        };

        // Act
        var result = _parser.ParseEntity(entity, "around");

        // Assert
        result.Should().HaveCount(1);
        result[0].FieldName.Should().Be("price");
        result[0].Operator.Should().Be(ConstraintOperator.Between);
        result[0].Type.Should().Be(ConstraintType.Range);

        var valueArray = result[0].Value as double[];
        valueArray.Should().NotBeNull();
        valueArray!.Length.Should().Be(2);
        valueArray[0].Should().BeApproximately(18000, 1); // 20000 - 10%
        valueArray[1].Should().BeApproximately(22000, 1); // 20000 + 10%
    }

    [Fact]
    public void ParseEntity_PriceRange_15kTo25k_CreatesBetweenConstraint()
    {
        // Arrange
        var entity = new ExtractedEntity
        {
            Type = EntityType.PriceRange,
            Value = "15000-25000",
            Confidence = 0.9
        };

        // Act
        var result = _parser.ParseEntity(entity, null);

        // Assert
        result.Should().HaveCount(1);
        result[0].FieldName.Should().Be("price");
        result[0].Operator.Should().Be(ConstraintOperator.Between);
        result[0].Type.Should().Be(ConstraintType.Range);

        var valueArray = result[0].Value as double[];
        valueArray.Should().NotBeNull();
        valueArray!.Length.Should().Be(2);
        valueArray[0].Should().Be(15000);
        valueArray[1].Should().Be(25000);
    }

    [Fact]
    public void ParseEntity_Mileage_Under50k_CreatesLessThanConstraint()
    {
        // Arrange
        var entity = new ExtractedEntity
        {
            Type = EntityType.Mileage,
            Value = "50000",
            Confidence = 0.9
        };

        // Act
        var result = _parser.ParseEntity(entity, "less than");

        // Assert
        result.Should().HaveCount(1);
        result[0].FieldName.Should().Be("mileage");
        result[0].Operator.Should().Be(ConstraintOperator.LessThan);
        result[0].Value.Should().Be(50000);
        result[0].Type.Should().Be(ConstraintType.Range);
    }

    [Fact]
    public void ParseEntity_QualitativeTerm_Cheap_CreatesPriceConstraint()
    {
        // Arrange
        var entity = new ExtractedEntity
        {
            Type = EntityType.QualitativeTerm,
            Value = "cheap",
            Confidence = 0.8
        };

        // Act
        var result = _parser.ParseEntity(entity, null);

        // Assert
        result.Should().HaveCount(1);
        result[0].FieldName.Should().Be("price");
        result[0].Operator.Should().Be(ConstraintOperator.LessThanOrEqual);
        result[0].Value.Should().Be(12000);
        result[0].Type.Should().Be(ConstraintType.Semantic);
    }

    [Fact]
    public void ParseEntity_QualitativeTerm_Economical_CreatesMultipleConstraints()
    {
        // Arrange
        var entity = new ExtractedEntity
        {
            Type = EntityType.QualitativeTerm,
            Value = "economical",
            Confidence = 0.8
        };

        // Act
        var result = _parser.ParseEntity(entity, null);

        // Assert
        result.Should().HaveCount(2);
        
        var engineConstraint = result.FirstOrDefault(c => c.FieldName == "engineSize");
        engineConstraint.Should().NotBeNull();
        engineConstraint!.Operator.Should().Be(ConstraintOperator.LessThanOrEqual);
        engineConstraint.Value.Should().Be(2.0);

        var fuelConstraint = result.FirstOrDefault(c => c.FieldName == "fuelType");
        fuelConstraint.Should().NotBeNull();
        fuelConstraint!.Operator.Should().Be(ConstraintOperator.In);
    }

    [Fact]
    public void ParseEntity_QualitativeTerm_LowMileage_Uses30kDefault()
    {
        // Arrange
        var entity = new ExtractedEntity
        {
            Type = EntityType.QualitativeTerm,
            Value = "low mileage",
            Confidence = 0.8
        };

        // Act
        var result = _parser.ParseEntity(entity, null);

        // Assert
        result.Should().HaveCount(1);
        result[0].FieldName.Should().Be("mileage");
        result[0].Operator.Should().Be(ConstraintOperator.LessThanOrEqual);
        result[0].Value.Should().Be(30000);
        result[0].Type.Should().Be(ConstraintType.Semantic);
    }

    [Fact]
    public void ParseEntity_Location_CreatesExactConstraint()
    {
        // Arrange
        var entity = new ExtractedEntity
        {
            Type = EntityType.Location,
            Value = "Manchester",
            Confidence = 0.9
        };

        // Act
        var result = _parser.ParseEntity(entity, null);

        // Assert
        result.Should().HaveCount(1);
        result[0].FieldName.Should().Be("saleLocation");
        result[0].Operator.Should().Be(ConstraintOperator.Equals);
        result[0].Value.Should().Be("Manchester");
    }

    [Fact]
    public void ParseEntity_Year_2024_CreatesDateConstraint()
    {
        // Arrange
        var entity = new ExtractedEntity
        {
            Type = EntityType.Year,
            Value = "2024",
            Confidence = 0.9
        };

        // Act
        var result = _parser.ParseEntity(entity, "or newer");

        // Assert
        result.Should().HaveCount(1);
        result[0].FieldName.Should().Be("registrationDate");
        result[0].Operator.Should().Be(ConstraintOperator.GreaterThanOrEqual);
        result[0].Type.Should().Be(ConstraintType.Range);

        var dateValue = result[0].Value as DateTimeOffset?;
        dateValue.Should().NotBeNull();
        dateValue!.Value.Year.Should().Be(2024);
    }

    [Fact]
    public void ParseEntity_InvalidPrice_ReturnsEmptyList()
    {
        // Arrange
        var entity = new ExtractedEntity
        {
            Type = EntityType.Price,
            Value = "invalid",
            Confidence = 0.9
        };

        // Act
        var result = _parser.ParseEntity(entity, null);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ParseEntity_InvalidMileage_ReturnsEmptyList()
    {
        // Arrange
        var entity = new ExtractedEntity
        {
            Type = EntityType.Mileage,
            Value = "not a number",
            Confidence = 0.9
        };

        // Act
        var result = _parser.ParseEntity(entity, null);

        // Assert
        result.Should().BeEmpty();
    }
}
