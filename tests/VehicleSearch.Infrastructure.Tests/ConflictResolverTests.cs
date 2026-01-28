using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using VehicleSearch.Core.Models;
using VehicleSearch.Infrastructure.AI;

namespace VehicleSearch.Infrastructure.Tests;

public class ConflictResolverTests
{
    private readonly Mock<ILogger<ConflictResolver>> _loggerMock;
    private readonly ConflictResolver _resolver;

    public ConflictResolverTests()
    {
        _loggerMock = new Mock<ILogger<ConflictResolver>>();
        _resolver = new ConflictResolver(_loggerMock.Object);
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => new ConflictResolver(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void DetectConflicts_NoConflicts_ReturnsEmptyList()
    {
        // Arrange
        var query = new ComposedQuery
        {
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
        var conflicts = _resolver.DetectConflicts(query);

        // Assert
        conflicts.Should().BeEmpty();
    }

    [Fact]
    public void DetectConflicts_RangeInversion_DetectsConflict()
    {
        // Arrange
        var query = new ComposedQuery
        {
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
                            Operator = ConstraintOperator.GreaterThanOrEqual,
                            Value = 30000
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
        var conflicts = _resolver.DetectConflicts(query);

        // Assert
        conflicts.Should().NotBeEmpty();
        conflicts.Should().Contain(c => c.Contains("inversion"));
    }

    [Fact]
    public void DetectConflicts_ContradictoryValues_DetectsConflict()
    {
        // Arrange
        var query = new ComposedQuery
        {
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
                            FieldName = "make",
                            Operator = ConstraintOperator.Equals,
                            Value = "Audi"
                        }
                    }
                }
            }
        };

        // Act
        var conflicts = _resolver.DetectConflicts(query);

        // Assert
        conflicts.Should().NotBeEmpty();
        conflicts.Should().Contain(c => c.Contains("Contradictory"));
    }

    [Fact]
    public void ResolveConflicts_OverlappingRanges_MergesRanges()
    {
        // Arrange
        var query = new ComposedQuery
        {
            ConstraintGroups = new List<ConstraintGroup>
            {
                new ConstraintGroup
                {
                    Operator = LogicalOperator.And,
                    Priority = 1.0,
                    Constraints = new List<SearchConstraint>
                    {
                        new SearchConstraint
                        {
                            FieldName = "price",
                            Operator = ConstraintOperator.GreaterThanOrEqual,
                            Value = 15000,
                            Type = ConstraintType.Range
                        },
                        new SearchConstraint
                        {
                            FieldName = "price",
                            Operator = ConstraintOperator.LessThanOrEqual,
                            Value = 25000,
                            Type = ConstraintType.Range
                        }
                    }
                }
            }
        };

        // Act
        var resolved = _resolver.ResolveConflicts(query);

        // Assert
        resolved.Should().NotBeNull();
        resolved.ConstraintGroups.Should().HaveCount(1);
        resolved.ConstraintGroups[0].Constraints.Should().HaveCount(1);
        resolved.ConstraintGroups[0].Constraints[0].Operator.Should().Be(ConstraintOperator.Between);
    }

    [Fact]
    public void ResolveConflicts_ImpossibleRange_AddsWarning()
    {
        // Arrange
        var query = new ComposedQuery
        {
            ConstraintGroups = new List<ConstraintGroup>
            {
                new ConstraintGroup
                {
                    Operator = LogicalOperator.And,
                    Priority = 1.0,
                    Constraints = new List<SearchConstraint>
                    {
                        new SearchConstraint
                        {
                            FieldName = "price",
                            Operator = ConstraintOperator.GreaterThanOrEqual,
                            Value = 30000,
                            Type = ConstraintType.Range
                        },
                        new SearchConstraint
                        {
                            FieldName = "price",
                            Operator = ConstraintOperator.LessThanOrEqual,
                            Value = 20000,
                            Type = ConstraintType.Range
                        }
                    }
                }
            }
        };

        // Act
        var resolved = _resolver.ResolveConflicts(query);

        // Assert
        resolved.Should().NotBeNull();
        resolved.Warnings.Should().Contain(w => w.Contains("Impossible range"));
    }

    [Fact]
    public void ResolveConflicts_OrOperator_DoesNotMerge()
    {
        // Arrange
        var query = new ComposedQuery
        {
            ConstraintGroups = new List<ConstraintGroup>
            {
                new ConstraintGroup
                {
                    Operator = LogicalOperator.Or,
                    Priority = 1.0,
                    Constraints = new List<SearchConstraint>
                    {
                        new SearchConstraint
                        {
                            FieldName = "price",
                            Operator = ConstraintOperator.LessThanOrEqual,
                            Value = 20000,
                            Type = ConstraintType.Range
                        },
                        new SearchConstraint
                        {
                            FieldName = "mileage",
                            Operator = ConstraintOperator.LessThanOrEqual,
                            Value = 30000,
                            Type = ConstraintType.Range
                        }
                    }
                }
            }
        };

        // Act
        var resolved = _resolver.ResolveConflicts(query);

        // Assert
        resolved.Should().NotBeNull();
        resolved.ConstraintGroups.Should().HaveCount(1);
        resolved.ConstraintGroups[0].Constraints.Should().HaveCount(2);
    }
}
