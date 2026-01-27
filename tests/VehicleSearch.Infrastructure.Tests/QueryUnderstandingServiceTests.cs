using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using VehicleSearch.Core.Interfaces;
using VehicleSearch.Core.Models;
using VehicleSearch.Infrastructure.AI;

namespace VehicleSearch.Infrastructure.Tests;

public class QueryUnderstandingServiceTests
{
    private readonly Mock<IIntentClassifier> _intentClassifierMock;
    private readonly Mock<IEntityExtractor> _entityExtractorMock;
    private readonly Mock<ILogger<QueryUnderstandingService>> _loggerMock;
    private readonly QueryUnderstandingService _service;

    public QueryUnderstandingServiceTests()
    {
        _intentClassifierMock = new Mock<IIntentClassifier>();
        _entityExtractorMock = new Mock<IEntityExtractor>();
        _loggerMock = new Mock<ILogger<QueryUnderstandingService>>();

        _service = new QueryUnderstandingService(
            _intentClassifierMock.Object,
            _entityExtractorMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public void Constructor_WithNullIntentClassifier_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => new QueryUnderstandingService(
            null!,
            _entityExtractorMock.Object,
            _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithNullEntityExtractor_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => new QueryUnderstandingService(
            _intentClassifierMock.Object,
            null!,
            _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => new QueryUnderstandingService(
            _intentClassifierMock.Object,
            _entityExtractorMock.Object,
            null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task ParseQueryAsync_WithNullQuery_ThrowsArgumentException()
    {
        // Act
        Func<Task> act = async () => await _service.ParseQueryAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Query cannot be null or empty*");
    }

    [Fact]
    public async Task ParseQueryAsync_WithEmptyQuery_ThrowsArgumentException()
    {
        // Act
        Func<Task> act = async () => await _service.ParseQueryAsync(string.Empty);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Query cannot be null or empty*");
    }

    [Fact]
    public async Task ParseQueryAsync_ValidQuery_ReturnsCompleteResult()
    {
        // Arrange
        var query = "Show me BMW under £20,000";
        var expectedIntent = QueryIntent.Search;
        var expectedConfidence = 0.95;
        var expectedEntities = new List<ExtractedEntity>
        {
            new ExtractedEntity
            {
                Type = EntityType.Make,
                Value = "BMW",
                Confidence = 1.0,
                StartPosition = 8,
                EndPosition = 11
            },
            new ExtractedEntity
            {
                Type = EntityType.Price,
                Value = "20000",
                Confidence = 0.95,
                StartPosition = 18,
                EndPosition = 25
            }
        };

        _intentClassifierMock
            .Setup(x => x.ClassifyAsync(query, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedIntent, expectedConfidence));

        _entityExtractorMock
            .Setup(x => x.ExtractAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedEntities);

        // Act
        var result = await _service.ParseQueryAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.OriginalQuery.Should().Be(query);
        result.Intent.Should().Be(expectedIntent);
        result.ConfidenceScore.Should().Be(expectedConfidence);
        result.Entities.Should().HaveCount(2);
        result.Entities.Should().Contain(e => e.Type == EntityType.Make && e.Value == "BMW");
        result.Entities.Should().Contain(e => e.Type == EntityType.Price && e.Value == "20000");
    }

    [Fact]
    public async Task ParseQueryAsync_WithContext_PassesContextToClassifier()
    {
        // Arrange
        var query = "Show me cheaper ones";
        var context = new ConversationContext
        {
            SessionId = "test",
            History = new List<string> { "Show me BMW cars" }
        };

        _intentClassifierMock
            .Setup(x => x.ClassifyAsync(query, context, It.IsAny<CancellationToken>()))
            .ReturnsAsync((QueryIntent.Refine, 0.9));

        _entityExtractorMock
            .Setup(x => x.ExtractAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ExtractedEntity>());

        // Act
        var result = await _service.ParseQueryAsync(query, context);

        // Assert
        result.Intent.Should().Be(QueryIntent.Refine);
        _intentClassifierMock.Verify(
            x => x.ClassifyAsync(query, context, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ClassifyIntentAsync_ValidQuery_ReturnsIntent()
    {
        // Arrange
        var query = "Show me BMW cars";
        var expectedIntent = QueryIntent.Search;

        _intentClassifierMock
            .Setup(x => x.ClassifyAsync(query, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((expectedIntent, 0.95));

        // Act
        var intent = await _service.ClassifyIntentAsync(query);

        // Assert
        intent.Should().Be(expectedIntent);
    }

    [Fact]
    public async Task ClassifyIntentAsync_WithNullQuery_ThrowsArgumentException()
    {
        // Act
        Func<Task> act = async () => await _service.ClassifyIntentAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Query cannot be null or empty*");
    }

    [Fact]
    public async Task ExtractEntitiesAsync_ValidQuery_ReturnsEntities()
    {
        // Arrange
        var query = "Show me BMW under £20k";
        var expectedEntities = new List<ExtractedEntity>
        {
            new ExtractedEntity
            {
                Type = EntityType.Make,
                Value = "BMW",
                Confidence = 1.0
            },
            new ExtractedEntity
            {
                Type = EntityType.Price,
                Value = "20000",
                Confidence = 0.95
            }
        };

        _entityExtractorMock
            .Setup(x => x.ExtractAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedEntities);

        // Act
        var entities = await _service.ExtractEntitiesAsync(query);

        // Assert
        entities.Should().HaveCount(2);
        entities.Should().Contain(e => e.Type == EntityType.Make && e.Value == "BMW");
        entities.Should().Contain(e => e.Type == EntityType.Price && e.Value == "20000");
    }

    [Fact]
    public async Task ExtractEntitiesAsync_WithNullQuery_ThrowsArgumentException()
    {
        // Act
        Func<Task> act = async () => await _service.ExtractEntitiesAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Query cannot be null or empty*");
    }

    [Fact]
    public async Task ParseQueryAsync_CallsBothServicesInParallel()
    {
        // Arrange
        var query = "Show me BMW cars";
        
        _intentClassifierMock
            .Setup(x => x.ClassifyAsync(query, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((QueryIntent.Search, 0.95));

        _entityExtractorMock
            .Setup(x => x.ExtractAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ExtractedEntity>());

        // Act
        await _service.ParseQueryAsync(query);

        // Assert
        _intentClassifierMock.Verify(
            x => x.ClassifyAsync(query, null, It.IsAny<CancellationToken>()),
            Times.Once);
        
        _entityExtractorMock.Verify(
            x => x.ExtractAsync(query, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ParseQueryAsync_IdentifiesUnmappedTerms()
    {
        // Arrange
        var query = "Show me BMW cars with unknown feature";
        var entities = new List<ExtractedEntity>
        {
            new ExtractedEntity
            {
                Type = EntityType.Make,
                Value = "BMW",
                Confidence = 1.0,
                StartPosition = 8,
                EndPosition = 11
            }
        };

        _intentClassifierMock
            .Setup(x => x.ClassifyAsync(query, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((QueryIntent.Search, 0.95));

        _entityExtractorMock
            .Setup(x => x.ExtractAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entities);

        // Act
        var result = await _service.ParseQueryAsync(query);

        // Assert
        result.UnmappedTerms.Should().NotBeEmpty();
        result.UnmappedTerms.Should().Contain(t => 
            t.Equals("unknown", StringComparison.OrdinalIgnoreCase) || 
            t.Equals("feature", StringComparison.OrdinalIgnoreCase));
    }
}
