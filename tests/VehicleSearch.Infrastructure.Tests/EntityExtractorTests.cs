using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using VehicleSearch.Core.Models;
using VehicleSearch.Infrastructure.AI;

namespace VehicleSearch.Infrastructure.Tests;

public class EntityExtractorTests
{
    private readonly Mock<ILogger<EntityExtractor>> _loggerMock;
    private readonly EntityExtractor _extractor;

    public EntityExtractorTests()
    {
        _loggerMock = new Mock<ILogger<EntityExtractor>>();
        _extractor = new EntityExtractor(_loggerMock.Object);
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => new EntityExtractor(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task ExtractAsync_WithNullQuery_ThrowsArgumentException()
    {
        // Act
        Func<Task> act = async () => await _extractor.ExtractAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Query cannot be null or empty*");
    }

    [Fact]
    public async Task ExtractAsync_WithEmptyQuery_ThrowsArgumentException()
    {
        // Act
        Func<Task> act = async () => await _extractor.ExtractAsync(string.Empty);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Query cannot be null or empty*");
    }

    [Fact]
    public async Task ExtractAsync_BMWUnder20k_ExtractsMakeAndPrice()
    {
        // Arrange
        var query = "Show me BMW under £20,000";

        // Act
        var entities = (await _extractor.ExtractAsync(query)).ToList();

        // Assert
        entities.Should().HaveCountGreaterThanOrEqualTo(2);
        
        var makeEntity = entities.FirstOrDefault(e => e.Type == EntityType.Make);
        makeEntity.Should().NotBeNull();
        makeEntity!.Value.Should().Be("BMW");
        makeEntity.Confidence.Should().Be(1.0);

        var priceEntity = entities.FirstOrDefault(e => e.Type == EntityType.Price);
        priceEntity.Should().NotBeNull();
        priceEntity!.Value.Should().Be("20000");
    }

    [Fact]
    public async Task ExtractAsync_MultipleEntities_ExtractsAll()
    {
        // Arrange
        var query = "Show me BMW 3 series under £20k with leather seats";

        // Act
        var entities = (await _extractor.ExtractAsync(query)).ToList();

        // Assert
        entities.Should().HaveCountGreaterThanOrEqualTo(3);
        
        entities.Should().Contain(e => e.Type == EntityType.Make && e.Value == "BMW");
        entities.Should().Contain(e => e.Type == EntityType.Price);
        entities.Should().Contain(e => e.Type == EntityType.Feature);
    }

    [Theory]
    [InlineData("BMW", "BMW")]
    [InlineData("Audi", "Audi")]
    [InlineData("Mercedes", "Mercedes")]
    [InlineData("beamer", "BMW")]
    [InlineData("merc", "Mercedes-Benz")]
    public async Task ExtractAsync_Make_ExtractsCorrectly(string input, string expectedMake)
    {
        // Arrange
        var query = $"Show me {input} cars";

        // Act
        var entities = (await _extractor.ExtractAsync(query)).ToList();

        // Assert
        var makeEntity = entities.FirstOrDefault(e => e.Type == EntityType.Make);
        makeEntity.Should().NotBeNull();
        makeEntity!.Value.Should().Be(expectedMake);
    }

    [Theory]
    [InlineData("Petrol", "Petrol")]
    [InlineData("Diesel", "Diesel")]
    [InlineData("Electric", "Electric")]
    [InlineData("Hybrid", "Hybrid")]
    public async Task ExtractAsync_FuelType_ExtractsCorrectly(string fuelType, string expected)
    {
        // Arrange
        var query = $"Show me {fuelType} cars";

        // Act
        var entities = (await _extractor.ExtractAsync(query)).ToList();

        // Assert
        var fuelEntity = entities.FirstOrDefault(e => e.Type == EntityType.FuelType);
        fuelEntity.Should().NotBeNull();
        fuelEntity!.Value.Should().Be(expected);
        fuelEntity.Confidence.Should().Be(0.95);
    }

    [Theory]
    [InlineData("Manual", "Manual")]
    [InlineData("Automatic", "Automatic")]
    public async Task ExtractAsync_Transmission_ExtractsCorrectly(string transmission, string expected)
    {
        // Arrange
        var query = $"Show me {transmission} cars";

        // Act
        var entities = (await _extractor.ExtractAsync(query)).ToList();

        // Assert
        var transmissionEntity = entities.FirstOrDefault(e => e.Type == EntityType.Transmission);
        transmissionEntity.Should().NotBeNull();
        transmissionEntity!.Value.Should().Be(expected);
        transmissionEntity.Confidence.Should().Be(0.95);
    }

    [Theory]
    [InlineData("SUV", "SUV")]
    [InlineData("Sedan", "Sedan")]
    [InlineData("Hatchback", "Hatchback")]
    public async Task ExtractAsync_BodyType_ExtractsCorrectly(string bodyType, string expected)
    {
        // Arrange
        var query = $"Show me {bodyType} cars";

        // Act
        var entities = (await _extractor.ExtractAsync(query)).ToList();

        // Assert
        var bodyEntity = entities.FirstOrDefault(e => e.Type == EntityType.BodyType);
        bodyEntity.Should().NotBeNull();
        bodyEntity!.Value.Should().Be(expected);
        bodyEntity.Confidence.Should().Be(0.9);
    }

    [Theory]
    [InlineData("leather", "Leather")]
    [InlineData("navigation", "Navigation")]
    [InlineData("parking sensors", "Parking Sensors")]
    public async Task ExtractAsync_Features_ExtractsCorrectly(string feature, string expected)
    {
        // Arrange
        var query = $"Show me cars with {feature}";

        // Act
        var entities = (await _extractor.ExtractAsync(query)).ToList();

        // Assert
        var featureEntity = entities.FirstOrDefault(e => e.Type == EntityType.Feature);
        featureEntity.Should().NotBeNull();
        featureEntity!.Value.Should().Be(expected);
    }

    [Theory]
    [InlineData("Manchester", "Manchester")]
    [InlineData("London", "London")]
    [InlineData("Leeds", "Leeds")]
    public async Task ExtractAsync_Location_ExtractsCorrectly(string location, string expected)
    {
        // Arrange
        var query = $"Show me cars in {location}";

        // Act
        var entities = (await _extractor.ExtractAsync(query)).ToList();

        // Assert
        var locationEntity = entities.FirstOrDefault(e => e.Type == EntityType.Location);
        locationEntity.Should().NotBeNull();
        locationEntity!.Value.Should().Be(expected);
        locationEntity.Confidence.Should().Be(0.9);
    }

    [Theory]
    [InlineData("reliable", "Reliable")]
    [InlineData("economical", "Economical")]
    [InlineData("family car", "Family Car")]
    public async Task ExtractAsync_QualitativeTerm_ExtractsCorrectly(string term, string expected)
    {
        // Arrange
        var query = $"Show me {term}";

        // Act
        var entities = (await _extractor.ExtractAsync(query)).ToList();

        // Assert
        var qualitativeEntity = entities.FirstOrDefault(e => e.Type == EntityType.QualitativeTerm);
        qualitativeEntity.Should().NotBeNull();
        qualitativeEntity!.Value.Should().Be(expected);
        qualitativeEntity.Confidence.Should().Be(0.75);
    }

    [Fact]
    public async Task ExtractAsync_ComplexQuery_ExtractsAllEntities()
    {
        // Arrange
        var query = "Show me BMW 3 series diesel automatic under £20k with leather in Manchester";

        // Act
        var entities = (await _extractor.ExtractAsync(query)).ToList();

        // Assert
        entities.Should().HaveCountGreaterThanOrEqualTo(5);
        
        entities.Should().Contain(e => e.Type == EntityType.Make && e.Value == "BMW");
        entities.Should().Contain(e => e.Type == EntityType.FuelType && e.Value == "Diesel");
        entities.Should().Contain(e => e.Type == EntityType.Transmission && e.Value == "Automatic");
        entities.Should().Contain(e => e.Type == EntityType.Price);
        entities.Should().Contain(e => e.Type == EntityType.Feature);
        entities.Should().Contain(e => e.Type == EntityType.Location && e.Value == "Manchester");
    }

    [Fact]
    public async Task ExtractAsync_DuplicateEntities_RemovesDuplicates()
    {
        // Arrange
        var query = "Show me BMW BMW cars"; // BMW appears twice

        // Act
        var entities = (await _extractor.ExtractAsync(query)).ToList();

        // Assert
        var makeEntities = entities.Where(e => e.Type == EntityType.Make && e.Value == "BMW").ToList();
        makeEntities.Should().HaveCount(1);
    }

    [Fact]
    public async Task ExtractAsync_FuzzyMakeMatch_ExtractsWithLowerConfidence()
    {
        // Arrange
        var query = "Show me auddi cars"; // Typo: auddi instead of audi

        // Act
        var entities = (await _extractor.ExtractAsync(query)).ToList();

        // Assert
        var makeEntity = entities.FirstOrDefault(e => e.Type == EntityType.Make);
        makeEntity.Should().NotBeNull();
        makeEntity!.Value.Should().Be("Audi");
        makeEntity.Confidence.Should().BeLessThan(1.0); // Fuzzy match has lower confidence
    }
}
