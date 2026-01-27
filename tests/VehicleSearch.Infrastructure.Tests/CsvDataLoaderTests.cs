using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using VehicleSearch.Infrastructure.Data;

namespace VehicleSearch.Infrastructure.Tests;

public class CsvDataLoaderTests
{
    private readonly Mock<ILogger<CsvDataLoader>> _loggerMock;
    private readonly CsvDataLoader _csvLoader;

    public CsvDataLoaderTests()
    {
        _loggerMock = new Mock<ILogger<CsvDataLoader>>();
        _csvLoader = new CsvDataLoader(_loggerMock.Object);
    }

    [Fact]
    public async Task LoadFromStreamAsync_ValidFile_ReturnsAllVehicles()
    {
        // Arrange
        var csvPath = Path.Combine("TestData", "valid_vehicles.csv");
        using var fileStream = File.OpenRead(csvPath);

        // Act
        var result = await _csvLoader.LoadFromStreamAsync(fileStream);
        var vehicleList = result.Vehicles.ToList();

        // Assert
        vehicleList.Should().HaveCount(3);
        
        var firstVehicle = vehicleList[0];
        firstVehicle.Id.Should().Be("ABC123");
        firstVehicle.Make.Should().Be("VOLKSWAGEN");
        firstVehicle.Model.Should().Be("GOLF");
        firstVehicle.Price.Should().Be(18500);
        firstVehicle.Mileage.Should().Be(25000);

        result.Equipment.Should().ContainKey("ABC123");
        result.Equipment["ABC123"].Should().Contain("Air Conditioning");
    }

    [Fact]
    public async Task LoadFromStreamAsync_InvalidPrice_LoadsRow()
    {
        // Arrange
        var csvPath = Path.Combine("TestData", "invalid_vehicles.csv");
        using var fileStream = File.OpenRead(csvPath);

        // Act
        var result = await _csvLoader.LoadFromStreamAsync(fileStream);
        var vehicleList = result.Vehicles.ToList();

        // Assert - CSV loader should load all rows, validation happens later
        vehicleList.Should().HaveCount(3);
    }

    [Fact]
    public async Task LoadFromStreamAsync_EmptyStream_ReturnsEmptyCollection()
    {
        // Arrange
        var csvContent = "Registration Number,Make,Model,Derivative,Body,Engine Size,Fuel,Transmission,Colour,Number Of Doors,Buy Now Price,Mileage,Registration Date,Sale Location,Channel,Sale Type,Equipment,Service History Present,Number of Services,Last Service Date,MOT Expiry,Grade,VAT Type,Additional Information,Declarations,Cap Retail Price,Cap Clean Price\n";
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csvContent));

        // Act
        var result = await _csvLoader.LoadFromStreamAsync(stream);

        // Assert
        result.Vehicles.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadFromStreamAsync_HandlesQuotedFieldsWithCommas()
    {
        // Arrange
        var csvContent = @"Registration Number,Make,Model,Equipment
TEST123,Ford,Focus,""Air Conditioning, Bluetooth, Parking Sensors""";
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csvContent));

        // Act
        var result = await _csvLoader.LoadFromStreamAsync(stream);
        var vehicleList = result.Vehicles.ToList();

        // Assert
        vehicleList.Should().HaveCount(1);
        result.Equipment["TEST123"].Should().Contain("Air Conditioning, Bluetooth, Parking Sensors");
    }
}
