using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using VehicleSearch.Core.Models;
using VehicleSearch.Infrastructure.AI;

namespace VehicleSearch.Infrastructure.Tests;

public class QueryComposerServiceTests
{
    private readonly Mock<ILogger<QueryComposerService>> _loggerMock;
    private readonly Mock<ILogger<ConflictResolver>> _conflictLoggerMock;
    private readonly Mock<ILogger<ODataTranslator>> _odataLoggerMock;
    private readonly QueryComposerService _service;
    private readonly ConflictResolver _conflictResolver;
    private readonly ODataTranslator _odataTranslator;

    public QueryComposerServiceTests()
    {
        _loggerMock = new Mock<ILogger<QueryComposerService>>();
        _conflictLoggerMock = new Mock<ILogger<ConflictResolver>>();
        _odataLoggerMock = new Mock<ILogger<ODataTranslator>>();
        
        _conflictResolver = new ConflictResolver(_conflictLoggerMock.Object);
        _odataTranslator = new ODataTranslator(_odataLoggerMock.Object);
        _service = new QueryComposerService(_loggerMock.Object, _conflictResolver, _odataTranslator);
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => new QueryComposerService(null!, _conflictResolver, _odataTranslator);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithNullConflictResolver_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => new QueryComposerService(_loggerMock.Object, null!, _odataTranslator);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithNullODataTranslator_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => new QueryComposerService(_loggerMock.Object, _conflictResolver, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task ComposeQueryAsync_SingleConstraint_ReturnsSimpleQuery()
    {
        // Arrange
        var mappedQuery = new MappedQuery
        {
            Constraints = new List<SearchConstraint>
            {
                new SearchConstraint
                {
                    FieldName = "make",
                    Operator = ConstraintOperator.Equals,
                    Value = "BMW",
                    Type = ConstraintType.Exact
                }
            }
        };

        // Act
        var result = await _service.ComposeQueryAsync(mappedQuery);

        // Assert
        result.Should().NotBeNull();
        result.Type.Should().Be(QueryType.Simple);
        result.ConstraintGroups.Should().HaveCount(1);
        result.ConstraintGroups[0].Constraints.Should().HaveCount(1);
        result.GroupOperator.Should().Be(LogicalOperator.And);
        result.HasConflicts.Should().BeFalse();
        result.ODataFilter.Should().NotBeNullOrEmpty();
        result.ODataFilter.Should().Contain("make eq 'BMW'");
    }

    [Fact]
    public async Task ComposeQueryAsync_MultipleANDConstraints_ReturnsFilteredQuery()
    {
        // Arrange
        var mappedQuery = new MappedQuery
        {
            Constraints = new List<SearchConstraint>
            {
                new SearchConstraint
                {
                    FieldName = "make",
                    Operator = ConstraintOperator.Equals,
                    Value = "BMW",
                    Type = ConstraintType.Exact
                },
                new SearchConstraint
                {
                    FieldName = "price",
                    Operator = ConstraintOperator.LessThanOrEqual,
                    Value = 20000,
                    Type = ConstraintType.Range
                }
            }
        };

        // Act
        var result = await _service.ComposeQueryAsync(mappedQuery);

        // Assert
        result.Should().NotBeNull();
        result.Type.Should().Be(QueryType.Filtered);
        result.ConstraintGroups.Should().NotBeEmpty();
        result.GroupOperator.Should().Be(LogicalOperator.And);
        result.HasConflicts.Should().BeFalse();
        result.ODataFilter.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ComposeQueryAsync_ORKeyword_UsesOrOperator()
    {
        // Arrange
        var mappedQuery = new MappedQuery
        {
            Constraints = new List<SearchConstraint>
            {
                new SearchConstraint
                {
                    FieldName = "make",
                    Operator = ConstraintOperator.Equals,
                    Value = "BMW",
                    Type = ConstraintType.Exact
                },
                new SearchConstraint
                {
                    FieldName = "make",
                    Operator = ConstraintOperator.Equals,
                    Value = "Audi",
                    Type = ConstraintType.Exact
                }
            },
            UnmappableTerms = new List<string> { "or" }
        };

        // Act
        var result = await _service.ComposeQueryAsync(mappedQuery);

        // Assert
        result.Should().NotBeNull();
        result.GroupOperator.Should().Be(LogicalOperator.Or);
        result.ConstraintGroups.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ComposeQueryAsync_ORInMetadata_UsesOrOperator()
    {
        // Arrange
        var mappedQuery = new MappedQuery
        {
            Constraints = new List<SearchConstraint>
            {
                new SearchConstraint
                {
                    FieldName = "make",
                    Operator = ConstraintOperator.Equals,
                    Value = "BMW",
                    Type = ConstraintType.Exact
                },
                new SearchConstraint
                {
                    FieldName = "make",
                    Operator = ConstraintOperator.Equals,
                    Value = "Audi",
                    Type = ConstraintType.Exact
                }
            },
            Metadata = new Dictionary<string, object>
            {
                { "hasOrOperator", true }
            }
        };

        // Act
        var result = await _service.ComposeQueryAsync(mappedQuery);

        // Assert
        result.Should().NotBeNull();
        result.GroupOperator.Should().Be(LogicalOperator.Or);
    }

    [Fact]
    public async Task ComposeQueryAsync_ComplexWithSemanticTerms_CreatesMultiModalQuery()
    {
        // Arrange
        var mappedQuery = new MappedQuery
        {
            Constraints = new List<SearchConstraint>
            {
                new SearchConstraint
                {
                    FieldName = "make",
                    Operator = ConstraintOperator.Equals,
                    Value = "BMW",
                    Type = ConstraintType.Exact
                },
                new SearchConstraint
                {
                    FieldName = "price",
                    Operator = ConstraintOperator.LessThanOrEqual,
                    Value = 20000,
                    Type = ConstraintType.Range
                },
                new SearchConstraint
                {
                    FieldName = "description",
                    Operator = ConstraintOperator.Contains,
                    Value = "reliable",
                    Type = ConstraintType.Semantic
                }
            }
        };

        // Act
        var result = await _service.ComposeQueryAsync(mappedQuery);

        // Assert
        result.Should().NotBeNull();
        result.Type.Should().Be(QueryType.MultiModal);
        result.ConstraintGroups.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ValidateQueryAsync_ContradictoryPrice_ReturnsFalse()
    {
        // Arrange
        var query = new ComposedQuery
        {
            Type = QueryType.Filtered,
            GroupOperator = LogicalOperator.And,
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
                            Operator = ConstraintOperator.Equals,
                            Value = 10000,
                            Type = ConstraintType.Exact
                        },
                        new SearchConstraint
                        {
                            FieldName = "price",
                            Operator = ConstraintOperator.Equals,
                            Value = 20000,
                            Type = ConstraintType.Exact
                        }
                    }
                }
            }
        };

        // Act
        var result = await _service.ValidateQueryAsync(query);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateQueryAsync_ValidQuery_ReturnsTrue()
    {
        // Arrange
        var query = new ComposedQuery
        {
            Type = QueryType.Filtered,
            GroupOperator = LogicalOperator.And,
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
                            FieldName = "make",
                            Operator = ConstraintOperator.Equals,
                            Value = "BMW",
                            Type = ConstraintType.Exact
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
        var result = await _service.ValidateQueryAsync(query);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ResolveConflictsAsync_OverlappingRanges_MergesCorrectly()
    {
        // Arrange
        var query = new ComposedQuery
        {
            Type = QueryType.Filtered,
            GroupOperator = LogicalOperator.And,
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
                        },
                        new SearchConstraint
                        {
                            FieldName = "price",
                            Operator = ConstraintOperator.LessThanOrEqual,
                            Value = 30000,
                            Type = ConstraintType.Range
                        }
                    }
                }
            }
        };

        // Act
        var result = await _service.ResolveConflictsAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.Warnings.Should().Contain(w => w.Contains("Merged overlapping ranges"));
        result.ConstraintGroups.Should().NotBeEmpty();
        result.ConstraintGroups[0].Constraints.Should().HaveCountLessThan(query.ConstraintGroups[0].Constraints.Count);
    }

    [Theory]
    [InlineData(ConstraintOperator.Equals, "BMW", "make eq 'BMW'")]
    [InlineData(ConstraintOperator.GreaterThanOrEqual, 20000, "price ge 20000")]
    [InlineData(ConstraintOperator.LessThanOrEqual, 50000, "mileage le 50000")]
    public async Task ToODataFilter_VariousConstraints_ReturnsValidOData(
        ConstraintOperator op, object value, string expectedFragment)
    {
        // Arrange
        var mappedQuery = new MappedQuery
        {
            Constraints = new List<SearchConstraint>
            {
                new SearchConstraint
                {
                    FieldName = op == ConstraintOperator.Equals ? "make" : 
                                 op == ConstraintOperator.GreaterThanOrEqual ? "price" : "mileage",
                    Operator = op,
                    Value = value,
                    Type = op == ConstraintOperator.Equals ? ConstraintType.Exact : ConstraintType.Range
                }
            }
        };

        // Act
        var result = await _service.ComposeQueryAsync(mappedQuery);

        // Assert
        result.Should().NotBeNull();
        result.ODataFilter.Should().NotBeNullOrEmpty();
        result.ODataFilter.Should().Contain(expectedFragment);
    }

    [Fact]
    public async Task ComposeQueryAsync_NullMappedQuery_ThrowsArgumentNullException()
    {
        // Act
        Func<Task> act = async () => await _service.ComposeQueryAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ComposeQueryAsync_EmptyConstraints_ReturnsWarning()
    {
        // Arrange
        var mappedQuery = new MappedQuery
        {
            Constraints = new List<SearchConstraint>()
        };

        // Act
        var result = await _service.ComposeQueryAsync(mappedQuery);

        // Assert
        result.Should().NotBeNull();
        result.Warnings.Should().Contain("No constraints provided");
    }

    [Fact]
    public async Task ComposeQueryAsync_BetweenOperator_CreatesCorrectOData()
    {
        // Arrange
        var mappedQuery = new MappedQuery
        {
            Constraints = new List<SearchConstraint>
            {
                new SearchConstraint
                {
                    FieldName = "price",
                    Operator = ConstraintOperator.Between,
                    Value = new object[] { 15000, 25000 },
                    Type = ConstraintType.Range
                }
            }
        };

        // Act
        var result = await _service.ComposeQueryAsync(mappedQuery);

        // Assert
        result.Should().NotBeNull();
        result.ODataFilter.Should().Contain("price ge 15000");
        result.ODataFilter.Should().Contain("price le 25000");
    }

    [Fact]
    public async Task ComposeQueryAsync_InOperator_CreatesCorrectOData()
    {
        // Arrange
        var mappedQuery = new MappedQuery
        {
            Constraints = new List<SearchConstraint>
            {
                new SearchConstraint
                {
                    FieldName = "fuelType",
                    Operator = ConstraintOperator.In,
                    Value = new object[] { "Electric", "Hybrid" },
                    Type = ConstraintType.Exact
                }
            }
        };

        // Act
        var result = await _service.ComposeQueryAsync(mappedQuery);

        // Assert
        result.Should().NotBeNull();
        result.ODataFilter.Should().Contain("search.in(fuelType");
    }
}
