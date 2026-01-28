using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using VehicleSearch.Core.Entities;
using VehicleSearch.Core.Models;
using VehicleSearch.Infrastructure.AI;

namespace VehicleSearch.Infrastructure.Tests;

public class SimilarityScorerTests
{
    private readonly Mock<ILogger<SimilarityScorer>> _loggerMock;
    private readonly SimilarityScorer _scorer;

    public SimilarityScorerTests()
    {
        _loggerMock = new Mock<ILogger<SimilarityScorer>>();
        _scorer = new SimilarityScorer(_loggerMock.Object);
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => new SimilarityScorer(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ComputeScore_WithNullVehicle_ThrowsArgumentNullException()
    {
        // Arrange
        var concept = ConceptMappings.Mappings["reliable"];

        // Act
        Action act = () => _scorer.ComputeScore(null!, concept);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ComputeScore_WithNullConcept_ThrowsArgumentNullException()
    {
        // Arrange
        var vehicle = CreateTestVehicle();

        // Act
        Action act = () => _scorer.ComputeScore(vehicle, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ComputeScore_ReliableConcept_HighScoreForLowMileage()
    {
        // Arrange
        var vehicle = CreateTestVehicle(mileage: 45000, serviceHistory: true);
        var concept = ConceptMappings.Mappings["reliable"];

        // Act
        var result = _scorer.ComputeScore(vehicle, concept);

        // Assert
        result.Should().NotBeNull();
        result.OverallScore.Should().BeGreaterThan(0.7);
        result.ComponentScores.Should().ContainKey("mileage");
        result.ComponentScores["mileage"].Should().BeGreaterThan(0.8);
    }

    [Fact]
    public void ComputeScore_ReliableConcept_LowScoreForHighMileage()
    {
        // Arrange
        var vehicle = CreateTestVehicle(mileage: 120000, serviceHistory: false);
        var concept = ConceptMappings.Mappings["reliable"];

        // Act
        var result = _scorer.ComputeScore(vehicle, concept);

        // Assert
        result.Should().NotBeNull();
        result.OverallScore.Should().BeLessThan(0.5);
        result.ComponentScores.Should().ContainKey("mileage");
        result.ComponentScores["mileage"].Should().BeLessThan(0.3);
    }

    [Fact]
    public void ComputeScore_EconomicalConcept_HighScoreForHybrid()
    {
        // Arrange
        var vehicle = CreateTestVehicle(fuelType: "Hybrid", engineSize: 1.8m, price: 18000m);
        var concept = ConceptMappings.Mappings["economical"];

        // Act
        var result = _scorer.ComputeScore(vehicle, concept);

        // Assert
        result.Should().NotBeNull();
        result.OverallScore.Should().BeGreaterThan(0.7);
        result.ComponentScores.Should().ContainKey("fuelType");
        result.ComponentScores["fuelType"].Should().Be(1.0);
        result.ComponentScores.Should().ContainKey("engineSize");
        result.ComponentScores["engineSize"].Should().BeGreaterThan(0.7);
    }

    [Fact]
    public void ComputeScore_EconomicalConcept_LowScoreForLargePetrolEngine()
    {
        // Arrange
        var vehicle = CreateTestVehicle(fuelType: "Petrol", engineSize: 3.5m, price: 45000m);
        var concept = ConceptMappings.Mappings["economical"];

        // Act
        var result = _scorer.ComputeScore(vehicle, concept);

        // Assert
        result.Should().NotBeNull();
        result.OverallScore.Should().BeLessThan(0.6);
        result.ComponentScores.Should().ContainKey("engineSize");
        result.ComponentScores["engineSize"].Should().BeLessThan(0.3);
    }

    [Fact]
    public void ComputeScore_FamilyCarConcept_HighScoreForSUV()
    {
        // Arrange
        var vehicle = CreateTestVehicle(bodyType: "SUV", numberOfDoors: 5);
        var concept = ConceptMappings.Mappings["family car"];

        // Act
        var result = _scorer.ComputeScore(vehicle, concept);

        // Assert
        result.Should().NotBeNull();
        result.OverallScore.Should().BeGreaterThan(0.6);
        result.ComponentScores.Should().ContainKey("bodyType");
        result.ComponentScores["bodyType"].Should().Be(1.0);
        result.ComponentScores.Should().ContainKey("numberOfDoors");
    }

    [Fact]
    public void ComputeScore_SportyConcept_HighScoreForLargeEngineCoupe()
    {
        // Arrange
        var vehicle = CreateTestVehicle(engineSize: 3.0m, bodyType: "Coupe", transmission: "Manual");
        var concept = ConceptMappings.Mappings["sporty"];

        // Act
        var result = _scorer.ComputeScore(vehicle, concept);

        // Assert
        result.Should().NotBeNull();
        result.OverallScore.Should().BeGreaterThan(0.7);
        result.ComponentScores.Should().ContainKey("engineSize");
        result.ComponentScores["engineSize"].Should().BeGreaterThan(0.8);
    }

    [Fact]
    public void ComputeScore_LuxuryConcept_HighScoreForBMW()
    {
        // Arrange
        var vehicle = CreateTestVehicle(make: "BMW", price: 45000m);
        var concept = ConceptMappings.Mappings["luxury"];

        // Act
        var result = _scorer.ComputeScore(vehicle, concept);

        // Assert
        result.Should().NotBeNull();
        result.OverallScore.Should().BeGreaterThan(0.6);
        result.ComponentScores.Should().ContainKey("make");
        result.ComponentScores["make"].Should().Be(1.0);
        result.ComponentScores.Should().ContainKey("price");
    }

    [Fact]
    public void ComputeScore_PracticalConcept_HighScoreForEstate()
    {
        // Arrange
        var vehicle = CreateTestVehicle(bodyType: "Estate", numberOfDoors: 5, fuelType: "Diesel");
        var concept = ConceptMappings.Mappings["practical"];

        // Act
        var result = _scorer.ComputeScore(vehicle, concept);

        // Assert
        result.Should().NotBeNull();
        result.OverallScore.Should().BeGreaterThan(0.7);
        result.ComponentScores.Should().ContainKey("bodyType");
        result.ComponentScores["bodyType"].Should().Be(1.0);
    }

    [Fact]
    public void ComputeScore_WithPositiveIndicators_AddsBoost()
    {
        // Arrange
        var vehicle = CreateTestVehicle(
            mileage: 50000,
            serviceHistory: true,
            description: "Full service history, one owner, warranty included");
        var concept = ConceptMappings.Mappings["reliable"];

        // Act
        var result = _scorer.ComputeScore(vehicle, concept);

        // Assert
        result.Should().NotBeNull();
        result.DescriptionBoost.Should().BeGreaterThan(0.0);
    }

    [Fact]
    public void ComputeScore_WithNegativeIndicators_ReducesBoost()
    {
        // Arrange
        var vehicle = CreateTestVehicle(
            mileage: 50000,
            serviceHistory: true,
            description: "High mileage, accident damage, no service history");
        var concept = ConceptMappings.Mappings["reliable"];

        // Act
        var result = _scorer.ComputeScore(vehicle, concept);

        // Assert
        result.Should().NotBeNull();
        result.DescriptionBoost.Should().BeLessThan(0.0);
    }

    [Fact]
    public void ComputeScore_DescriptionBoost_CappedAtMaximum()
    {
        // Arrange
        var vehicle = CreateTestVehicle(
            mileage: 50000,
            serviceHistory: true,
            description: "Full service history full service history full service history full service history full service history full service history full service history full service history full service history full service history");
        var concept = ConceptMappings.Mappings["reliable"];

        // Act
        var result = _scorer.ComputeScore(vehicle, concept);

        // Assert
        result.Should().NotBeNull();
        result.DescriptionBoost.Should().BeLessThanOrEqualTo(0.5);
    }

    [Fact]
    public void ComputeScore_FinalScore_ClampedBetweenZeroAndOne()
    {
        // Arrange
        var vehicle = CreateTestVehicle(mileage: 10000, serviceHistory: true);
        var concept = ConceptMappings.Mappings["reliable"];

        // Act
        var result = _scorer.ComputeScore(vehicle, concept);

        // Assert
        result.Should().NotBeNull();
        result.OverallScore.Should().BeGreaterThanOrEqualTo(0.0);
        result.OverallScore.Should().BeLessThanOrEqualTo(1.0);
    }

    [Fact]
    public void ComputeScore_TracksMatchingAttributes()
    {
        // Arrange
        var vehicle = CreateTestVehicle(mileage: 45000, serviceHistory: true);
        var concept = ConceptMappings.Mappings["reliable"];

        // Act
        var result = _scorer.ComputeScore(vehicle, concept);

        // Assert
        result.Should().NotBeNull();
        result.MatchingAttributes.Should().Contain("mileage");
        result.MatchingAttributes.Should().Contain("serviceHistoryPresent");
    }

    [Fact]
    public void ComputeScore_TracksMismatchingAttributes()
    {
        // Arrange
        var vehicle = CreateTestVehicle(mileage: 150000, serviceHistory: false);
        var concept = ConceptMappings.Mappings["reliable"];

        // Act
        var result = _scorer.ComputeScore(vehicle, concept);

        // Assert
        result.Should().NotBeNull();
        result.MismatchingAttributes.Should().Contain("mileage");
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
        string transmission = "Manual",
        bool serviceHistory = false,
        string description = "Standard vehicle description")
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
            TransmissionType = transmission,
            ServiceHistoryPresent = serviceHistory,
            Description = description,
            Features = new List<string>(),
            MotExpiryDate = DateTime.Now.AddDays(180)
        };
    }
}
