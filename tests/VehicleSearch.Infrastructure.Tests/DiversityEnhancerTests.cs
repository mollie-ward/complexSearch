using Microsoft.Extensions.Logging;
using Moq;
using VehicleSearch.Core.Entities;
using VehicleSearch.Core.Models;
using VehicleSearch.Infrastructure.Search;
using Xunit;

namespace VehicleSearch.Infrastructure.Tests;

/// <summary>
/// Unit tests for DiversityEnhancer.
/// </summary>
public class DiversityEnhancerTests
{
    private readonly Mock<ILogger<DiversityEnhancer>> _mockLogger;
    private readonly DiversityEnhancer _enhancer;

    public DiversityEnhancerTests()
    {
        _mockLogger = new Mock<ILogger<DiversityEnhancer>>();
        _enhancer = new DiversityEnhancer(_mockLogger.Object);
    }

    [Fact]
    public void EnsureDiversity_LimitsVehiclesPerMake()
    {
        // Arrange
        var results = new List<VehicleResult>
        {
            CreateVehicleResult("1", "BMW", "3 Series", 0.9),
            CreateVehicleResult("2", "BMW", "5 Series", 0.88),
            CreateVehicleResult("3", "BMW", "X5", 0.86),
            CreateVehicleResult("4", "BMW", "X3", 0.84),
            CreateVehicleResult("5", "BMW", "7 Series", 0.82),
        };

        // Act
        var diverse = _enhancer.EnsureDiversity(results, maxPerMake: 3, maxPerModel: 2);

        // Assert
        Assert.True(diverse.Count <= 3);
        Assert.All(diverse, r => Assert.Equal("BMW", r.Vehicle.Make));
    }

    [Fact]
    public void EnsureDiversity_LimitsVehiclesPerModel()
    {
        // Arrange
        var results = new List<VehicleResult>
        {
            CreateVehicleResult("1", "BMW", "3 Series", 0.9),
            CreateVehicleResult("2", "BMW", "3 Series", 0.88),
            CreateVehicleResult("3", "BMW", "3 Series", 0.86),
            CreateVehicleResult("4", "BMW", "5 Series", 0.84),
        };

        // Act
        var diverse = _enhancer.EnsureDiversity(results, maxPerMake: 5, maxPerModel: 2);

        // Assert
        var bmw3SeriesCount = diverse.Count(r => r.Vehicle.Model == "3 Series");
        Assert.True(bmw3SeriesCount <= 2);
    }

    [Fact]
    public void EnsureDiversity_PreservesRelevanceOrder()
    {
        // Arrange
        var results = new List<VehicleResult>
        {
            CreateVehicleResult("1", "BMW", "3 Series", 0.9),
            CreateVehicleResult("2", "Audi", "A4", 0.85),
            CreateVehicleResult("3", "Mercedes-Benz", "C-Class", 0.88),
        };

        // Act
        var diverse = _enhancer.EnsureDiversity(results, maxPerMake: 1, maxPerModel: 1);

        // Assert
        // Should pick the highest scoring vehicle first (BMW 3 Series)
        Assert.Equal("1", diverse[0].Vehicle.Id);
        // Then Mercedes (0.88) before Audi (0.85)
        Assert.Equal("3", diverse[1].Vehicle.Id);
        Assert.Equal("2", diverse[2].Vehicle.Id);
    }

    [Fact]
    public void EnsureDiversity_WithMaxResults_LimitsTotalResults()
    {
        // Arrange
        var results = new List<VehicleResult>
        {
            CreateVehicleResult("1", "BMW", "3 Series", 0.9),
            CreateVehicleResult("2", "Audi", "A4", 0.85),
            CreateVehicleResult("3", "Mercedes-Benz", "C-Class", 0.88),
            CreateVehicleResult("4", "Toyota", "Camry", 0.82),
            CreateVehicleResult("5", "Honda", "Accord", 0.80),
        };

        // Act
        var diverse = _enhancer.EnsureDiversity(results, maxPerMake: 5, maxPerModel: 5, maxResults: 3);

        // Assert
        Assert.Equal(3, diverse.Count);
    }

    [Fact]
    public void EnsureDiversity_WithMixedMakes_DistributesEvenly()
    {
        // Arrange
        var results = new List<VehicleResult>
        {
            CreateVehicleResult("1", "BMW", "3 Series", 0.9),
            CreateVehicleResult("2", "BMW", "5 Series", 0.88),
            CreateVehicleResult("3", "Audi", "A4", 0.86),
            CreateVehicleResult("4", "Audi", "A6", 0.84),
            CreateVehicleResult("5", "Mercedes-Benz", "C-Class", 0.82),
            CreateVehicleResult("6", "Mercedes-Benz", "E-Class", 0.80),
        };

        // Act
        var diverse = _enhancer.EnsureDiversity(results, maxPerMake: 2, maxPerModel: 1);

        // Assert
        Assert.Equal(6, diverse.Count); // All should fit with these limits
        var makeGroups = diverse.GroupBy(r => r.Vehicle.Make);
        Assert.All(makeGroups, g => Assert.True(g.Count() <= 2));
    }

    [Fact]
    public void EnsureDiversity_WithEmptyResults_ReturnsEmpty()
    {
        // Arrange
        var results = new List<VehicleResult>();

        // Act
        var diverse = _enhancer.EnsureDiversity(results);

        // Assert
        Assert.Empty(diverse);
    }

    [Fact]
    public void EnsureDiversity_WithNullResults_ReturnsEmpty()
    {
        // Arrange
        List<VehicleResult> results = null!;

        // Act
        var diverse = _enhancer.EnsureDiversity(results);

        // Assert
        Assert.Empty(diverse);
    }

    [Fact]
    public void EnsureDiversity_WithSingleResult_ReturnsSingleResult()
    {
        // Arrange
        var results = new List<VehicleResult>
        {
            CreateVehicleResult("1", "BMW", "3 Series", 0.9)
        };

        // Act
        var diverse = _enhancer.EnsureDiversity(results, maxPerMake: 1, maxPerModel: 1);

        // Assert
        Assert.Single(diverse);
        Assert.Equal("1", diverse[0].Vehicle.Id);
    }

    [Fact]
    public void AnalyzeDiversity_CalculatesStatistics()
    {
        // Arrange
        var results = new List<VehicleResult>
        {
            CreateVehicleResult("1", "BMW", "3 Series", 0.9),
            CreateVehicleResult("2", "BMW", "5 Series", 0.88),
            CreateVehicleResult("3", "BMW", "3 Series", 0.86),
            CreateVehicleResult("4", "Audi", "A4", 0.84),
            CreateVehicleResult("5", "Audi", "A4", 0.82),
        };

        // Act
        var stats = _enhancer.AnalyzeDiversity(results);

        // Assert
        Assert.Equal(5, stats.TotalResults);
        Assert.Equal(2, stats.UniqueMakes); // BMW, Audi
        Assert.Equal(3, stats.UniqueModels); // 3 Series, 5 Series, A4
        Assert.Equal(3, stats.MaxVehiclesPerMake); // BMW has 3
        Assert.Equal(2, stats.MaxVehiclesPerModel); // BMW 3 Series and Audi A4 each have 2
    }

    [Fact]
    public void AnalyzeDiversity_WithEmptyResults_ReturnsZeroStats()
    {
        // Arrange
        var results = new List<VehicleResult>();

        // Act
        var stats = _enhancer.AnalyzeDiversity(results);

        // Assert
        Assert.Equal(0, stats.TotalResults);
        Assert.Equal(0, stats.UniqueMakes);
        Assert.Equal(0, stats.UniqueModels);
    }

    [Fact]
    public void AnalyzeDiversity_WithAllUnique_ReturnsCorrectStats()
    {
        // Arrange
        var results = new List<VehicleResult>
        {
            CreateVehicleResult("1", "BMW", "3 Series", 0.9),
            CreateVehicleResult("2", "Audi", "A4", 0.88),
            CreateVehicleResult("3", "Mercedes-Benz", "C-Class", 0.86),
        };

        // Act
        var stats = _enhancer.AnalyzeDiversity(results);

        // Assert
        Assert.Equal(3, stats.TotalResults);
        Assert.Equal(3, stats.UniqueMakes);
        Assert.Equal(3, stats.UniqueModels);
        Assert.Equal(1, stats.MaxVehiclesPerMake);
        Assert.Equal(1, stats.MaxVehiclesPerModel);
        Assert.Equal(1.0, stats.AverageVehiclesPerMake);
        Assert.Equal(1.0, stats.AverageVehiclesPerModel);
    }

    [Fact]
    public void EnsureDiversity_DefaultParameters_UsesCorrectDefaults()
    {
        // Arrange
        var results = new List<VehicleResult>
        {
            CreateVehicleResult("1", "BMW", "3 Series", 0.9),
            CreateVehicleResult("2", "BMW", "3 Series", 0.88),
            CreateVehicleResult("3", "BMW", "3 Series", 0.86),
            CreateVehicleResult("4", "BMW", "3 Series", 0.84),
        };

        // Act
        var diverse = _enhancer.EnsureDiversity(results); // Use defaults: maxPerMake=3, maxPerModel=2

        // Assert
        Assert.Equal(2, diverse.Count); // Should limit to 2 per model
    }

    // Helper method
    private VehicleResult CreateVehicleResult(string id, string make, string model, double score)
    {
        return new VehicleResult
        {
            Vehicle = new Vehicle
            {
                Id = id,
                Make = make,
                Model = model,
                Price = 20000m,
                Mileage = 50000
            },
            Score = score,
            ScoreBreakdown = new SearchScoreBreakdown
            {
                SemanticScore = score,
                FinalScore = score
            }
        };
    }
}
