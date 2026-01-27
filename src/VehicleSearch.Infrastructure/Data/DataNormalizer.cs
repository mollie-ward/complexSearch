using System.Globalization;
using Microsoft.Extensions.Logging;
using VehicleSearch.Core.Entities;

namespace VehicleSearch.Infrastructure.Data;

/// <summary>
/// Service for normalizing vehicle data.
/// </summary>
public class DataNormalizer
{
    private readonly ILogger<DataNormalizer> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataNormalizer"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public DataNormalizer(ILogger<DataNormalizer> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Normalizes a collection of vehicles.
    /// </summary>
    /// <param name="vehicles">The vehicles to normalize.</param>
    /// <param name="rawEquipment">Dictionary mapping registration numbers to raw equipment strings.</param>
    /// <param name="rawDeclarations">Dictionary mapping registration numbers to raw declarations strings.</param>
    /// <returns>The normalized vehicles.</returns>
    public IEnumerable<Vehicle> Normalize(IEnumerable<Vehicle> vehicles, 
        Dictionary<string, string> rawEquipment, 
        Dictionary<string, string> rawDeclarations)
    {
        var normalizedVehicles = new List<Vehicle>();

        foreach (var vehicle in vehicles)
        {
            try
            {
                NormalizeVehicle(vehicle, rawEquipment, rawDeclarations);
                normalizedVehicles.Add(vehicle);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error normalizing vehicle {VehicleId}", vehicle.Id);
            }
        }

        return normalizedVehicles;
    }

    private void NormalizeVehicle(Vehicle vehicle, 
        Dictionary<string, string> rawEquipment, 
        Dictionary<string, string> rawDeclarations)
    {
        // Trim all string fields
        vehicle.Id = vehicle.Id?.Trim() ?? string.Empty;
        vehicle.Make = NormalizeToTitleCase(vehicle.Make);
        vehicle.Model = NormalizeToTitleCase(vehicle.Model);
        vehicle.Derivative = vehicle.Derivative?.Trim() ?? string.Empty;
        vehicle.BodyType = NormalizeToTitleCase(vehicle.BodyType);
        vehicle.FuelType = vehicle.FuelType?.Trim() ?? string.Empty;
        vehicle.TransmissionType = vehicle.TransmissionType?.Trim() ?? string.Empty;
        vehicle.Colour = NormalizeColour(vehicle.Colour);
        vehicle.SaleLocation = vehicle.SaleLocation?.Trim() ?? string.Empty;
        vehicle.Channel = vehicle.Channel?.Trim() ?? string.Empty;
        vehicle.SaleType = vehicle.SaleType?.Trim() ?? string.Empty;
        vehicle.Grade = vehicle.Grade?.Trim() ?? string.Empty;
        vehicle.VatType = vehicle.VatType?.Trim() ?? string.Empty;
        vehicle.AdditionalInfo = vehicle.AdditionalInfo?.Trim() ?? string.Empty;

        // Parse features from raw equipment string
        if (rawEquipment.TryGetValue(vehicle.Id, out var equipment))
        {
            vehicle.Features = SplitAndClean(equipment);
        }

        // Parse declarations from raw declarations string
        if (rawDeclarations.TryGetValue(vehicle.Id, out var declarations))
        {
            vehicle.Declarations = SplitAndClean(declarations);
        }

        // Set processed date
        vehicle.ProcessedDate = DateTime.UtcNow;
    }

    private static string NormalizeToTitleCase(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var trimmed = value.Trim();
        var textInfo = CultureInfo.CurrentCulture.TextInfo;
        return textInfo.ToTitleCase(trimmed.ToLower());
    }

    private static string NormalizeColour(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var trimmed = value.Trim();
        
        // Normalize common colors to title case
        return trimmed.ToUpper() switch
        {
            "GREY" or "GRAY" => "Grey",
            "BLACK" => "Black",
            "WHITE" => "White",
            "BLUE" => "Blue",
            "RED" => "Red",
            "SILVER" => "Silver",
            "GREEN" => "Green",
            "YELLOW" => "Yellow",
            "ORANGE" => "Orange",
            "BROWN" => "Brown",
            "BEIGE" => "Beige",
            "GOLD" => "Gold",
            _ => NormalizeToTitleCase(trimmed)
        };
    }

    private static List<string> SplitAndClean(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return new List<string>();

        return value.Split(',')
            .Select(item => item.Trim())
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .ToList();
    }
}
