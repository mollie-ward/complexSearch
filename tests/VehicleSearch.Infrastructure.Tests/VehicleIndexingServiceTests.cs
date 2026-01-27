using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using VehicleSearch.Core.Entities;
using VehicleSearch.Core.Interfaces;
using VehicleSearch.Core.Models;
using VehicleSearch.Infrastructure.Search;

namespace VehicleSearch.Infrastructure.Tests;

public class VehicleIndexingServiceTests
{
    private readonly Mock<IOptions<AzureSearchConfig>> _configMock;
    private readonly Mock<IEmbeddingService> _embeddingServiceMock;
    private readonly Mock<ILogger<VehicleIndexingService>> _loggerMock;
    private readonly AzureSearchConfig _config;

    public VehicleIndexingServiceTests()
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

        _embeddingServiceMock = new Mock<IEmbeddingService>();
        _loggerMock = new Mock<ILogger<VehicleIndexingService>>();
    }

    [Fact]
    public void Constructor_WithValidConfig_CreatesInstance()
    {
        // Act
        var service = new VehicleIndexingService(_configMock.Object, _embeddingServiceMock.Object, _loggerMock.Object);

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNullConfig_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => new VehicleIndexingService(null!, _embeddingServiceMock.Object, _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithNullEmbeddingService_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => new VehicleIndexingService(_configMock.Object, null!, _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithEmptyEndpoint_ThrowsInvalidOperationException()
    {
        // Arrange
        _config.Endpoint = string.Empty;

        // Act
        Action act = () => new VehicleIndexingService(_configMock.Object, _embeddingServiceMock.Object, _loggerMock.Object);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*endpoint*");
    }

    [Fact]
    public async Task IndexVehiclesAsync_WithEmptyList_ReturnsEmptyResult()
    {
        // Arrange
        var service = new VehicleIndexingService(_configMock.Object, _embeddingServiceMock.Object, _loggerMock.Object);
        var vehicles = new List<Vehicle>();

        // Act
        var result = await service.IndexVehiclesAsync(vehicles);

        // Assert
        result.Should().NotBeNull();
        result.TotalVehicles.Should().Be(0);
    }

    [Fact]
    public async Task IndexVehiclesAsync_GeneratesEmbeddingsForAllVehicles()
    {
        // Arrange
        var service = new VehicleIndexingService(_configMock.Object, _embeddingServiceMock.Object, _loggerMock.Object);
        
        var vehicles = new List<Vehicle>
        {
            new Vehicle
            {
                Id = "TEST001",
                Description = "Test vehicle 1",
                Make = "Test",
                Model = "Model",
                Price = 10000,
                Mileage = 20000
            },
            new Vehicle
            {
                Id = "TEST002",
                Description = "Test vehicle 2",
                Make = "Test",
                Model = "Model",
                Price = 15000,
                Mileage = 25000
            }
        };

        var embeddings = new List<VehicleEmbedding>
        {
            new VehicleEmbedding { VehicleId = "TEST001", Vector = new float[1536], Dimensions = 1536 },
            new VehicleEmbedding { VehicleId = "TEST002", Vector = new float[1536], Dimensions = 1536 }
        };

        _embeddingServiceMock
            .Setup(x => x.GenerateBatchEmbeddingsAsync(It.IsAny<IEnumerable<Vehicle>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(embeddings);

        // Act
        var result = await service.IndexVehiclesAsync(vehicles);

        // Assert
        result.Should().NotBeNull();
        result.TotalVehicles.Should().Be(2);
        result.EmbeddingsGenerated.Should().Be(2);
        
        // Verify embedding service was called
        _embeddingServiceMock.Verify(x => x.GenerateBatchEmbeddingsAsync(
            It.IsAny<IEnumerable<Vehicle>>(),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task IndexVehicleAsync_CallsIndexVehiclesAsync()
    {
        // Arrange
        var service = new VehicleIndexingService(_configMock.Object, _embeddingServiceMock.Object, _loggerMock.Object);
        var vehicle = new Vehicle
        {
            Id = "TEST001",
            Description = "Test vehicle",
            Make = "Test",
            Model = "Model",
            Price = 10000,
            Mileage = 20000
        };

        var embedding = new VehicleEmbedding
        {
            VehicleId = "TEST001",
            Vector = new float[1536],
            Dimensions = 1536
        };

        _embeddingServiceMock
            .Setup(x => x.GenerateBatchEmbeddingsAsync(It.IsAny<IEnumerable<Vehicle>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { embedding });

        // Act
        var result = await service.IndexVehicleAsync(vehicle);

        // Assert
        result.Should().NotBeNull();
        result.TotalVehicles.Should().Be(1);
        result.EmbeddingsGenerated.Should().Be(1);
    }
}
