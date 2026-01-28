using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using VehicleSearch.Core.Entities;
using VehicleSearch.Core.Interfaces;
using VehicleSearch.Core.Models;
using VehicleSearch.Infrastructure.AI;

namespace VehicleSearch.Infrastructure.Tests;

public class CachedEmbeddingServiceTests
{
    private readonly Mock<IEmbeddingService> _innerServiceMock;
    private readonly IMemoryCache _cache;
    private readonly Mock<ILogger<CachedEmbeddingService>> _loggerMock;
    private readonly CachedEmbeddingService _service;

    public CachedEmbeddingServiceTests()
    {
        _innerServiceMock = new Mock<IEmbeddingService>();
        _cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 1000 });
        _loggerMock = new Mock<ILogger<CachedEmbeddingService>>();
        _service = new CachedEmbeddingService(_innerServiceMock.Object, _cache, _loggerMock.Object);
    }

    [Fact]
    public void Constructor_WithNullInnerService_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => new CachedEmbeddingService(null!, _cache, _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithNullCache_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => new CachedEmbeddingService(_innerServiceMock.Object, null!, _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => new CachedEmbeddingService(_innerServiceMock.Object, _cache, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_WithNullText_ThrowsArgumentException()
    {
        // Act
        Func<Task> act = async () => await _service.GenerateEmbeddingAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Text cannot be null or empty*");
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_WithEmptyText_ThrowsArgumentException()
    {
        // Act
        Func<Task> act = async () => await _service.GenerateEmbeddingAsync(string.Empty);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Text cannot be null or empty*");
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_FirstCall_CallsInnerService()
    {
        // Arrange
        var text = "test query";
        var expectedEmbedding = new float[] { 0.1f, 0.2f, 0.3f };
        _innerServiceMock
            .Setup(s => s.GenerateEmbeddingAsync(text, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedEmbedding);

        // Act
        var result = await _service.GenerateEmbeddingAsync(text);

        // Assert
        result.Should().BeEquivalentTo(expectedEmbedding);
        _innerServiceMock.Verify(s => s.GenerateEmbeddingAsync(text, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_SecondCallWithSameText_ReturnsCachedValue()
    {
        // Arrange
        var text = "test query";
        var expectedEmbedding = new float[] { 0.1f, 0.2f, 0.3f };
        _innerServiceMock
            .Setup(s => s.GenerateEmbeddingAsync(text, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedEmbedding);

        // Act
        var result1 = await _service.GenerateEmbeddingAsync(text);
        var result2 = await _service.GenerateEmbeddingAsync(text);

        // Assert
        result1.Should().BeEquivalentTo(expectedEmbedding);
        result2.Should().BeEquivalentTo(expectedEmbedding);
        _innerServiceMock.Verify(s => s.GenerateEmbeddingAsync(text, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_CaseInsensitive_ReturnsCachedValue()
    {
        // Arrange
        var text1 = "Test Query";
        var text2 = "test query";
        var expectedEmbedding = new float[] { 0.1f, 0.2f, 0.3f };
        _innerServiceMock
            .Setup(s => s.GenerateEmbeddingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedEmbedding);

        // Act
        var result1 = await _service.GenerateEmbeddingAsync(text1);
        var result2 = await _service.GenerateEmbeddingAsync(text2);

        // Assert
        result1.Should().BeEquivalentTo(expectedEmbedding);
        result2.Should().BeEquivalentTo(expectedEmbedding);
        _innerServiceMock.Verify(s => s.GenerateEmbeddingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_WithWhitespace_TrimsAndCaches()
    {
        // Arrange
        var text1 = "  test query  ";
        var text2 = "test query";
        var expectedEmbedding = new float[] { 0.1f, 0.2f, 0.3f };
        _innerServiceMock
            .Setup(s => s.GenerateEmbeddingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedEmbedding);

        // Act
        var result1 = await _service.GenerateEmbeddingAsync(text1);
        var result2 = await _service.GenerateEmbeddingAsync(text2);

        // Assert
        result1.Should().BeEquivalentTo(expectedEmbedding);
        result2.Should().BeEquivalentTo(expectedEmbedding);
        _innerServiceMock.Verify(s => s.GenerateEmbeddingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GenerateBatchEmbeddingsAsync_DelegatesToInnerService()
    {
        // Arrange
        var vehicles = new List<Vehicle>
        {
            new Vehicle { Id = "V001", Description = "Test vehicle 1" },
            new Vehicle { Id = "V002", Description = "Test vehicle 2" }
        };
        var expectedEmbeddings = new List<VehicleEmbedding>
        {
            new VehicleEmbedding { VehicleId = "V001", Vector = new float[] { 0.1f }, Dimensions = 1 },
            new VehicleEmbedding { VehicleId = "V002", Vector = new float[] { 0.2f }, Dimensions = 1 }
        };
        _innerServiceMock
            .Setup(s => s.GenerateBatchEmbeddingsAsync(vehicles, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedEmbeddings);

        // Act
        var result = await _service.GenerateBatchEmbeddingsAsync(vehicles);

        // Assert
        result.Should().BeEquivalentTo(expectedEmbeddings);
        _innerServiceMock.Verify(s => s.GenerateBatchEmbeddingsAsync(vehicles, 100, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void PrepareQueryForEmbedding_WithEconomicalTerm_AddsEnrichment()
    {
        // Arrange
        var query = "economical car";

        // Act
        var result = CachedEmbeddingService.PrepareQueryForEmbedding(query);

        // Assert
        result.Should().Contain("economical vehicles");
        result.Should().Contain("fuel-efficient cars");
    }

    [Fact]
    public void PrepareQueryForEmbedding_WithReliableTerm_AddsEnrichment()
    {
        // Arrange
        var query = "reliable vehicle";

        // Act
        var result = CachedEmbeddingService.PrepareQueryForEmbedding(query);

        // Assert
        result.Should().Contain("reliable vehicles");
        result.Should().Contain("well-maintained cars");
    }

    [Fact]
    public void PrepareQueryForEmbedding_WithFamilyTerm_AddsEnrichment()
    {
        // Arrange
        var query = "family car";

        // Act
        var result = CachedEmbeddingService.PrepareQueryForEmbedding(query);

        // Assert
        result.Should().Contain("family vehicle");
        result.Should().Contain("spacious practical");
    }

    [Fact]
    public void PrepareQueryForEmbedding_WithSportyTerm_AddsEnrichment()
    {
        // Arrange
        var query = "sporty car";

        // Act
        var result = CachedEmbeddingService.PrepareQueryForEmbedding(query);

        // Assert
        result.Should().Contain("sporty performance vehicles");
    }

    [Fact]
    public void PrepareQueryForEmbedding_WithNoQualitativeTerms_ReturnsOriginalQuery()
    {
        // Arrange
        var query = "BMW 3 Series";

        // Act
        var result = CachedEmbeddingService.PrepareQueryForEmbedding(query);

        // Assert
        result.Should().Be(query);
    }

    [Fact]
    public void PrepareQueryForEmbedding_WithEmptyQuery_ReturnsEmpty()
    {
        // Arrange
        var query = "";

        // Act
        var result = CachedEmbeddingService.PrepareQueryForEmbedding(query);

        // Assert
        result.Should().Be(query);
    }
}
