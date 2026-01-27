using FluentAssertions;
using VehicleSearch.Core.Models;
using VehicleSearch.Infrastructure.AI;

namespace VehicleSearch.Infrastructure.Tests;

public class PatternMatcherTests
{
    [Theory]
    [InlineData("£20,000", 20000)]
    [InlineData("£20k", 20000)]
    [InlineData("20000", 20000)]
    [InlineData("£15,000", 15000)]
    [InlineData("30k", 30000)]
    public void ExtractPrices_SinglePrice_ExtractsCorrectly(string input, int expectedPrice)
    {
        // Arrange
        var query = $"Show me cars under {input}";

        // Act
        var entities = PatternMatcher.ExtractPrices(query);

        // Assert
        entities.Should().ContainSingle();
        var entity = entities.First();
        entity.Type.Should().Be(EntityType.Price);
        entity.Value.Should().Be(expectedPrice.ToString());
        entity.Confidence.Should().BeGreaterThan(0.8);
    }

    [Fact]
    public void ExtractPrices_PriceRange_ExtractsPriceRange()
    {
        // Arrange
        var query = "Show me cars between £15,000 and £25,000";

        // Act
        var entities = PatternMatcher.ExtractPrices(query);

        // Assert
        entities.Should().ContainSingle();
        var entity = entities.First();
        entity.Type.Should().Be(EntityType.PriceRange);
        entity.Value.Should().Be("15000-25000");
        entity.Confidence.Should().Be(1.0);
    }

    [Fact]
    public void ExtractPrices_PriceRangeWithK_ExtractsPriceRange()
    {
        // Arrange
        var query = "Show me cars £15k-£25k";

        // Act
        var entities = PatternMatcher.ExtractPrices(query);

        // Assert
        entities.Should().ContainSingle();
        var entity = entities.First();
        entity.Type.Should().Be(EntityType.PriceRange);
        entity.Value.Should().Be("15000-25000");
    }

    [Fact]
    public void ExtractPrices_UnderPrice_ExtractsAsPrice()
    {
        // Arrange
        var query = "Show me cars under £20,000";

        // Act
        var entities = PatternMatcher.ExtractPrices(query);

        // Assert
        entities.Should().ContainSingle();
        var entity = entities.First();
        entity.Type.Should().Be(EntityType.Price);
        entity.Value.Should().Be("20000");
    }

    [Fact]
    public void ExtractPrices_SmallNumber_DoesNotExtractAsPrice()
    {
        // Arrange
        var query = "Show me BMW 3 series";

        // Act
        var entities = PatternMatcher.ExtractPrices(query);

        // Assert
        entities.Should().BeEmpty();
    }

    [Theory]
    [InlineData("under 50,000 miles", 50000)]  // Qualifier pattern works well
    [InlineData("under 50k miles", 50000)]
    [InlineData("less than 30000 miles", 30000)]
    public void ExtractMileage_WithQualifier_ExtractsCorrectly(string input, int expectedMileage)
    {
        // Arrange
        var query = $"Show me cars {input}";

        // Act
        var entities = PatternMatcher.ExtractMileage(query);

        // Assert
        entities.Should().ContainSingle();
        var entity = entities.First();
        entity.Type.Should().Be(EntityType.Mileage);
        entity.Value.Should().Be(expectedMileage.ToString());
        entity.Confidence.Should().BeGreaterThan(0.7);
    }

    [Fact]
    public void ExtractMileage_LowMileage_ReturnsDefault()
    {
        // Arrange
        var query = "Show me cars with low mileage";

        // Act
        var entities = PatternMatcher.ExtractMileage(query);

        // Assert
        entities.Should().ContainSingle();
        var entity = entities.First();
        entity.Type.Should().Be(EntityType.Mileage);
        entity.Value.Should().Be("30000");
        entity.Confidence.Should().Be(0.7);
    }

    [Fact]
    public void ExtractMileage_UnderMileage_ExtractsCorrectly()
    {
        // Arrange
        var query = "Show me cars under 50k miles";

        // Act
        var entities = PatternMatcher.ExtractMileage(query);

        // Assert
        entities.Should().ContainSingle();
        var entity = entities.First();
        entity.Type.Should().Be(EntityType.Mileage);
        entity.Value.Should().Be("50000");
        entity.Confidence.Should().Be(0.95);
    }

    [Fact]
    public void ExtractMileage_WithPrice_DoesNotConfusePriceWithMileage()
    {
        // Arrange
        var query = "Show me cars under £20,000";

        // Act
        var entities = PatternMatcher.ExtractMileage(query);

        // Assert
        entities.Should().BeEmpty();
    }

    [Theory]
    [InlineData("2020", "2020")]
    [InlineData("2015", "2015")]
    [InlineData("2023", "2023")]
    [InlineData("1995", "1995")]
    public void ExtractYears_ValidYear_ExtractsCorrectly(string input, string expectedYear)
    {
        // Arrange
        var query = $"Show me {input} cars";

        // Act
        var entities = PatternMatcher.ExtractYears(query);

        // Assert
        entities.Should().ContainSingle();
        var entity = entities.First();
        entity.Type.Should().Be(EntityType.Year);
        entity.Value.Should().Be(expectedYear);
        entity.Confidence.Should().Be(0.95);
    }

    [Fact]
    public void ExtractYears_InvalidYear_DoesNotExtract()
    {
        // Arrange
        var query = "Show me 3000 cars"; // Invalid year

        // Act
        var entities = PatternMatcher.ExtractYears(query);

        // Assert
        entities.Should().BeEmpty();
    }

    [Theory]
    [InlineData("beamer", "BMW", 4)]  // beamer->BMW requires substantial changes
    [InlineData("auddi", "Audi", 1)]  // just one character different
    [InlineData("BMW", "BMW", 0)]      // exact match (case-insensitive)
    [InlineData("bmw", "BMW", 0)]      // case doesn't matter
    [InlineData("Audi", "Audi", 0)]
    public void LevenshteinDistance_CalculatesCorrectly(string source, string target, int expectedDistance)
    {
        // Act
        var distance = PatternMatcher.LevenshteinDistance(source, target);

        // Assert
        distance.Should().Be(expectedDistance);
    }

    [Fact]
    public void ExtractPricesAndMileage_ComplexQuery_ExtractsBoth()
    {
        // Arrange
        var query = "Show me BMW under £20k with under 50k miles";

        // Act
        var prices = PatternMatcher.ExtractPrices(query);
        var mileage = PatternMatcher.ExtractMileage(query);

        // Assert
        prices.Should().NotBeEmpty();
        prices.First().Value.Should().Be("20000");

        mileage.Should().NotBeEmpty();
        mileage.First().Value.Should().Be("50000");
    }
}
