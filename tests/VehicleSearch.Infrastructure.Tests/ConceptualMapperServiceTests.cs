using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using VehicleSearch.Core.Entities;
using VehicleSearch.Core.Models;
using VehicleSearch.Infrastructure.AI;

namespace VehicleSearch.Infrastructure.Tests;

public class ConceptualMapperServiceTests
{
    private readonly Mock<ILogger<ConceptualMapperService>> _loggerMock;
    private readonly Mock<ILogger<SimilarityScorer>> _scorerLoggerMock;
    private readonly ConceptualMapperService _service;
    private readonly SimilarityScorer _scorer;

    public ConceptualMapperServiceTests()
    {
        _loggerMock = new Mock<ILogger<ConceptualMapperService>>();
        _scorerLoggerMock = new Mock<ILogger<SimilarityScorer>>();
        _scorer = new SimilarityScorer(_scorerLoggerMock.Object);
        _service = new ConceptualMapperService(_loggerMock.Object, _scorer);
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => new ConceptualMapperService(null!, _scorer);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithNullScorer_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => new ConceptualMapperService(_loggerMock.Object, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task MapConceptToAttributesAsync_WithNullConcept_ThrowsArgumentException()
    {
        // Act
        Func<Task> act = async () => await _service.MapConceptToAttributesAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task MapConceptToAttributesAsync_WithEmptyConcept_ThrowsArgumentException()
    {
        // Act
        Func<Task> act = async () => await _service.MapConceptToAttributesAsync(string.Empty);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task MapConceptToAttributesAsync_WithReliableConcept_ReturnsMapping()
    {
        // Act
        var result = await _service.MapConceptToAttributesAsync("reliable");

        // Assert
        result.Should().NotBeNull();
        result!.Concept.Should().Be("reliable");
        result.AttributeWeights.Should().NotBeEmpty();
        result.AttributeWeights.Should().HaveCount(4);
        result.PositiveIndicators.Should().NotBeEmpty();
        result.NegativeIndicators.Should().NotBeEmpty();
    }

    [Fact]
    public async Task MapConceptToAttributesAsync_WithEconomicalConcept_ReturnsMapping()
    {
        // Act
        var result = await _service.MapConceptToAttributesAsync("economical");

        // Assert
        result.Should().NotBeNull();
        result!.Concept.Should().Be("economical");
        result.AttributeWeights.Should().HaveCount(3);
    }

    [Fact]
    public async Task MapConceptToAttributesAsync_WithFamilyCarConcept_ReturnsMapping()
    {
        // Act
        var result = await _service.MapConceptToAttributesAsync("family car");

        // Assert
        result.Should().NotBeNull();
        result!.Concept.Should().Be("family car");
        result.AttributeWeights.Should().HaveCount(3);
    }

    [Fact]
    public async Task MapConceptToAttributesAsync_WithSportyConcept_ReturnsMapping()
    {
        // Act
        var result = await _service.MapConceptToAttributesAsync("sporty");

        // Assert
        result.Should().NotBeNull();
        result!.Concept.Should().Be("sporty");
        result.AttributeWeights.Should().HaveCount(3);
    }

    [Fact]
    public async Task MapConceptToAttributesAsync_WithLuxuryConcept_ReturnsMapping()
    {
        // Act
        var result = await _service.MapConceptToAttributesAsync("luxury");

        // Assert
        result.Should().NotBeNull();
        result!.Concept.Should().Be("luxury");
        result.AttributeWeights.Should().HaveCount(3);
    }

    [Fact]
    public async Task MapConceptToAttributesAsync_WithPracticalConcept_ReturnsMapping()
    {
        // Act
        var result = await _service.MapConceptToAttributesAsync("practical");

        // Assert
        result.Should().NotBeNull();
        result!.Concept.Should().Be("practical");
        result.AttributeWeights.Should().HaveCount(3);
    }

    [Fact]
    public async Task MapConceptToAttributesAsync_WithUnknownConcept_ReturnsNull()
    {
        // Act
        var result = await _service.MapConceptToAttributesAsync("unknown");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task MapConceptToAttributesAsync_IsCaseInsensitive()
    {
        // Act
        var result1 = await _service.MapConceptToAttributesAsync("RELIABLE");
        var result2 = await _service.MapConceptToAttributesAsync("Reliable");
        var result3 = await _service.MapConceptToAttributesAsync("reliable");

        // Assert
        result1.Should().NotBeNull();
        result2.Should().NotBeNull();
        result3.Should().NotBeNull();
        result1!.Concept.Should().Be(result2!.Concept);
        result2!.Concept.Should().Be(result3!.Concept);
    }

    [Fact]
    public async Task MapConceptToAttributesAsync_AllConcepts_HaveWeightsSummingToOne()
    {
        // Arrange
        var concepts = new[] { "reliable", "economical", "family car", "sporty", "luxury", "practical" };

        foreach (var conceptName in concepts)
        {
            // Act
            var concept = await _service.MapConceptToAttributesAsync(conceptName);

            // Assert
            concept.Should().NotBeNull();
            var totalWeight = concept!.AttributeWeights.Sum(w => w.Weight);
            totalWeight.Should().BeApproximately(1.0, 0.01, 
                $"Weights for '{conceptName}' should sum to 1.0");
        }
    }

    [Fact]
    public async Task ComputeSimilarityAsync_WithNullVehicle_ThrowsArgumentNullException()
    {
        // Arrange
        var concept = ConceptMappings.Mappings["reliable"];

        // Act
        Func<Task> act = async () => await _service.ComputeSimilarityAsync(null!, concept);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ComputeSimilarityAsync_WithNullConcept_ThrowsArgumentNullException()
    {
        // Arrange
        var vehicle = CreateTestVehicle();

        // Act
        Func<Task> act = async () => await _service.ComputeSimilarityAsync(vehicle, null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ComputeSimilarityAsync_WithReliableVehicle_ReturnsHighScore()
    {
        // Arrange
        var vehicle = CreateTestVehicle(mileage: 30000, serviceHistory: true);
        var concept = ConceptMappings.Mappings["reliable"];

        // Act
        var result = await _service.ComputeSimilarityAsync(vehicle, concept);

        // Assert
        result.Should().NotBeNull();
        result.OverallScore.Should().BeGreaterThanOrEqualTo(0.6);
        result.ComponentScores.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ExplainRelevanceAsync_WithNullVehicle_ThrowsArgumentNullException()
    {
        // Arrange
        var query = CreateTestQuery();

        // Act
        Func<Task> act = async () => await _service.ExplainRelevanceAsync(null!, query);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ExplainRelevanceAsync_WithNullQuery_ThrowsArgumentNullException()
    {
        // Arrange
        var vehicle = CreateTestVehicle();

        // Act
        Func<Task> act = async () => await _service.ExplainRelevanceAsync(vehicle, null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ExplainRelevanceAsync_WithMakeMatch_GeneratesExplanation()
    {
        // Arrange
        var vehicle = CreateTestVehicle(make: "BMW");
        var query = new ParsedQuery
        {
            OriginalQuery = "BMW",
            Intent = QueryIntent.Search,
            Entities = new List<ExtractedEntity>
            {
                new ExtractedEntity
                {
                    Type = EntityType.Make,
                    Value = "BMW",
                    Confidence = 0.9
                }
            }
        };

        // Act
        var result = await _service.ExplainRelevanceAsync(vehicle, query);

        // Assert
        result.Should().NotBeNull();
        result.Score.Should().BeGreaterThanOrEqualTo(0.0);
        result.Score.Should().BeLessThanOrEqualTo(1.0);
        result.Explanation.Should().NotBeNullOrWhiteSpace();
        result.Components.Should().NotBeEmpty();
        result.Components.Should().Contain(c => c.Factor == "Make Match");
    }

    [Fact]
    public async Task ExplainRelevanceAsync_WithQualitativeTerm_IncludesConceptualFactor()
    {
        // Arrange
        var vehicle = CreateTestVehicle(mileage: 40000, serviceHistory: true);
        var query = new ParsedQuery
        {
            OriginalQuery = "reliable car",
            Intent = QueryIntent.Search,
            Entities = new List<ExtractedEntity>
            {
                new ExtractedEntity
                {
                    Type = EntityType.QualitativeTerm,
                    Value = "reliable",
                    Confidence = 0.9
                }
            }
        };

        // Act
        var result = await _service.ExplainRelevanceAsync(vehicle, query);

        // Assert
        result.Should().NotBeNull();
        result.Components.Should().Contain(c => c.Factor.Contains("Conceptual: reliable"));
    }

    [Fact]
    public async Task ExplainRelevanceAsync_WithMultipleFactors_GeneratesDetailedExplanation()
    {
        // Arrange
        var vehicle = CreateTestVehicle(make: "BMW", mileage: 40000, serviceHistory: true, price: 18000m);
        var query = new ParsedQuery
        {
            OriginalQuery = "reliable BMW under 20000",
            Intent = QueryIntent.Search,
            Entities = new List<ExtractedEntity>
            {
                new ExtractedEntity
                {
                    Type = EntityType.Make,
                    Value = "BMW",
                    Confidence = 0.9
                },
                new ExtractedEntity
                {
                    Type = EntityType.QualitativeTerm,
                    Value = "reliable",
                    Confidence = 0.85
                },
                new ExtractedEntity
                {
                    Type = EntityType.Price,
                    Value = "20000",
                    Confidence = 0.9
                }
            }
        };

        // Act
        var result = await _service.ExplainRelevanceAsync(vehicle, query);

        // Assert
        result.Should().NotBeNull();
        result.Explanation.Should().Contain("BMW");
        result.Explanation.Should().Contain("reliable");
        result.Components.Count.Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task ExplainRelevanceAsync_ScoreComponents_SumCorrectly()
    {
        // Arrange
        var vehicle = CreateTestVehicle(make: "BMW", mileage: 40000);
        var query = new ParsedQuery
        {
            OriginalQuery = "BMW",
            Intent = QueryIntent.Search,
            Entities = new List<ExtractedEntity>
            {
                new ExtractedEntity
                {
                    Type = EntityType.Make,
                    Value = "BMW",
                    Confidence = 0.9
                }
            }
        };

        // Act
        var result = await _service.ExplainRelevanceAsync(vehicle, query);

        // Assert
        result.Should().NotBeNull();
        var totalWeight = result.Components.Sum(c => c.Weight);
        totalWeight.Should().BeGreaterThan(0);
    }

    private Vehicle CreateTestVehicle(
        string make = "Toyota",
        string model = "Corolla",
        int mileage = 50000,
        decimal price = 15000m,
        string fuelType = "Petrol",
        decimal engineSize = 1.6m,
        string bodyType = "Hatchback",
        int numberOfDoors = 5,
        bool serviceHistory = false)
    {
        return new Vehicle
        {
            Id = "TEST001",
            Make = make,
            Model = model,
            Mileage = mileage,
            Price = price,
            FuelType = fuelType,
            EngineSize = engineSize,
            BodyType = bodyType,
            NumberOfDoors = numberOfDoors,
            ServiceHistoryPresent = serviceHistory,
            Description = "Standard vehicle description",
            Features = new List<string>(),
            SaleLocation = "London",
            TransmissionType = "Manual"
        };
    }

    private ParsedQuery CreateTestQuery()
    {
        return new ParsedQuery
        {
            OriginalQuery = "test query",
            Intent = QueryIntent.Search,
            Entities = new List<ExtractedEntity>()
        };
    }
}
