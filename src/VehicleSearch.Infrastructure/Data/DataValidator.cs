using Microsoft.Extensions.Logging;
using VehicleSearch.Core.Entities;
using VehicleSearch.Core.Models;

namespace VehicleSearch.Infrastructure.Data;

/// <summary>
/// Service for validating vehicle data.
/// </summary>
public class DataValidator
{
    private readonly ILogger<DataValidator> _logger;
    private static readonly int[] ValidDoors = { 2, 3, 4, 5, 7 };
    private static readonly DateTime MinRegistrationDate = new(1990, 1, 1);
    private static readonly DateTime MaxRegistrationDate = new(2026, 12, 31);

    /// <summary>
    /// Initializes a new instance of the <see cref="DataValidator"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public DataValidator(ILogger<DataValidator> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Validates a collection of vehicles.
    /// </summary>
    /// <param name="vehicles">The vehicles to validate.</param>
    /// <returns>The validation result.</returns>
    public ValidationResult Validate(IEnumerable<Vehicle> vehicles)
    {
        var result = new ValidationResult { IsValid = true };
        var rowNumber = 1; // Start from 1 (excluding header)

        foreach (var vehicle in vehicles)
        {
            rowNumber++;
            var errors = ValidateVehicle(vehicle, rowNumber);
            
            if (errors.Any())
            {
                result.IsValid = false;
                result.Errors.AddRange(errors);
                
                _logger.LogWarning("Validation errors found for vehicle {VehicleId} at row {RowNumber}: {ErrorCount} errors",
                    vehicle.Id, rowNumber, errors.Count);
            }
        }

        if (result.IsValid)
        {
            _logger.LogInformation("All vehicles validated successfully");
        }
        else
        {
            _logger.LogWarning("Validation completed with {ErrorCount} errors across {VehicleCount} vehicles",
                result.ErrorCount, vehicles.Count());
        }

        return result;
    }

    private List<ValidationError> ValidateVehicle(Vehicle vehicle, int rowNumber)
    {
        var errors = new List<ValidationError>();

        // Required field: Registration Number
        if (string.IsNullOrWhiteSpace(vehicle.Id))
        {
            errors.Add(new ValidationError
            {
                RowNumber = rowNumber,
                FieldName = "Registration Number",
                Message = "Registration Number is required",
                Value = vehicle.Id
            });
        }

        // Required field: Make
        if (string.IsNullOrWhiteSpace(vehicle.Make))
        {
            errors.Add(new ValidationError
            {
                RowNumber = rowNumber,
                FieldName = "Make",
                Message = "Make is required",
                Value = vehicle.Make
            });
        }

        // Required field: Model
        if (string.IsNullOrWhiteSpace(vehicle.Model))
        {
            errors.Add(new ValidationError
            {
                RowNumber = rowNumber,
                FieldName = "Model",
                Message = "Model is required",
                Value = vehicle.Model
            });
        }

        // Price validation: must be > 0 and < 200,000
        if (vehicle.Price <= 0)
        {
            errors.Add(new ValidationError
            {
                RowNumber = rowNumber,
                FieldName = "Price",
                Message = "Price must be greater than 0",
                Value = vehicle.Price.ToString()
            });
        }
        else if (vehicle.Price >= 200000)
        {
            errors.Add(new ValidationError
            {
                RowNumber = rowNumber,
                FieldName = "Price",
                Message = "Price must be less than 200,000",
                Value = vehicle.Price.ToString()
            });
        }

        // Mileage validation: must be >= 0 and < 500,000
        if (vehicle.Mileage < 0)
        {
            errors.Add(new ValidationError
            {
                RowNumber = rowNumber,
                FieldName = "Mileage",
                Message = "Mileage must be greater than or equal to 0",
                Value = vehicle.Mileage.ToString()
            });
        }
        else if (vehicle.Mileage >= 500000)
        {
            errors.Add(new ValidationError
            {
                RowNumber = rowNumber,
                FieldName = "Mileage",
                Message = "Mileage must be less than 500,000",
                Value = vehicle.Mileage.ToString()
            });
        }

        // Engine size validation: must be > 0 and < 10.0
        if (vehicle.EngineSize > 0)
        {
            if (vehicle.EngineSize >= 10.0m)
            {
                errors.Add(new ValidationError
                {
                    RowNumber = rowNumber,
                    FieldName = "Engine Size",
                    Message = "Engine size must be less than 10.0 liters",
                    Value = vehicle.EngineSize.ToString()
                });
            }
        }

        // Registration date validation
        if (vehicle.RegistrationDate.HasValue)
        {
            if (vehicle.RegistrationDate.Value < MinRegistrationDate ||
                vehicle.RegistrationDate.Value > MaxRegistrationDate)
            {
                errors.Add(new ValidationError
                {
                    RowNumber = rowNumber,
                    FieldName = "Registration Date",
                    Message = $"Registration date must be between {MinRegistrationDate:yyyy} and {MaxRegistrationDate:yyyy}",
                    Value = vehicle.RegistrationDate.Value.ToString("yyyy-MM-dd")
                });
            }
        }

        // Number of doors validation
        if (vehicle.NumberOfDoors.HasValue)
        {
            if (!ValidDoors.Contains(vehicle.NumberOfDoors.Value))
            {
                errors.Add(new ValidationError
                {
                    RowNumber = rowNumber,
                    FieldName = "Number Of Doors",
                    Message = "Number of doors must be 2, 3, 4, 5, or 7",
                    Value = vehicle.NumberOfDoors.Value.ToString()
                });
            }
        }

        return errors;
    }
}
