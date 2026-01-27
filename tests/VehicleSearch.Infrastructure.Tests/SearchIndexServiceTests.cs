using Azure;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using VehicleSearch.Core.Interfaces;
using VehicleSearch.Core.Models;
using VehicleSearch.Infrastructure.Search;

namespace VehicleSearch.Infrastructure.Tests;

public class SearchIndexServiceTests : IDisposable
{
    private readonly Mock<ILogger<SearchIndexService>> _loggerMock;
    private readonly AzureSearchConfig _validConfig;
    private readonly IOptions<AzureSearchConfig> _configOptions;

    public SearchIndexServiceTests()
    {
        _loggerMock = new Mock<ILogger<SearchIndexService>>();
        
        // Use a valid-looking configuration for tests
        _validConfig = new AzureSearchConfig
        {
            Endpoint = "https://test-search.search.windows.net",
            ApiKey = "test-api-key-12345",
            IndexName = "test-vehicles-index",
            VectorDimensions = 1536
        };
        
        _configOptions = Options.Create(_validConfig);
    }

    public void Dispose()
    {
        // Cleanup if needed
    }

    [Fact]
    public void Constructor_WithValidConfig_InitializesSuccessfully()
    {
        // Act
        var service = new SearchIndexService(_configOptions, _loggerMock.Object);

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNullConfig_ThrowsArgumentNullException()
    {
        // Arrange
        IOptions<AzureSearchConfig>? nullConfig = null;

        // Act
        Action act = () => new SearchIndexService(nullConfig!, _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        ILogger<SearchIndexService>? nullLogger = null;

        // Act
        Action act = () => new SearchIndexService(_configOptions, nullLogger!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithEmptyEndpoint_ThrowsInvalidOperationException()
    {
        // Arrange
        var invalidConfig = new AzureSearchConfig
        {
            Endpoint = "",
            ApiKey = "test-api-key",
            IndexName = "test-index"
        };
        var options = Options.Create(invalidConfig);

        // Act
        Action act = () => new SearchIndexService(options, _loggerMock.Object);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*endpoint*");
    }

    [Fact]
    public void Constructor_WithEmptyApiKey_ThrowsInvalidOperationException()
    {
        // Arrange
        var invalidConfig = new AzureSearchConfig
        {
            Endpoint = "https://test-search.search.windows.net",
            ApiKey = "",
            IndexName = "test-index"
        };
        var options = Options.Create(invalidConfig);

        // Act
        Action act = () => new SearchIndexService(options, _loggerMock.Object);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*API key*");
    }

    [Fact]
    public async Task CreateIndexAsync_WithInvalidCredentials_ThrowsException()
    {
        // Arrange
        var service = new SearchIndexService(_configOptions, _loggerMock.Object);

        // Act & Assert
        // Note: This will fail because we're using invalid credentials
        // In a real integration test, you would use valid test credentials
        await Assert.ThrowsAnyAsync<Exception>(() => 
            service.CreateIndexAsync(CancellationToken.None));
    }

    [Fact]
    public async Task IndexExistsAsync_WithInvalidCredentials_ThrowsException()
    {
        // Arrange
        var service = new SearchIndexService(_configOptions, _loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(() => 
            service.IndexExistsAsync(CancellationToken.None));
    }

    [Fact]
    public async Task DeleteIndexAsync_WithInvalidCredentials_ThrowsException()
    {
        // Arrange
        var service = new SearchIndexService(_configOptions, _loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(() => 
            service.DeleteIndexAsync(CancellationToken.None));
    }

    [Fact]
    public async Task GetIndexStatusAsync_WithInvalidCredentials_ThrowsException()
    {
        // Arrange
        var service = new SearchIndexService(_configOptions, _loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(() => 
            service.GetIndexStatusAsync(CancellationToken.None));
    }

    [Fact]
    public async Task UpdateIndexSchemaAsync_WithInvalidCredentials_ThrowsException()
    {
        // Arrange
        var service = new SearchIndexService(_configOptions, _loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(() => 
            service.UpdateIndexSchemaAsync(CancellationToken.None));
    }

    [Fact]
    public void Service_ImplementsISearchIndexService()
    {
        // Arrange
        var service = new SearchIndexService(_configOptions, _loggerMock.Object);

        // Assert
        service.Should().BeAssignableTo<ISearchIndexService>();
    }

    [Fact]
    public void AzureSearchConfig_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var config = new AzureSearchConfig();

        // Assert
        config.IndexName.Should().Be("vehicles-index");
        config.VectorDimensions.Should().Be(1536);
        config.Endpoint.Should().BeEmpty();
        config.ApiKey.Should().BeEmpty();
    }

    [Fact]
    public void IndexStatus_CanBeCreated()
    {
        // Arrange & Act
        var status = new IndexStatus
        {
            Exists = true,
            IndexName = "test-index",
            DocumentCount = 100,
            StorageSize = "2.5 MB"
        };

        // Assert
        status.Should().NotBeNull();
        status.Exists.Should().BeTrue();
        status.IndexName.Should().Be("test-index");
        status.DocumentCount.Should().Be(100);
        status.StorageSize.Should().Be("2.5 MB");
    }
}
