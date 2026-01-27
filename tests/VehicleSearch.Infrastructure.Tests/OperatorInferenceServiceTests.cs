using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using VehicleSearch.Core.Models;
using VehicleSearch.Infrastructure.AI;

namespace VehicleSearch.Infrastructure.Tests;

public class OperatorInferenceServiceTests
{
    private readonly Mock<ILogger<OperatorInferenceService>> _loggerMock;
    private readonly OperatorInferenceService _service;

    public OperatorInferenceServiceTests()
    {
        _loggerMock = new Mock<ILogger<OperatorInferenceService>>();
        _service = new OperatorInferenceService(_loggerMock.Object);
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => new OperatorInferenceService(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData("under", ConstraintOperator.LessThanOrEqual)]
    [InlineData("below", ConstraintOperator.LessThanOrEqual)]
    [InlineData("up to", ConstraintOperator.LessThanOrEqual)]
    [InlineData("less than", ConstraintOperator.LessThan)]
    public void InferOperator_LessThanKeywords_ReturnsLessThanOrLessThanOrEqual(string context, ConstraintOperator expected)
    {
        // Act
        var result = _service.InferOperator(context);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("over", ConstraintOperator.GreaterThanOrEqual)]
    [InlineData("above", ConstraintOperator.GreaterThanOrEqual)]
    [InlineData("at least", ConstraintOperator.GreaterThanOrEqual)]
    [InlineData("more than", ConstraintOperator.GreaterThan)]
    [InlineData("greater than", ConstraintOperator.GreaterThan)]
    public void InferOperator_GreaterThanKeywords_ReturnsGreaterThanOrGreaterThanOrEqual(string context, ConstraintOperator expected)
    {
        // Act
        var result = _service.InferOperator(context);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("between", ConstraintOperator.Between)]
    [InlineData("from", ConstraintOperator.Between)]
    public void InferOperator_BetweenKeywords_ReturnsBetween(string context, ConstraintOperator expected)
    {
        // Act
        var result = _service.InferOperator(context);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("around", ConstraintOperator.Between)]
    [InlineData("about", ConstraintOperator.Between)]
    [InlineData("approximately", ConstraintOperator.Between)]
    [InlineData("roughly", ConstraintOperator.Between)]
    public void InferOperator_ApproximateKeywords_ReturnsBetween(string context, ConstraintOperator expected)
    {
        // Act
        var result = _service.InferOperator(context);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("exactly", ConstraintOperator.Equals)]
    [InlineData("is", ConstraintOperator.Equals)]
    public void InferOperator_ExactKeywords_ReturnsEquals(string context, ConstraintOperator expected)
    {
        // Act
        var result = _service.InferOperator(context);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void InferOperator_NoContext_ReturnsDefaultOperator()
    {
        // Act
        var result = _service.InferOperator(null);

        // Assert
        result.Should().Be(ConstraintOperator.Equals);
    }

    [Fact]
    public void InferOperator_EmptyContext_ReturnsDefaultOperator()
    {
        // Act
        var result = _service.InferOperator("");

        // Assert
        result.Should().Be(ConstraintOperator.Equals);
    }

    [Fact]
    public void InferOperator_UnrecognizedContext_ReturnsDefaultOperator()
    {
        // Act
        var result = _service.InferOperator("some random context");

        // Assert
        result.Should().Be(ConstraintOperator.Equals);
    }

    [Fact]
    public void InferOperator_CustomDefaultOperator_ReturnsCustomDefault()
    {
        // Act
        var result = _service.InferOperator("some random context", ConstraintOperator.GreaterThan);

        // Assert
        result.Should().Be(ConstraintOperator.GreaterThan);
    }

    [Theory]
    [InlineData("around")]
    [InlineData("about")]
    [InlineData("approximately")]
    [InlineData("roughly")]
    public void IsApproximate_ApproximateKeywords_ReturnsTrue(string context)
    {
        // Act
        var result = _service.IsApproximate(context);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("under")]
    [InlineData("over")]
    [InlineData("exactly")]
    [InlineData("some random text")]
    public void IsApproximate_NonApproximateKeywords_ReturnsFalse(string context)
    {
        // Act
        var result = _service.IsApproximate(context);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsApproximate_NullContext_ReturnsFalse()
    {
        // Act
        var result = _service.IsApproximate(null);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsApproximate_EmptyContext_ReturnsFalse()
    {
        // Act
        var result = _service.IsApproximate("");

        // Assert
        result.Should().BeFalse();
    }
}
