using Microsoft.Extensions.Logging;
using Moq;
using VehicleSearch.Core.Entities;
using VehicleSearch.Core.Models;
using VehicleSearch.Infrastructure.Search;
using Xunit;

namespace VehicleSearch.Infrastructure.Tests;

/// <summary>
/// Unit tests for RRFMerger.
/// </summary>
public class RRFMergerTests
{
    private readonly Mock<ILogger<RRFMerger>> _mockLogger;
    private readonly RRFMerger _merger;

    public RRFMergerTests()
    {
        _mockLogger = new Mock<ILogger<RRFMerger>>();
        _merger = new RRFMerger(_mockLogger.Object);
    }

    [Fact]
    public void MergeWithRRF_WithTwoLists_MergesCorrectly()
    {
        // Arrange
        var list1 = new List<VehicleResult>
        {
            CreateVehicleResult("1", "BMW", 0.9),
            CreateVehicleResult("2", "Audi", 0.8),
            CreateVehicleResult("3", "Mercedes", 0.7),
        };

        var list2 = new List<VehicleResult>
        {
            CreateVehicleResult("2", "Audi", 0.85), // Duplicate
            CreateVehicleResult("4", "Toyota", 0.75),
            CreateVehicleResult("1", "BMW", 0.7), // Duplicate
        };

        // Act
        var merged = _merger.MergeWithRRF(list1, list2);

        // Assert
        Assert.Equal(4, merged.Count); // Should have 4 unique vehicles
        Assert.Contains(merged, r => r.Vehicle.Id == "1");
        Assert.Contains(merged, r => r.Vehicle.Id == "2");
        Assert.Contains(merged, r => r.Vehicle.Id == "3");
        Assert.Contains(merged, r => r.Vehicle.Id == "4");
    }

    [Fact]
    public void MergeWithRRF_OrdersByRRFScore_DescendingOrder()
    {
        // Arrange
        var list1 = new List<VehicleResult>
        {
            CreateVehicleResult("1", "BMW", 0.9),
            CreateVehicleResult("2", "Audi", 0.8),
        };

        var list2 = new List<VehicleResult>
        {
            CreateVehicleResult("2", "Audi", 0.9), // Same vehicle, higher rank in list2
            CreateVehicleResult("3", "Mercedes", 0.7),
        };

        // Act
        var merged = _merger.MergeWithRRF(list1, list2);

        // Assert
        for (int i = 0; i < merged.Count - 1; i++)
        {
            Assert.True(merged[i].Score >= merged[i + 1].Score,
                $"Results not properly ordered by RRF score");
        }
    }

    [Fact]
    public void MergeWithRRF_WithDifferentWeights_ProducesValidScores()
    {
        // Arrange
        var list1 = new List<VehicleResult>
        {
            CreateVehicleResult("1", "BMW", 0.9),
            CreateVehicleResult("2", "Audi", 0.8),
        };

        var list2 = new List<VehicleResult>
        {
            CreateVehicleResult("3", "Mercedes", 0.9),
            CreateVehicleResult("4", "Toyota", 0.7),
        };

        // Act - Different weights should still produce valid results
        var mergedHighWeight1 = _merger.MergeWithRRF(list1, list2, weight1: 0.8, weight2: 0.2);
        var mergedHighWeight2 = _merger.MergeWithRRF(list1, list2, weight1: 0.2, weight2: 0.8);

        // Assert
        Assert.Equal(4, mergedHighWeight1.Count);
        Assert.Equal(4, mergedHighWeight2.Count);
        Assert.All(mergedHighWeight1, r => Assert.True(r.Score > 0));
        Assert.All(mergedHighWeight2, r => Assert.True(r.Score > 0));
    }

    [Fact]
    public void MergeWithRRF_WithCustomK_ProducesValidResults()
    {
        // Arrange
        var list1 = new List<VehicleResult>
        {
            CreateVehicleResult("1", "BMW", 0.9),
            CreateVehicleResult("2", "Audi", 0.8),
        };

        var list2 = new List<VehicleResult>
        {
            CreateVehicleResult("3", "Mercedes", 0.9),
            CreateVehicleResult("4", "Toyota", 0.7),
        };

        // Act
        var mergedK60 = _merger.MergeWithRRF(list1, list2, k: 60);
        var mergedK10 = _merger.MergeWithRRF(list1, list2, k: 10);

        // Assert
        Assert.Equal(4, mergedK60.Count);
        Assert.Equal(4, mergedK10.Count);
        Assert.All(mergedK60, r => Assert.True(r.Score > 0));
        Assert.All(mergedK10, r => Assert.True(r.Score > 0));
    }

    [Fact]
    public void MergeWithRRF_DeduplicatesVehicles()
    {
        // Arrange
        var list1 = new List<VehicleResult>
        {
            CreateVehicleResult("1", "BMW", 0.9),
            CreateVehicleResult("2", "Audi", 0.8),
        };

        var list2 = new List<VehicleResult>
        {
            CreateVehicleResult("1", "BMW", 0.85),
            CreateVehicleResult("2", "Audi", 0.75),
        };

        // Act
        var merged = _merger.MergeWithRRF(list1, list2);

        // Assert
        Assert.Equal(2, merged.Count);
        Assert.Single(merged, r => r.Vehicle.Id == "1");
        Assert.Single(merged, r => r.Vehicle.Id == "2");
    }

    [Fact]
    public void MergeWithRRF_WithEmptyList1_ReturnsOnlyList2()
    {
        // Arrange
        var list1 = new List<VehicleResult>();
        var list2 = new List<VehicleResult>
        {
            CreateVehicleResult("1", "BMW", 0.9),
            CreateVehicleResult("2", "Audi", 0.8),
        };

        // Act
        var merged = _merger.MergeWithRRF(list1, list2);

        // Assert
        Assert.Equal(2, merged.Count);
    }

    [Fact]
    public void MergeWithRRF_WithBothEmpty_ReturnsEmpty()
    {
        // Arrange
        var list1 = new List<VehicleResult>();
        var list2 = new List<VehicleResult>();

        // Act
        var merged = _merger.MergeWithRRF(list1, list2);

        // Assert
        Assert.Empty(merged);
    }

    [Fact]
    public void MergeMultipleWithRRF_WithThreeLists_MergesCorrectly()
    {
        // Arrange
        var lists = new List<(List<VehicleResult> Results, double Weight)>
        {
            (new List<VehicleResult>
            {
                CreateVehicleResult("1", "BMW", 0.9),
                CreateVehicleResult("2", "Audi", 0.8),
            }, 0.5),
            (new List<VehicleResult>
            {
                CreateVehicleResult("2", "Audi", 0.9),
                CreateVehicleResult("3", "Mercedes", 0.7),
            }, 0.3),
            (new List<VehicleResult>
            {
                CreateVehicleResult("4", "Toyota", 0.85),
                CreateVehicleResult("1", "BMW", 0.8),
            }, 0.2)
        };

        // Act
        var merged = _merger.MergeMultipleWithRRF(lists);

        // Assert
        Assert.Equal(4, merged.Count); // 4 unique vehicles
        Assert.All(merged, r => Assert.True(r.Score > 0));
    }

    [Fact]
    public void MergeMultipleWithRRF_NormalizesWeights()
    {
        // Arrange
        var lists = new List<(List<VehicleResult> Results, double Weight)>
        {
            (new List<VehicleResult>
            {
                CreateVehicleResult("1", "BMW", 0.9),
            }, 2.0), // Non-normalized weight
            (new List<VehicleResult>
            {
                CreateVehicleResult("2", "Audi", 0.8),
            }, 3.0)  // Non-normalized weight
        };

        // Act
        var merged = _merger.MergeMultipleWithRRF(lists);

        // Assert
        Assert.NotEmpty(merged);
        Assert.All(merged, r => Assert.InRange(r.Score, 0.0, 1.0));
    }

    [Fact]
    public void MergeMultipleWithRRF_WithEmptyLists_ReturnsEmpty()
    {
        // Arrange
        var lists = new List<(List<VehicleResult> Results, double Weight)>();

        // Act
        var merged = _merger.MergeMultipleWithRRF(lists);

        // Assert
        Assert.Empty(merged);
    }

    [Fact]
    public void MergeWithRRF_WithNullList1_ThrowsArgumentNullException()
    {
        // Arrange
        var list2 = new List<VehicleResult>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _merger.MergeWithRRF(null!, list2));
    }

    [Fact]
    public void MergeWithRRF_WithNullList2_ThrowsArgumentNullException()
    {
        // Arrange
        var list1 = new List<VehicleResult>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _merger.MergeWithRRF(list1, null!));
    }

    // Helper method
    private VehicleResult CreateVehicleResult(string id, string make, double score)
    {
        return new VehicleResult
        {
            Vehicle = new Vehicle
            {
                Id = id,
                Make = make,
                Model = "Test Model",
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
