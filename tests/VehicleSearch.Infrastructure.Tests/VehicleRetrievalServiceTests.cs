using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using VehicleSearch.Core.Models;
using VehicleSearch.Infrastructure.Search;

namespace VehicleSearch.Infrastructure.Tests;

public class VehicleRetrievalServiceTests
{
    private readonly Mock<IOptions<AzureSearchConfig>> _configMock;
    private readonly Mock<ILogger<VehicleRetrievalService>> _loggerMock;
    private readonly AzureSearchConfig _config;

    public VehicleRetrievalServiceTests()
    {
        _config = new AzureSearchConfig
        {
            Endpoint = "https://test-search.search.windows.net",
            ApiKey = "test-key",
            IndexName = "test-index",
            VectorDimensions = 1536
        };

        _configMock = new Mock<IOptions<AzureSearchConfig>>();
        _configMock.Setup(x => x.Value).Returns(_config);

        _loggerMock = new Mock<ILogger<VehicleRetrievalService>>();
    }

    [Fact]
    public void Constructor_WithValidConfig_CreatesInstance()
    {
        // Act
        var service = new VehicleRetrievalService(_configMock.Object, _loggerMock.Object);

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNullConfig_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => new VehicleRetrievalService(null!, _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => new VehicleRetrievalService(_configMock.Object, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithEmptyEndpoint_ThrowsInvalidOperationException()
    {
        // Arrange
        _config.Endpoint = string.Empty;

        // Act
        Action act = () => new VehicleRetrievalService(_configMock.Object, _loggerMock.Object);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*endpoint*");
    }

    [Fact]
    public void Constructor_WithEmptyApiKey_ThrowsInvalidOperationException()
    {
        // Arrange
        _config.ApiKey = string.Empty;

        // Act
        Action act = () => new VehicleRetrievalService(_configMock.Object, _loggerMock.Object);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*API key*");
    }

    [Fact]
    public async Task GetVehicleByIdAsync_WithNullId_ThrowsArgumentException()
    {
        // Arrange
        var service = new VehicleRetrievalService(_configMock.Object, _loggerMock.Object);

        // Act
        Func<Task> act = async () => await service.GetVehicleByIdAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Vehicle ID cannot be null or empty*");
    }

    [Fact]
    public async Task GetVehicleByIdAsync_WithEmptyId_ThrowsArgumentException()
    {
        // Arrange
        var service = new VehicleRetrievalService(_configMock.Object, _loggerMock.Object);

        // Act
        Func<Task> act = async () => await service.GetVehicleByIdAsync(string.Empty);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Vehicle ID cannot be null or empty*");
    }

    [Fact]
    public async Task GetVehiclesByIdsAsync_WithEmptyList_ReturnsEmptyCollection()
    {
        // Arrange
        var service = new VehicleRetrievalService(_configMock.Object, _loggerMock.Object);
        var ids = new List<string>();

        // Act
        var result = await service.GetVehiclesByIdsAsync(ids);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetTotalCountAsync_CallsSearchClient()
    {
        // Arrange
        var service = new VehicleRetrievalService(_configMock.Object, _loggerMock.Object);

        // Act & Assert
        // This will fail when trying to connect to Azure Search
        Func<Task> act = async () => await service.GetTotalCountAsync();
        await act.Should().ThrowAsync<Exception>();
    }
}
