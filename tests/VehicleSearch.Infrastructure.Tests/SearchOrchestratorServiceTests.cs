using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using VehicleSearch.Core.Models;
using VehicleSearch.Infrastructure.Search;

namespace VehicleSearch.Infrastructure.Tests;

public class SearchOrchestratorServiceTests
{
    private readonly Mock<ExactSearchExecutor> _exactExecutorMock;
    private readonly Mock<SemanticSearchExecutor> _semanticExecutorMock;
    private readonly Mock<HybridSearchExecutor> _hybridExecutorMock;
    private readonly Mock<ILogger<SearchOrchestratorService>> _loggerMock;
    private readonly SearchOrchestratorService _service;

    public SearchOrchestratorServiceTests()
    {
        // Create a real AzureSearchClient for the executors to use in mocks
        var searchConfig = new AzureSearchConfig
        {
            Endpoint = "https://test-search.search.windows.net",
            ApiKey = "test-api-key",
            IndexName = "vehicles-index",
            VectorDimensions = 1536
        };
        var searchClientLogger = Mock.Of<ILogger<AzureSearchClient>>();
        var azureSearchClient = new AzureSearchClient(Options.Create(searchConfig), searchClientLogger);

        // Create mocks for the executors
        _exactExecutorMock = new Mock<ExactSearchExecutor>(
            azureSearchClient,
            Mock.Of<ILogger<ExactSearchExecutor>>());
        
        _semanticExecutorMock = new Mock<SemanticSearchExecutor>(
            Mock.Of<VehicleSearch.Core.Interfaces.IEmbeddingService>(),
            azureSearchClient,
            Mock.Of<ILogger<SemanticSearchExecutor>>());
        
        _hybridExecutorMock = new Mock<HybridSearchExecutor>(
            Mock.Of<VehicleSearch.Core.Interfaces.IEmbeddingService>(),
            azureSearchClient,
            Mock.Of<ILogger<HybridSearchExecutor>>());
        
        _loggerMock = new Mock<ILogger<SearchOrchestratorService>>();

        _service = new SearchOrchestratorService(
            _exactExecutorMock.Object,
            _semanticExecutorMock.Object,
            _hybridExecutorMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public void Constructor_WithNullExactExecutor_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => new SearchOrchestratorService(
            null!,
            _semanticExecutorMock.Object,
            _hybridExecutorMock.Object,
            _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithNullSemanticExecutor_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => new SearchOrchestratorService(
            _exactExecutorMock.Object,
            null!,
            _hybridExecutorMock.Object,
            _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithNullHybridExecutor_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => new SearchOrchestratorService(
            _exactExecutorMock.Object,
            _semanticExecutorMock.Object,
            null!,
            _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => new SearchOrchestratorService(
            _exactExecutorMock.Object,
            _semanticExecutorMock.Object,
            _hybridExecutorMock.Object,
            null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task DetermineStrategyAsync_WithNullQuery_ThrowsArgumentNullException()
    {
        // Act
        Func<Task> act = async () => await _service.DetermineStrategyAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task DetermineStrategyAsync_WithExactConstraintsOnly_ReturnsExactOnlyStrategy()
    {
        // Arrange
        var query = new ComposedQuery
        {
            Type = QueryType.Filtered,
            ConstraintGroups = new List<ConstraintGroup>
            {
                new ConstraintGroup
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
                }
            }
        };

        // Act
        var result = await _service.DetermineStrategyAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.Type.Should().Be(StrategyType.ExactOnly);
        result.Approaches.Should().ContainSingle();
        result.Approaches.Should().Contain(SearchApproach.ExactMatch);
        result.Weights[SearchApproach.ExactMatch].Should().Be(1.0);
        result.ShouldRerank.Should().BeFalse();
    }

    [Fact]
    public async Task DetermineStrategyAsync_WithSemanticConstraintsOnly_ReturnsSemanticOnlyStrategy()
    {
        // Arrange
        var query = new ComposedQuery
        {
            Type = QueryType.Complex,
            ConstraintGroups = new List<ConstraintGroup>
            {
                new ConstraintGroup
                {
                    Constraints = new List<SearchConstraint>
                    {
                        new SearchConstraint
                        {
                            FieldName = "description",
                            Operator = ConstraintOperator.Contains,
                            Value = "reliable",
                            Type = ConstraintType.Semantic
                        },
                        new SearchConstraint
                        {
                            FieldName = "description",
                            Operator = ConstraintOperator.Contains,
                            Value = "economical",
                            Type = ConstraintType.Semantic
                        }
                    }
                }
            }
        };

        // Act
        var result = await _service.DetermineStrategyAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.Type.Should().Be(StrategyType.SemanticOnly);
        result.Approaches.Should().ContainSingle();
        result.Approaches.Should().Contain(SearchApproach.SemanticSearch);
        result.Weights[SearchApproach.SemanticSearch].Should().Be(1.0);
        result.ShouldRerank.Should().BeFalse();
    }

    [Fact]
    public async Task DetermineStrategyAsync_WithMixedConstraints_ReturnsHybridStrategy()
    {
        // Arrange
        var query = new ComposedQuery
        {
            Type = QueryType.Complex,
            ConstraintGroups = new List<ConstraintGroup>
            {
                new ConstraintGroup
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
                }
            }
        };

        // Act
        var result = await _service.DetermineStrategyAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.Type.Should().Be(StrategyType.Hybrid);
        result.Approaches.Should().HaveCount(2);
        result.Approaches.Should().Contain(SearchApproach.ExactMatch);
        result.Approaches.Should().Contain(SearchApproach.SemanticSearch);
        result.Weights.Should().ContainKey(SearchApproach.ExactMatch);
        result.Weights.Should().ContainKey(SearchApproach.SemanticSearch);
        result.Weights[SearchApproach.ExactMatch].Should().BeGreaterThan(0);
        result.Weights[SearchApproach.SemanticSearch].Should().BeGreaterThan(0);
        result.ShouldRerank.Should().BeTrue();
    }

    [Fact]
    public async Task DetermineStrategyAsync_WithNoConstraints_ReturnsSemanticOnlyStrategyAsFallback()
    {
        // Arrange
        var query = new ComposedQuery
        {
            Type = QueryType.Simple,
            ConstraintGroups = new List<ConstraintGroup>()
        };

        // Act
        var result = await _service.DetermineStrategyAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.Type.Should().Be(StrategyType.SemanticOnly);
        result.Approaches.Should().ContainSingle();
        result.Approaches.Should().Contain(SearchApproach.SemanticSearch);
    }

    [Fact]
    public async Task ExecuteSearchAsync_WithNullQuery_ThrowsArgumentNullException()
    {
        // Arrange
        var strategy = new SearchStrategy { Type = StrategyType.ExactOnly };

        // Act
        Func<Task> act = async () => await _service.ExecuteSearchAsync(null!, strategy);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ExecuteSearchAsync_WithNullStrategy_ThrowsArgumentNullException()
    {
        // Arrange
        var query = new ComposedQuery();

        // Act
        Func<Task> act = async () => await _service.ExecuteSearchAsync(query, null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ExecuteSearchAsync_WithInvalidMaxResults_ThrowsArgumentException()
    {
        // Arrange
        var query = new ComposedQuery();
        var strategy = new SearchStrategy { Type = StrategyType.ExactOnly };

        // Act
        Func<Task> act = async () => await _service.ExecuteSearchAsync(query, strategy, maxResults: 0);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*MaxResults must be between 1 and 100*");
    }

    [Fact]
    public async Task ExecuteSearchAsync_WithMaxResultsOver100_ThrowsArgumentException()
    {
        // Arrange
        var query = new ComposedQuery();
        var strategy = new SearchStrategy { Type = StrategyType.ExactOnly };

        // Act
        Func<Task> act = async () => await _service.ExecuteSearchAsync(query, strategy, maxResults: 101);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*MaxResults must be between 1 and 100*");
    }

    [Fact]
    public async Task ExecuteHybridSearchAsync_WithNullQuery_ThrowsArgumentNullException()
    {
        // Act
        Func<Task> act = async () => await _service.ExecuteHybridSearchAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task DetermineStrategyAsync_WithMultipleExactConstraints_CalculatesCorrectWeights()
    {
        // Arrange
        var query = new ComposedQuery
        {
            Type = QueryType.Complex,
            ConstraintGroups = new List<ConstraintGroup>
            {
                new ConstraintGroup
                {
                    Constraints = new List<SearchConstraint>
                    {
                        new SearchConstraint { FieldName = "make", Type = ConstraintType.Exact, Value = "BMW" },
                        new SearchConstraint { FieldName = "model", Type = ConstraintType.Exact, Value = "320d" },
                        new SearchConstraint { FieldName = "price", Type = ConstraintType.Range, Value = 20000 },
                        new SearchConstraint { FieldName = "mileage", Type = ConstraintType.Range, Value = 50000 },
                        new SearchConstraint { FieldName = "description", Type = ConstraintType.Semantic, Value = "reliable" }
                    }
                }
            }
        };

        // Act
        var result = await _service.DetermineStrategyAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.Type.Should().Be(StrategyType.Hybrid);
        
        // With 4 exact constraints, exactWeight = min(0.7, 4 * 0.15) = 0.6
        result.Weights[SearchApproach.ExactMatch].Should().Be(0.6);
        result.Weights[SearchApproach.SemanticSearch].Should().Be(0.4);
    }

    [Fact]
    public async Task DetermineStrategyAsync_WithManyExactConstraints_CapsWeightAt70Percent()
    {
        // Arrange
        var query = new ComposedQuery
        {
            Type = QueryType.Complex,
            ConstraintGroups = new List<ConstraintGroup>
            {
                new ConstraintGroup
                {
                    Constraints = new List<SearchConstraint>
                    {
                        new SearchConstraint { FieldName = "make", Type = ConstraintType.Exact, Value = "BMW" },
                        new SearchConstraint { FieldName = "model", Type = ConstraintType.Exact, Value = "320d" },
                        new SearchConstraint { FieldName = "price", Type = ConstraintType.Range, Value = 20000 },
                        new SearchConstraint { FieldName = "mileage", Type = ConstraintType.Range, Value = 50000 },
                        new SearchConstraint { FieldName = "colour", Type = ConstraintType.Exact, Value = "Blue" },
                        new SearchConstraint { FieldName = "fuelType", Type = ConstraintType.Exact, Value = "Diesel" },
                        new SearchConstraint { FieldName = "description", Type = ConstraintType.Semantic, Value = "reliable" }
                    }
                }
            }
        };

        // Act
        var result = await _service.DetermineStrategyAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.Type.Should().Be(StrategyType.Hybrid);
        
        // With 6 exact constraints, exactWeight would be 6 * 0.15 = 0.9, but should cap at 0.7
        result.Weights[SearchApproach.ExactMatch].Should().Be(0.7);
        result.Weights[SearchApproach.SemanticSearch].Should().BeApproximately(0.3, 0.0001);
    }
}
