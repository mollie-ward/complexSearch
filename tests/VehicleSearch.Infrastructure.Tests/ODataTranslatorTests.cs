using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using VehicleSearch.Core.Models;
using VehicleSearch.Infrastructure.AI;

namespace VehicleSearch.Infrastructure.Tests;

public class ODataTranslatorTests
{
    private readonly Mock<ILogger<ODataTranslator>> _loggerMock;
    private readonly ODataTranslator _translator;

    public ODataTranslatorTests()
    {
        _loggerMock = new Mock<ILogger<ODataTranslator>>();
        _translator = new ODataTranslator(_loggerMock.Object);
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => new ODataTranslator(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ToODataFilter_NullQuery_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => _translator.ToODataFilter(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ToODataFilter_EmptyConstraintGroups_ReturnsEmptyString()
    {
        // Arrange
        var query = new ComposedQuery
        {
            ConstraintGroups = new List<ConstraintGroup>()
        };

        // Act
        var result = _translator.ToODataFilter(query);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ToODataFilter_EqualsOperator_GeneratesCorrectSyntax()
    {
        // Arrange
        var query = new ComposedQuery
        {
            GroupOperator = LogicalOperator.And,
            ConstraintGroups = new List<ConstraintGroup>
            {
                new ConstraintGroup
                {
                    Operator = LogicalOperator.And,
                    Constraints = new List<SearchConstraint>
                    {
                        new SearchConstraint
                        {
                            FieldName = "make",
                            Operator = ConstraintOperator.Equals,
                            Value = "BMW"
                        }
                    }
                }
            }
        };

        // Act
        var result = _translator.ToODataFilter(query);

        // Assert
        result.Should().Be("make eq 'BMW'");
    }

    [Fact]
    public void ToODataFilter_MultipleAndConstraints_GeneratesCorrectSyntax()
    {
        // Arrange
        var query = new ComposedQuery
        {
            GroupOperator = LogicalOperator.And,
            ConstraintGroups = new List<ConstraintGroup>
            {
                new ConstraintGroup
                {
                    Operator = LogicalOperator.And,
                    Constraints = new List<SearchConstraint>
                    {
                        new SearchConstraint
                        {
                            FieldName = "make",
                            Operator = ConstraintOperator.Equals,
                            Value = "BMW"
                        },
                        new SearchConstraint
                        {
                            FieldName = "price",
                            Operator = ConstraintOperator.LessThanOrEqual,
                            Value = 20000
                        }
                    }
                }
            }
        };

        // Act
        var result = _translator.ToODataFilter(query);

        // Assert
        result.Should().Contain("make eq 'BMW'");
        result.Should().Contain("price le 20000");
        result.Should().Contain(" and ");
    }

    [Fact]
    public void ToODataFilter_OrOperator_GeneratesCorrectSyntax()
    {
        // Arrange
        var query = new ComposedQuery
        {
            GroupOperator = LogicalOperator.Or,
            ConstraintGroups = new List<ConstraintGroup>
            {
                new ConstraintGroup
                {
                    Operator = LogicalOperator.And,
                    Constraints = new List<SearchConstraint>
                    {
                        new SearchConstraint
                        {
                            FieldName = "make",
                            Operator = ConstraintOperator.Equals,
                            Value = "BMW"
                        }
                    }
                },
                new ConstraintGroup
                {
                    Operator = LogicalOperator.And,
                    Constraints = new List<SearchConstraint>
                    {
                        new SearchConstraint
                        {
                            FieldName = "make",
                            Operator = ConstraintOperator.Equals,
                            Value = "Audi"
                        }
                    }
                }
            }
        };

        // Act
        var result = _translator.ToODataFilter(query);

        // Assert
        result.Should().Contain("make eq 'BMW'");
        result.Should().Contain("make eq 'Audi'");
        result.Should().Contain(" or ");
    }

    [Fact]
    public void ToODataFilter_BetweenOperator_GeneratesCorrectSyntax()
    {
        // Arrange
        var query = new ComposedQuery
        {
            GroupOperator = LogicalOperator.And,
            ConstraintGroups = new List<ConstraintGroup>
            {
                new ConstraintGroup
                {
                    Operator = LogicalOperator.And,
                    Constraints = new List<SearchConstraint>
                    {
                        new SearchConstraint
                        {
                            FieldName = "price",
                            Operator = ConstraintOperator.Between,
                            Value = new object[] { 15000, 25000 }
                        }
                    }
                }
            }
        };

        // Act
        var result = _translator.ToODataFilter(query);

        // Assert
        result.Should().Contain("price ge 15000");
        result.Should().Contain("price le 25000");
        result.Should().Contain(" and ");
    }

    [Fact]
    public void ToODataFilter_InOperator_GeneratesCorrectSyntax()
    {
        // Arrange
        var query = new ComposedQuery
        {
            GroupOperator = LogicalOperator.And,
            ConstraintGroups = new List<ConstraintGroup>
            {
                new ConstraintGroup
                {
                    Operator = LogicalOperator.And,
                    Constraints = new List<SearchConstraint>
                    {
                        new SearchConstraint
                        {
                            FieldName = "fuelType",
                            Operator = ConstraintOperator.In,
                            Value = new object[] { "Electric", "Hybrid" }
                        }
                    }
                }
            }
        };

        // Act
        var result = _translator.ToODataFilter(query);

        // Assert
        result.Should().Contain("search.in(fuelType");
        result.Should().Contain("Electric");
        result.Should().Contain("Hybrid");
    }

    [Fact]
    public void ToODataFilter_ContainsOperator_GeneratesCorrectSyntax()
    {
        // Arrange
        var query = new ComposedQuery
        {
            GroupOperator = LogicalOperator.And,
            ConstraintGroups = new List<ConstraintGroup>
            {
                new ConstraintGroup
                {
                    Operator = LogicalOperator.And,
                    Constraints = new List<SearchConstraint>
                    {
                        new SearchConstraint
                        {
                            FieldName = "description",
                            Operator = ConstraintOperator.Contains,
                            Value = "reliable"
                        }
                    }
                }
            }
        };

        // Act
        var result = _translator.ToODataFilter(query);

        // Assert
        result.Should().Contain("search.ismatch");
        result.Should().Contain("reliable");
    }

    [Theory]
    [InlineData(ConstraintOperator.GreaterThan, "price", 20000, "price gt 20000")]
    [InlineData(ConstraintOperator.GreaterThanOrEqual, "price", 20000, "price ge 20000")]
    [InlineData(ConstraintOperator.LessThan, "mileage", 50000, "mileage lt 50000")]
    [InlineData(ConstraintOperator.LessThanOrEqual, "mileage", 50000, "mileage le 50000")]
    [InlineData(ConstraintOperator.NotEquals, "make", "BMW", "make ne 'BMW'")]
    public void ToODataFilter_VariousOperators_GeneratesCorrectSyntax(
        ConstraintOperator op, string fieldName, object value, string expected)
    {
        // Arrange
        var query = new ComposedQuery
        {
            GroupOperator = LogicalOperator.And,
            ConstraintGroups = new List<ConstraintGroup>
            {
                new ConstraintGroup
                {
                    Operator = LogicalOperator.And,
                    Constraints = new List<SearchConstraint>
                    {
                        new SearchConstraint
                        {
                            FieldName = fieldName,
                            Operator = op,
                            Value = value
                        }
                    }
                }
            }
        };

        // Act
        var result = _translator.ToODataFilter(query);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void ToODataFilter_StringWithQuotes_EscapesQuotes()
    {
        // Arrange
        var query = new ComposedQuery
        {
            GroupOperator = LogicalOperator.And,
            ConstraintGroups = new List<ConstraintGroup>
            {
                new ConstraintGroup
                {
                    Operator = LogicalOperator.And,
                    Constraints = new List<SearchConstraint>
                    {
                        new SearchConstraint
                        {
                            FieldName = "description",
                            Operator = ConstraintOperator.Equals,
                            Value = "Owner's manual"
                        }
                    }
                }
            }
        };

        // Act
        var result = _translator.ToODataFilter(query);

        // Assert
        result.Should().Contain("''");
    }

    [Fact]
    public void ToODataFilter_BooleanValue_GeneratesLowercase()
    {
        // Arrange
        var query = new ComposedQuery
        {
            GroupOperator = LogicalOperator.And,
            ConstraintGroups = new List<ConstraintGroup>
            {
                new ConstraintGroup
                {
                    Operator = LogicalOperator.And,
                    Constraints = new List<SearchConstraint>
                    {
                        new SearchConstraint
                        {
                            FieldName = "isAvailable",
                            Operator = ConstraintOperator.Equals,
                            Value = true
                        }
                    }
                }
            }
        };

        // Act
        var result = _translator.ToODataFilter(query);

        // Assert
        result.Should().Contain("true");
        result.Should().NotContain("True");
    }
}
