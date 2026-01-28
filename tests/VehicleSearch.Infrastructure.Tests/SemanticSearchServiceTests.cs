using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using VehicleSearch.Core.Interfaces;
using VehicleSearch.Core.Models;
using VehicleSearch.Infrastructure.Search;

namespace VehicleSearch.Infrastructure.Tests;

public class SemanticSearchServiceTests
{
    private readonly Mock<IEmbeddingService> _embeddingServiceMock;
    private readonly Mock<ILogger<SemanticSearchService>> _loggerMock;
    private readonly Mock<ILogger<AzureSearchClient>> _searchClientLoggerMock;
    private readonly AzureSearchConfig _searchConfig;

    public SemanticSearchServiceTests()
    {
        _embeddingServiceMock = new Mock<IEmbeddingService>();
        _loggerMock = new Mock<ILogger<SemanticSearchService>>();
        _searchClientLoggerMock = new Mock<ILogger<AzureSearchClient>>();
        _searchConfig = new AzureSearchConfig
        {
            Endpoint = "https://test-search.search.windows.net",
            ApiKey = "test-api-key",
            IndexName = "vehicles-index",
            VectorDimensions = 1536
        };
    }

    [Fact]
    public void Constructor_WithNullEmbeddingService_ThrowsArgumentNullException()
    {
        // Arrange
        var configOptions = Options.Create(_searchConfig);
        var searchClient = new AzureSearchClient(configOptions, _searchClientLoggerMock.Object);

        // Act
        Action act = () => new SemanticSearchService(null!, searchClient, _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithNullSearchClient_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => new SemanticSearchService(_embeddingServiceMock.Object, null!, _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var configOptions = Options.Create(_searchConfig);
        var searchClient = new AzureSearchClient(configOptions, _searchClientLoggerMock.Object);

        // Act
        Action act = () => new SemanticSearchService(_embeddingServiceMock.Object, searchClient, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task SearchAsync_WithNullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var configOptions = Options.Create(_searchConfig);
        var searchClient = new AzureSearchClient(configOptions, _searchClientLoggerMock.Object);
        var service = new SemanticSearchService(_embeddingServiceMock.Object, searchClient, _loggerMock.Object);

        // Act
        Func<Task> act = async () => await service.SearchAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task SearchAsync_WithEmptyQuery_ThrowsArgumentException()
    {
        // Arrange
        var configOptions = Options.Create(_searchConfig);
        var searchClient = new AzureSearchClient(configOptions, _searchClientLoggerMock.Object);
        var service = new SemanticSearchService(_embeddingServiceMock.Object, searchClient, _loggerMock.Object);
        var request = new SemanticSearchRequest { Query = "" };

        // Act
        Func<Task> act = async () => await service.SearchAsync(request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Query cannot be empty*");
    }

    [Fact]
    public async Task SearchByEmbeddingAsync_WithNullEmbedding_ThrowsArgumentException()
    {
        // Arrange
        var configOptions = Options.Create(_searchConfig);
        var searchClient = new AzureSearchClient(configOptions, _searchClientLoggerMock.Object);
        var service = new SemanticSearchService(_embeddingServiceMock.Object, searchClient, _loggerMock.Object);

        // Act
        Func<Task> act = async () => await service.SearchByEmbeddingAsync(null!, 10);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Embedding cannot be null or empty*");
    }

    [Fact]
    public async Task SearchByEmbeddingAsync_WithEmptyEmbedding_ThrowsArgumentException()
    {
        // Arrange
        var configOptions = Options.Create(_searchConfig);
        var searchClient = new AzureSearchClient(configOptions, _searchClientLoggerMock.Object);
        var service = new SemanticSearchService(_embeddingServiceMock.Object, searchClient, _loggerMock.Object);

        // Act
        Func<Task> act = async () => await service.SearchByEmbeddingAsync(Array.Empty<float>(), 10);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Embedding cannot be null or empty*");
    }

    [Fact]
    public async Task SearchByEmbeddingAsync_WithZeroMaxResults_ThrowsArgumentException()
    {
        // Arrange
        var configOptions = Options.Create(_searchConfig);
        var searchClient = new AzureSearchClient(configOptions, _searchClientLoggerMock.Object);
        var service = new SemanticSearchService(_embeddingServiceMock.Object, searchClient, _loggerMock.Object);
        var embedding = new float[] { 0.1f, 0.2f };

        // Act
        Func<Task> act = async () => await service.SearchByEmbeddingAsync(embedding, 0);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*MaxResults must be greater than 0*");
    }

    [Fact]
    public async Task SearchByEmbeddingAsync_WithNegativeMaxResults_ThrowsArgumentException()
    {
        // Arrange
        var configOptions = Options.Create(_searchConfig);
        var searchClient = new AzureSearchClient(configOptions, _searchClientLoggerMock.Object);
        var service = new SemanticSearchService(_embeddingServiceMock.Object, searchClient, _loggerMock.Object);
        var embedding = new float[] { 0.1f, 0.2f };

        // Act
        Func<Task> act = async () => await service.SearchByEmbeddingAsync(embedding, -1);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*MaxResults must be greater than 0*");
    }

    // Note: More comprehensive integration tests would require mocking Azure Search responses
    // or using actual Azure Search test instances. These basic tests verify parameter validation
    // and constructor behavior. Full semantic search behavior is tested in integration tests.
}
