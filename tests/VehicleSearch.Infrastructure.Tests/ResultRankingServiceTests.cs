using Microsoft.Extensions.Logging;
using Moq;
using VehicleSearch.Core.Entities;
using VehicleSearch.Core.Models;
using VehicleSearch.Infrastructure.Search;
using Xunit;

namespace VehicleSearch.Infrastructure.Tests;

/// <summary>
/// Unit tests for ResultRankingService.
/// </summary>
public class ResultRankingServiceTests
{
    private readonly Mock<ILogger<ResultRankingService>> _mockLogger;
    private readonly ResultRankingService _service;

    public ResultRankingServiceTests()
    {
        _mockLogger = new Mock<ILogger<ResultRankingService>>();
        _service = new ResultRankingService(_mockLogger.Object);
    }

    [Fact]
    public async Task RankResultsAsync_WithValidResults_ReturnsRankedList()
    {
        // Arrange
        var results = CreateTestResults();
        var query = CreateTestQuery();

        // Act
        var rankedResults = await _service.RankResultsAsync(results, query);

        // Assert
        Assert.NotNull(rankedResults);
        Assert.NotEmpty(rankedResults);
        Assert.True(rankedResults.Count <= results.Count); // Diversity may reduce count
    }

    [Fact]
    public async Task RankResultsAsync_OrdersByScore_DescendingOrder()
    {
        // Arrange
        var results = CreateTestResults();
        var query = CreateTestQuery();

        // Act
        var rankedResults = await _service.RankResultsAsync(results, query);

        // Assert
        for (int i = 0; i < rankedResults.Count - 1; i++)
        {
            Assert.True(rankedResults[i].Score >= rankedResults[i + 1].Score,
                $"Results not properly ordered: {rankedResults[i].Score} < {rankedResults[i + 1].Score}");
        }
    }

    [Fact]
    public async Task RerankResultsAsync_WithWeightedScore_AppliesCorrectWeights()
    {
        // Arrange
        var results = CreateTestResults();
        var query = CreateTestQuery();
        var strategy = new RerankingStrategy
        {
            Approach = RerankingApproach.WeightedScore,
            FactorWeights = new Dictionary<RankingFactor, double>
            {
                [RankingFactor.SemanticRelevance] = 0.5,
                [RankingFactor.ExactMatchCount] = 0.3,
                [RankingFactor.VehicleCondition] = 0.2
            },
            BusinessRules = new List<BusinessRule>(),
            ApplyDiversity = false
        };

        // Act
        var rankedResults = await _service.RerankResultsAsync(results, strategy, query);

        // Assert
        Assert.NotNull(rankedResults);
        Assert.Equal(results.Count, rankedResults.Count);
        Assert.All(rankedResults, r => Assert.InRange(r.Score, 0.0, 1.0));
    }

    [Fact]
    public async Task RerankResultsAsync_WithInvalidWeights_NormalizesWeights()
    {
        // Arrange
        var results = CreateTestResults();
        var query = CreateTestQuery();
        var strategy = new RerankingStrategy
        {
            Approach = RerankingApproach.WeightedScore,
            FactorWeights = new Dictionary<RankingFactor, double>
            {
                [RankingFactor.SemanticRelevance] = 0.6,
                [RankingFactor.ExactMatchCount] = 0.6, // Total > 1.0
                [RankingFactor.VehicleCondition] = 0.2
            },
            BusinessRules = new List<BusinessRule>(),
            ApplyDiversity = false
        };

        // Act
        var rankedResults = await _service.RerankResultsAsync(results, strategy, query);

        // Assert
        Assert.NotNull(rankedResults);
        Assert.All(rankedResults, r => Assert.InRange(r.Score, 0.0, 1.0));
    }

    [Fact]
    public async Task RerankResultsAsync_WithBusinessRules_AdjustsScores()
    {
        // Arrange
        var results = new List<VehicleResult>
        {
            CreateVehicleResult("1", "BMW", "3 Series", 150000, 0.8), // High mileage
            CreateVehicleResult("2", "Toyota", "Corolla", 30000, 0.8)
        };
        var query = CreateTestQuery();
        var strategy = new RerankingStrategy
        {
            Approach = RerankingApproach.WeightedScore,
            FactorWeights = new Dictionary<RankingFactor, double>
            {
                [RankingFactor.SemanticRelevance] = 1.0
            },
            BusinessRules = new List<BusinessRule>
            {
                new BusinessRule
                {
                    Name = "Boost Premium Makes",
                    Condition = v => v.Make == "BMW",
                    ScoreAdjustment = 0.1
                },
                new BusinessRule
                {
                    Name = "Penalize High Mileage",
                    Condition = v => v.Mileage > 100000,
                    ScoreAdjustment = -0.15
                }
            },
            ApplyDiversity = false
        };

        // Act
        var rankedResults = await _service.RerankResultsAsync(results, strategy, query);

        // Assert
        var bmw = rankedResults.First(r => r.Vehicle.Make == "BMW");
        var toyota = rankedResults.First(r => r.Vehicle.Make == "Toyota");
        
        // BMW has high mileage which gets penalized (-0.15)
        // BMW also gets premium boost (+0.05)
        // Net effect for BMW: -0.10 adjustment
        // Toyota gets no adjustments (stays at base score)
        // Therefore Toyota should rank higher
        Assert.True(toyota.Score > bmw.Score);
    }

    [Fact]
    public async Task RerankResultsAsync_WithDiversity_LimitsPerMake()
    {
        // Arrange
        var results = new List<VehicleResult>
        {
            CreateVehicleResult("1", "BMW", "3 Series", 50000, 0.9),
            CreateVehicleResult("2", "BMW", "5 Series", 60000, 0.88),
            CreateVehicleResult("3", "BMW", "X5", 70000, 0.86),
            CreateVehicleResult("4", "BMW", "X3", 55000, 0.84), // Should be excluded
            CreateVehicleResult("5", "Audi", "A4", 45000, 0.82),
        };
        var query = CreateTestQuery();
        var strategy = new RerankingStrategy
        {
            Approach = RerankingApproach.WeightedScore,
            FactorWeights = new Dictionary<RankingFactor, double>
            {
                [RankingFactor.SemanticRelevance] = 1.0
            },
            BusinessRules = new List<BusinessRule>(),
            ApplyDiversity = true,
            MaxPerMake = 3,
            MaxPerModel = 2
        };

        // Act
        var rankedResults = await _service.RerankResultsAsync(results, strategy, query);

        // Assert
        var bmwCount = rankedResults.Count(r => r.Vehicle.Make == "BMW");
        Assert.True(bmwCount <= 3, $"Expected max 3 BMW vehicles, got {bmwCount}");
    }

    [Fact]
    public async Task RerankResultsAsync_WithDiversity_LimitsPerModel()
    {
        // Arrange
        var results = new List<VehicleResult>
        {
            CreateVehicleResult("1", "BMW", "3 Series", 50000, 0.9),
            CreateVehicleResult("2", "BMW", "3 Series", 55000, 0.88),
            CreateVehicleResult("3", "BMW", "3 Series", 60000, 0.86), // Should be excluded
            CreateVehicleResult("4", "BMW", "5 Series", 65000, 0.84),
        };
        var query = CreateTestQuery();
        var strategy = new RerankingStrategy
        {
            Approach = RerankingApproach.WeightedScore,
            FactorWeights = new Dictionary<RankingFactor, double>
            {
                [RankingFactor.SemanticRelevance] = 1.0
            },
            BusinessRules = new List<BusinessRule>(),
            ApplyDiversity = true,
            MaxPerMake = 3,
            MaxPerModel = 2
        };

        // Act
        var rankedResults = await _service.RerankResultsAsync(results, strategy, query);

        // Assert
        var bmw3SeriesCount = rankedResults.Count(r => r.Vehicle.Make == "BMW" && r.Vehicle.Model == "3 Series");
        Assert.True(bmw3SeriesCount <= 2, $"Expected max 2 BMW 3 Series vehicles, got {bmw3SeriesCount}");
    }

    [Fact]
    public async Task ComputeBusinessScoreAsync_WithHighConditionVehicle_ReturnsHighScore()
    {
        // Arrange
        var result = CreateVehicleResult(
            "1", "BMW", "3 Series", 30000, 0.8,
            serviceHistory: true,
            numberOfServices: 5,
            motExpiryDate: DateTime.UtcNow.AddMonths(12));

        var query = CreateTestQuery();

        // Act
        var score = await _service.ComputeBusinessScoreAsync(result, query);

        // Assert
        Assert.InRange(score, 0.7, 1.0); // Should be high due to good condition
    }

    [Fact]
    public async Task ComputeBusinessScoreAsync_WithPoorConditionVehicle_ReturnsLowerScore()
    {
        // Arrange
        var result = CreateVehicleResult(
            "1", "BMW", "3 Series", 150000, 0.8,
            serviceHistory: false,
            numberOfServices: 0,
            motExpiryDate: DateTime.UtcNow.AddDays(15)); // Near expiry

        var query = CreateTestQuery();

        // Act
        var score = await _service.ComputeBusinessScoreAsync(result, query);

        // Assert
        Assert.InRange(score, 0.0, 0.7); // Should be lower due to poor condition
    }

    [Fact]
    public async Task RankResultsAsync_WithEmptyResults_ReturnsEmptyList()
    {
        // Arrange
        var results = new List<VehicleResult>();
        var query = CreateTestQuery();

        // Act
        var rankedResults = await _service.RankResultsAsync(results, query);

        // Assert
        Assert.Empty(rankedResults);
    }

    [Fact]
    public async Task RerankResultsAsync_WithNullStrategy_ThrowsArgumentNullException()
    {
        // Arrange
        var results = CreateTestResults();
        var query = CreateTestQuery();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _service.RerankResultsAsync(results, null!, query));
    }

    [Fact]
    public async Task RerankResultsAsync_WithInvalidMaxPerMake_ThrowsArgumentException()
    {
        // Arrange
        var results = CreateTestResults();
        var query = CreateTestQuery();
        var strategy = new RerankingStrategy
        {
            Approach = RerankingApproach.WeightedScore,
            FactorWeights = new Dictionary<RankingFactor, double>
            {
                [RankingFactor.SemanticRelevance] = 1.0
            },
            BusinessRules = new List<BusinessRule>(),
            ApplyDiversity = true,
            MaxPerMake = 0 // Invalid
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.RerankResultsAsync(results, strategy, query));
    }

    [Fact]
    public async Task RerankResultsAsync_WithInvalidMaxPerModel_ThrowsArgumentException()
    {
        // Arrange
        var results = CreateTestResults();
        var query = CreateTestQuery();
        var strategy = new RerankingStrategy
        {
            Approach = RerankingApproach.WeightedScore,
            FactorWeights = new Dictionary<RankingFactor, double>
            {
                [RankingFactor.SemanticRelevance] = 1.0
            },
            BusinessRules = new List<BusinessRule>(),
            ApplyDiversity = true,
            MaxPerMake = 3,
            MaxPerModel = -1 // Invalid
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.RerankResultsAsync(results, strategy, query));
    }

    [Fact]
    public async Task RerankResultsAsync_ClampsScoresToValidRange()
    {
        // Arrange
        var results = CreateTestResults();
        var query = CreateTestQuery();
        var strategy = new RerankingStrategy
        {
            Approach = RerankingApproach.WeightedScore,
            FactorWeights = new Dictionary<RankingFactor, double>
            {
                [RankingFactor.SemanticRelevance] = 1.0
            },
            BusinessRules = new List<BusinessRule>
            {
                new BusinessRule
                {
                    Name = "Huge Boost",
                    Condition = _ => true,
                    ScoreAdjustment = 2.0 // Would push score > 1.0
                }
            },
            ApplyDiversity = false
        };

        // Act
        var rankedResults = await _service.RerankResultsAsync(results, strategy, query);

        // Assert
        Assert.All(rankedResults, r =>
        {
            Assert.InRange(r.Score, 0.0, 1.0);
        });
    }

    // Helper methods
    private List<VehicleResult> CreateTestResults()
    {
        return new List<VehicleResult>
        {
            CreateVehicleResult("1", "BMW", "3 Series", 50000, 0.9),
            CreateVehicleResult("2", "Audi", "A4", 45000, 0.85),
            CreateVehicleResult("3", "Mercedes-Benz", "C-Class", 55000, 0.88),
            CreateVehicleResult("4", "Toyota", "Corolla", 30000, 0.82),
            CreateVehicleResult("5", "Honda", "Civic", 35000, 0.80),
        };
    }

    private VehicleResult CreateVehicleResult(
        string id,
        string make,
        string model,
        int mileage,
        double score,
        bool serviceHistory = true,
        int numberOfServices = 3,
        DateTime? motExpiryDate = null)
    {
        return new VehicleResult
        {
            Vehicle = new Vehicle
            {
                Id = id,
                Make = make,
                Model = model,
                Derivative = "Test Derivative",
                Price = 20000m,
                Mileage = mileage,
                BodyType = "Saloon",
                EngineSize = 2.0m,
                FuelType = "Petrol",
                TransmissionType = "Automatic",
                Colour = "Black",
                NumberOfDoors = 4,
                RegistrationDate = DateTime.UtcNow.AddYears(-3),
                ServiceHistoryPresent = serviceHistory,
                NumberOfServices = numberOfServices,
                MotExpiryDate = motExpiryDate ?? DateTime.UtcNow.AddMonths(6),
                Declarations = new List<string>()
            },
            Score = score,
            ScoreBreakdown = new SearchScoreBreakdown
            {
                SemanticScore = score,
                ExactMatchScore = 0.8,
                KeywordScore = 0.7,
                FinalScore = score
            }
        };
    }

    private ComposedQuery CreateTestQuery()
    {
        return new ComposedQuery
        {
            Type = QueryType.Filtered,
            ConstraintGroups = new List<ConstraintGroup>
            {
                new ConstraintGroup
                {
                    Operator = LogicalOperator.And,
                    Constraints = new List<SearchConstraint>
                    {
                        new SearchConstraint
                        {
                            FieldName = "BodyType",
                            Operator = ConstraintOperator.Equals,
                            Value = "Saloon",
                            Type = ConstraintType.Exact
                        }
                    }
                }
            }
        };
    }
}
