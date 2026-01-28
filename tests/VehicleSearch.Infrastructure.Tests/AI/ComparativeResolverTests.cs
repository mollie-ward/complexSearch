using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using VehicleSearch.Core.Models;
using VehicleSearch.Infrastructure.AI;

namespace VehicleSearch.Infrastructure.Tests.AI;

public class ComparativeResolverTests
{
    private readonly Mock<ILogger<ComparativeResolver>> _loggerMock;
    private readonly ComparativeResolver _resolver;

    public ComparativeResolverTests()
    {
        _loggerMock = new Mock<ILogger<ComparativeResolver>>();
        _resolver = new ComparativeResolver(_loggerMock.Object);
    }

    #region Price Comparatives

    [Fact]
    public void ResolveComparatives_Cheaper_ReducesPriceBy10Percent()
    {
        // Arrange
        var query = "Show me cheaper ones";
        var activeFilters = new Dictionary<string, SearchConstraint>
        {
            ["price"] = new SearchConstraint
            {
                FieldName = "price",
                Operator = ConstraintOperator.LessThanOrEqual,
                Value = 20000,
                Type = ConstraintType.Range
            }
        };

        // Act
        var result = _resolver.ResolveComparatives(query, activeFilters);

        // Assert
        result.Should().ContainKey("price");
        result["price"].Operator.Should().Be(ConstraintOperator.LessThan);
        result["price"].Value.Should().Be(18000); // 20000 - 10% = 18000
    }

    [Fact]
    public void ResolveComparatives_MoreExpensive_IncreasesPriceBy10Percent()
    {
        // Arrange
        var query = "Show me more expensive options";
        var activeFilters = new Dictionary<string, SearchConstraint>
        {
            ["price"] = new SearchConstraint
            {
                FieldName = "price",
                Operator = ConstraintOperator.LessThanOrEqual,
                Value = 20000,
                Type = ConstraintType.Range
            }
        };

        // Act
        var result = _resolver.ResolveComparatives(query, activeFilters);

        // Assert
        result.Should().ContainKey("price");
        result["price"].Operator.Should().Be(ConstraintOperator.GreaterThan);
        result["price"].Value.Should().Be(22000); // 20000 + 10% = 22000
    }

    [Fact]
    public void ResolveComparatives_Cheaper_NoPreviousPrice_ReturnsEmpty()
    {
        // Arrange
        var query = "Show me cheaper ones";
        var activeFilters = new Dictionary<string, SearchConstraint>();

        // Act
        var result = _resolver.ResolveComparatives(query, activeFilters);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region Mileage Comparatives

    [Fact]
    public void ResolveComparatives_LowerMileage_ReducesMileageBy10Percent()
    {
        // Arrange
        var query = "Show me cars with lower mileage";
        var activeFilters = new Dictionary<string, SearchConstraint>
        {
            ["mileage"] = new SearchConstraint
            {
                FieldName = "mileage",
                Operator = ConstraintOperator.LessThanOrEqual,
                Value = 50000,
                Type = ConstraintType.Range
            }
        };

        // Act
        var result = _resolver.ResolveComparatives(query, activeFilters);

        // Assert
        result.Should().ContainKey("mileage");
        result["mileage"].Operator.Should().Be(ConstraintOperator.LessThan);
        result["mileage"].Value.Should().Be(45000); // 50000 - 10% = 45000
    }

    [Fact]
    public void ResolveComparatives_HigherMileage_IncreasesMileageBy10Percent()
    {
        // Arrange
        var query = "Show me cars with higher mileage";
        var activeFilters = new Dictionary<string, SearchConstraint>
        {
            ["mileage"] = new SearchConstraint
            {
                FieldName = "mileage",
                Operator = ConstraintOperator.LessThanOrEqual,
                Value = 50000,
                Type = ConstraintType.Range
            }
        };

        // Act
        var result = _resolver.ResolveComparatives(query, activeFilters);

        // Assert
        result.Should().ContainKey("mileage");
        result["mileage"].Operator.Should().Be(ConstraintOperator.GreaterThan);
        result["mileage"].Value.Should().Be(55000); // 50000 + 10% = 55000
    }

    #endregion

    #region Date Comparatives

    [Fact]
    public void ResolveComparatives_Newer_IncreasesDateBy1Year()
    {
        // Arrange
        var query = "Show me newer cars";
        var baseDate = new DateTime(2020, 1, 1);
        var activeFilters = new Dictionary<string, SearchConstraint>
        {
            ["registrationDate"] = new SearchConstraint
            {
                FieldName = "registrationDate",
                Operator = ConstraintOperator.GreaterThanOrEqual,
                Value = baseDate,
                Type = ConstraintType.Range
            }
        };

        // Act
        var result = _resolver.ResolveComparatives(query, activeFilters);

        // Assert
        result.Should().ContainKey("registrationDate");
        result["registrationDate"].Operator.Should().Be(ConstraintOperator.GreaterThan);
        var resultDate = (DateTime)result["registrationDate"].Value;
        resultDate.Should().Be(new DateTime(2021, 1, 1));
    }

    [Fact]
    public void ResolveComparatives_Older_DecreasesDateBy1Year()
    {
        // Arrange
        var query = "Show me older cars";
        var baseDate = new DateTime(2020, 1, 1);
        var activeFilters = new Dictionary<string, SearchConstraint>
        {
            ["registrationDate"] = new SearchConstraint
            {
                FieldName = "registrationDate",
                Operator = ConstraintOperator.GreaterThanOrEqual,
                Value = baseDate,
                Type = ConstraintType.Range
            }
        };

        // Act
        var result = _resolver.ResolveComparatives(query, activeFilters);

        // Assert
        result.Should().ContainKey("registrationDate");
        result["registrationDate"].Operator.Should().Be(ConstraintOperator.LessThan);
        var resultDate = (DateTime)result["registrationDate"].Value;
        resultDate.Should().Be(new DateTime(2019, 1, 1));
    }

    #endregion

    #region Multiple Comparatives

    [Fact]
    public void ResolveComparatives_MultipleComparatives_ResolvesAll()
    {
        // Arrange
        var query = "Show me cheaper cars with lower mileage";
        var activeFilters = new Dictionary<string, SearchConstraint>
        {
            ["price"] = new SearchConstraint
            {
                FieldName = "price",
                Operator = ConstraintOperator.LessThanOrEqual,
                Value = 20000,
                Type = ConstraintType.Range
            },
            ["mileage"] = new SearchConstraint
            {
                FieldName = "mileage",
                Operator = ConstraintOperator.LessThanOrEqual,
                Value = 50000,
                Type = ConstraintType.Range
            }
        };

        // Act
        var result = _resolver.ResolveComparatives(query, activeFilters);

        // Assert
        result.Should().HaveCount(2);
        result.Should().ContainKey("price");
        result.Should().ContainKey("mileage");
        result["price"].Value.Should().Be(18000);
        result["mileage"].Value.Should().Be(45000);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void ResolveComparatives_NoComparatives_ReturnsEmpty()
    {
        // Arrange
        var query = "Show me BMW cars";
        var activeFilters = new Dictionary<string, SearchConstraint>
        {
            ["make"] = new SearchConstraint
            {
                FieldName = "make",
                Operator = ConstraintOperator.Equals,
                Value = "BMW",
                Type = ConstraintType.Exact
            }
        };

        // Act
        var result = _resolver.ResolveComparatives(query, activeFilters);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ResolveComparatives_ComparativeWithDecimalValue_AppliesPercentageCorrectly()
    {
        // Arrange
        var query = "Show me cheaper ones";
        var activeFilters = new Dictionary<string, SearchConstraint>
        {
            ["price"] = new SearchConstraint
            {
                FieldName = "price",
                Operator = ConstraintOperator.LessThanOrEqual,
                Value = 25000.50,
                Type = ConstraintType.Range
            }
        };

        // Act
        var result = _resolver.ResolveComparatives(query, activeFilters);

        // Assert
        result.Should().ContainKey("price");
        var expectedValue = 25000.50 - (25000.50 * 0.10);
        result["price"].Value.Should().Be(expectedValue);
    }

    #endregion
}
