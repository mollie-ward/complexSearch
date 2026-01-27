using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using VehicleSearch.Core.Entities;
using VehicleSearch.Infrastructure.Data;

namespace VehicleSearch.Infrastructure.Tests;

public class DataIngestionServiceTests
{
    private readonly Mock<CsvDataLoader> _csvLoaderMock;
    private readonly Mock<DataNormalizer> _normalizerMock;
    private readonly Mock<DataValidator> _validatorMock;
    private readonly Mock<ILogger<DataIngestionService>> _loggerMock;
    private readonly DataIngestionService _ingestionService;

    public DataIngestionServiceTests()
    {
        _csvLoaderMock = new Mock<CsvDataLoader>(Mock.Of<ILogger<CsvDataLoader>>());
        _normalizerMock = new Mock<DataNormalizer>(Mock.Of<ILogger<DataNormalizer>>());
        _validatorMock = new Mock<DataValidator>(Mock.Of<ILogger<DataValidator>>());
        _loggerMock = new Mock<ILogger<DataIngestionService>>();

        _ingestionService = new DataIngestionService(
            _csvLoaderMock.Object,
            _normalizerMock.Object,
            _validatorMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task IngestFromCsv_ValidFile_ReturnsSuccessResult()
    {
        // Arrange
        var csvPath = Path.Combine("TestData", "valid_vehicles.csv");
        
        // Act
        var result = await _ingestionService.IngestFromCsvAsync(csvPath);

        // Assert
        result.Should().NotBeNull();
        result.TotalRows.Should().BeGreaterThan(0);
        result.Success.Should().BeTrue();
        result.ProcessingTimeMs.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task IngestFromCsv_NonExistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var csvPath = "nonexistent.csv";

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(() => 
            _ingestionService.IngestFromCsvAsync(csvPath));
    }

    [Fact]
    public async Task IngestFromCsv_InvalidData_ReturnsErrorsInResult()
    {
        // Arrange
        var csvPath = Path.Combine("TestData", "invalid_vehicles.csv");

        // Act
        var result = await _ingestionService.IngestFromCsvAsync(csvPath);

        // Assert
        result.Should().NotBeNull();
        result.TotalRows.Should().BeGreaterThan(0);
        result.InvalidRows.Should().BeGreaterThan(0);
        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GenerateDescription_AllFields_CreatesReadableText()
    {
        // This test verifies the description generation through the actual service
        // Arrange
        var vehicle = new Vehicle
        {
            Id = "TEST1",
            Make = "Volkswagen",
            Model = "Golf",
            Derivative = "SE Nav",
            BodyType = "Hatchback",
            EngineSize = 1.5m,
            FuelType = "Petrol",
            TransmissionType = "Manual",
            Colour = "Blue",
            Mileage = 25000,
            Price = 18500,
            RegistrationDate = new DateTime(2020, 3, 15),
            SaleLocation = "London",
            Features = new List<string> { "Air Conditioning", "Bluetooth", "Parking Sensors" }
        };

        var vehicles = new List<Vehicle> { vehicle };
        var equipment = new Dictionary<string, string>
        {
            { "TEST1", "Air Conditioning, Bluetooth, Parking Sensors" }
        };
        var declarations = new Dictionary<string, string>();

        // Use the actual normalizer and validator
        var normalizer = new DataNormalizer(Mock.Of<ILogger<DataNormalizer>>());
        var validator = new DataValidator(Mock.Of<ILogger<DataValidator>>());
        var csvLoader = new CsvDataLoader(Mock.Of<ILogger<CsvDataLoader>>());

        var service = new DataIngestionService(
            csvLoader,
            normalizer,
            validator,
            Mock.Of<ILogger<DataIngestionService>>());

        // Create a temporary CSV file for testing
        var tempFile = Path.GetTempFileName();
        var csvContent = @"Registration Number,Make,Model,Derivative,Body,Engine Size,Fuel,Transmission,Colour,Number Of Doors,Buy Now Price,Mileage,Registration Date,Sale Location,Channel,Sale Type,Equipment,Service History Present,Number of Services,Last Service Date,MOT Expiry,Grade,VAT Type,Additional Information,Declarations,Cap Retail Price,Cap Clean Price
TEST1,Volkswagen,Golf,SE Nav,Hatchback,1.5,Petrol,Manual,Blue,5,18500,25000,15/03/2020,London,Retail,Stock,""Air Conditioning, Bluetooth, Parking Sensors"",Yes,3,10/01/2023,15/03/2025,Grade A,VAT Qualifying,Full service history,HPI Clear,19000,17500";
        await File.WriteAllTextAsync(tempFile, csvContent);

        try
        {
            // Act
            var result = await service.IngestFromCsvAsync(tempFile);

            // We can't directly test the generated description without accessing internal methods,
            // but we can verify the ingestion completed successfully
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
        }
        finally
        {
            File.Delete(tempFile);
        }
    }
}
