using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using VehicleSearch.Core.Entities;
using VehicleSearch.Infrastructure.Data;

namespace VehicleSearch.Infrastructure.Tests;

public class DataNormalizerTests
{
    private readonly Mock<ILogger<DataNormalizer>> _loggerMock;
    private readonly DataNormalizer _normalizer;

    public DataNormalizerTests()
    {
        _loggerMock = new Mock<ILogger<DataNormalizer>>();
        _normalizer = new DataNormalizer(_loggerMock.Object);
    }

    [Fact]
    public void Normalize_MakeAndModel_ConvertsToTitleCase()
    {
        // Arrange
        var vehicles = new List<Vehicle>
        {
            new() { Id = "TEST1", Make = "VOLKSWAGEN", Model = "GOLF" }
        };
        var equipment = new Dictionary<string, string>();
        var declarations = new Dictionary<string, string>();

        // Act
        var result = _normalizer.Normalize(vehicles, equipment, declarations).ToList();

        // Assert
        result[0].Make.Should().Be("Volkswagen");
        result[0].Model.Should().Be("Golf");
    }

    [Fact]
    public void Normalize_Colour_ConvertsToTitleCase()
    {
        // Arrange
        var vehicles = new List<Vehicle>
        {
            new() { Id = "TEST1", Colour = "GREY" },
            new() { Id = "TEST2", Colour = "BLACK" },
            new() { Id = "TEST3", Colour = "SILVER" }
        };
        var equipment = new Dictionary<string, string>();
        var declarations = new Dictionary<string, string>();

        // Act
        var result = _normalizer.Normalize(vehicles, equipment, declarations).ToList();

        // Assert
        result[0].Colour.Should().Be("Grey");
        result[1].Colour.Should().Be("Black");
        result[2].Colour.Should().Be("Silver");
    }

    [Fact]
    public void Normalize_Equipment_SplitsIntoFeatures()
    {
        // Arrange
        var vehicles = new List<Vehicle>
        {
            new() { Id = "TEST1" }
        };
        var equipment = new Dictionary<string, string>
        {
            { "TEST1", "Air Conditioning, Bluetooth, Parking Sensors" }
        };
        var declarations = new Dictionary<string, string>();

        // Act
        var result = _normalizer.Normalize(vehicles, equipment, declarations).ToList();

        // Assert
        result[0].Features.Should().HaveCount(3);
        result[0].Features.Should().Contain("Air Conditioning");
        result[0].Features.Should().Contain("Bluetooth");
        result[0].Features.Should().Contain("Parking Sensors");
    }

    [Fact]
    public void Normalize_Declarations_SplitsIntoList()
    {
        // Arrange
        var vehicles = new List<Vehicle>
        {
            new() { Id = "TEST1" }
        };
        var equipment = new Dictionary<string, string>();
        var declarations = new Dictionary<string, string>
        {
            { "TEST1", "HPI Clear, Full Service History" }
        };

        // Act
        var result = _normalizer.Normalize(vehicles, equipment, declarations).ToList();

        // Assert
        result[0].Declarations.Should().HaveCount(2);
        result[0].Declarations.Should().Contain("HPI Clear");
        result[0].Declarations.Should().Contain("Full Service History");
    }

    [Fact]
    public void Normalize_EmptyEquipment_ReturnsEmptyFeatures()
    {
        // Arrange
        var vehicles = new List<Vehicle>
        {
            new() { Id = "TEST1" }
        };
        var equipment = new Dictionary<string, string>
        {
            { "TEST1", "" }
        };
        var declarations = new Dictionary<string, string>();

        // Act
        var result = _normalizer.Normalize(vehicles, equipment, declarations).ToList();

        // Assert
        result[0].Features.Should().BeEmpty();
    }

    [Fact]
    public void Normalize_TrimsWhitespace_FromAllFields()
    {
        // Arrange
        var vehicles = new List<Vehicle>
        {
            new() 
            { 
                Id = "  TEST1  ", 
                Make = " Ford ", 
                Model = " Focus ",
                SaleLocation = "  London  "
            }
        };
        var equipment = new Dictionary<string, string>();
        var declarations = new Dictionary<string, string>();

        // Act
        var result = _normalizer.Normalize(vehicles, equipment, declarations).ToList();

        // Assert
        result[0].Id.Should().Be("TEST1");
        result[0].Make.Should().Be("Ford");
        result[0].Model.Should().Be("Focus");
        result[0].SaleLocation.Should().Be("London");
    }

    [Fact]
    public void Normalize_SetsProcessedDate()
    {
        // Arrange
        var vehicles = new List<Vehicle>
        {
            new() { Id = "TEST1" }
        };
        var equipment = new Dictionary<string, string>();
        var declarations = new Dictionary<string, string>();
        var beforeNormalize = DateTime.UtcNow;

        // Act
        var result = _normalizer.Normalize(vehicles, equipment, declarations).ToList();

        // Assert
        result[0].ProcessedDate.Should().BeAfter(beforeNormalize.AddSeconds(-1));
        result[0].ProcessedDate.Should().BeBefore(DateTime.UtcNow.AddSeconds(1));
    }
}
