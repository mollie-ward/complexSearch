using System.Diagnostics;
using System.Globalization;
using Microsoft.Extensions.Logging;
using VehicleSearch.Core.Entities;
using VehicleSearch.Core.Interfaces;
using VehicleSearch.Core.Models;

namespace VehicleSearch.Infrastructure.Data;

/// <summary>
/// Service for ingesting vehicle data from various sources.
/// </summary>
public class DataIngestionService : IDataIngestionService
{
    private readonly CsvDataLoader _csvLoader;
    private readonly DataNormalizer _normalizer;
    private readonly DataValidator _validator;
    private readonly ILogger<DataIngestionService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataIngestionService"/> class.
    /// </summary>
    public DataIngestionService(
        CsvDataLoader csvLoader,
        DataNormalizer normalizer,
        DataValidator validator,
        ILogger<DataIngestionService> logger)
    {
        _csvLoader = csvLoader;
        _normalizer = normalizer;
        _validator = validator;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<IngestionResult> IngestFromCsvAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new IngestionResult
        {
            CompletedAt = DateTime.UtcNow
        };

        try
        {
            _logger.LogInformation("Starting CSV ingestion from {FilePath}", filePath);

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"CSV file not found: {filePath}");
            }

            // Load vehicles from CSV
            IEnumerable<Vehicle> vehicles;
            Dictionary<string, string> rawEquipment;
            Dictionary<string, string> rawDeclarations;

            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true))
            {
                var loadResult = await _csvLoader.LoadFromStreamAsync(fileStream, cancellationToken);
                vehicles = loadResult.Vehicles;
                rawEquipment = loadResult.Equipment;
                rawDeclarations = loadResult.Declarations;
            }

            result.TotalRows = vehicles.Count();

            if (result.TotalRows == 0)
            {
                _logger.LogWarning("No vehicles found in CSV file");
                stopwatch.Stop();
                result.ProcessingTimeMs = stopwatch.ElapsedMilliseconds;
                return result;
            }

            // Normalize data
            vehicles = _normalizer.Normalize(vehicles, rawEquipment, rawDeclarations);

            // Generate descriptions
            foreach (var vehicle in vehicles)
            {
                vehicle.Description = GenerateDescription(vehicle);
            }

            // Validate data
            var validationResult = await ValidateDataAsync(vehicles);
            result.Errors = validationResult.Errors;

            // Count valid and invalid rows
            var invalidVehicleIds = validationResult.Errors
                .Select(e => e.RowNumber)
                .Distinct()
                .ToHashSet();

            result.InvalidRows = invalidVehicleIds.Count;
            result.ValidRows = result.TotalRows - result.InvalidRows;

            stopwatch.Stop();
            result.ProcessingTimeMs = stopwatch.ElapsedMilliseconds;

            _logger.LogInformation(
                "CSV ingestion completed. Total: {Total}, Valid: {Valid}, Invalid: {Invalid}, Time: {Time}ms",
                result.TotalRows, result.ValidRows, result.InvalidRows, result.ProcessingTimeMs);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during CSV ingestion");
            stopwatch.Stop();
            result.ProcessingTimeMs = stopwatch.ElapsedMilliseconds;
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Vehicle>> ParseCsvAsync(Stream csvStream, CancellationToken cancellationToken = default)
    {
        var loadResult = await _csvLoader.LoadFromStreamAsync(csvStream, cancellationToken);
        return _normalizer.Normalize(loadResult.Vehicles, loadResult.Equipment, loadResult.Declarations);
    }

    /// <inheritdoc/>
    public Task<ValidationResult> ValidateDataAsync(IEnumerable<Vehicle> vehicles)
    {
        var result = _validator.Validate(vehicles);
        return Task.FromResult(result);
    }

    /// <summary>
    /// Generates a description for a vehicle for semantic search.
    /// </summary>
    private string GenerateDescription(Vehicle vehicle)
    {
        var parts = new List<string>();

        // Basic vehicle info
        if (!string.IsNullOrWhiteSpace(vehicle.Make) && !string.IsNullOrWhiteSpace(vehicle.Model))
        {
            var basicInfo = $"{vehicle.Make} {vehicle.Model}";
            if (!string.IsNullOrWhiteSpace(vehicle.Derivative))
            {
                basicInfo += $" {vehicle.Derivative}";
            }
            parts.Add(basicInfo);
        }

        // Engine and transmission
        var engineInfo = new List<string>();
        if (vehicle.EngineSize > 0)
        {
            engineInfo.Add($"{vehicle.EngineSize:F1}L");
        }
        if (!string.IsNullOrWhiteSpace(vehicle.FuelType))
        {
            engineInfo.Add(vehicle.FuelType);
        }
        if (!string.IsNullOrWhiteSpace(vehicle.TransmissionType))
        {
            engineInfo.Add(vehicle.TransmissionType);
        }
        if (engineInfo.Any())
        {
            parts.Add(string.Join(" ", engineInfo));
        }

        // Body type and colour
        if (!string.IsNullOrWhiteSpace(vehicle.BodyType))
        {
            parts.Add(vehicle.BodyType);
        }
        if (!string.IsNullOrWhiteSpace(vehicle.Colour))
        {
            parts.Add(vehicle.Colour);
        }

        // Mileage and price
        if (vehicle.Mileage > 0)
        {
            parts.Add($"{vehicle.Mileage:N0} miles");
        }
        if (vehicle.Price > 0)
        {
            parts.Add($"£{vehicle.Price:N0}");
        }

        // Registration date
        if (vehicle.RegistrationDate.HasValue)
        {
            parts.Add($"registered {vehicle.RegistrationDate.Value:MMM yyyy}");
        }

        // Location
        if (!string.IsNullOrWhiteSpace(vehicle.SaleLocation))
        {
            parts.Add(vehicle.SaleLocation);
        }

        // Features
        if (vehicle.Features.Any())
        {
            var featuresStr = string.Join(", ", vehicle.Features.Take(5)); // Limit to first 5 features
            parts.Add($"Features: {featuresStr}");
        }

        return string.Join(", ", parts);
    }
}
