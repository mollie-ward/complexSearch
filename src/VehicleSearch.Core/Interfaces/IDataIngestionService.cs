using VehicleSearch.Core.Entities;
using VehicleSearch.Core.Models;

namespace VehicleSearch.Core.Interfaces;

/// <summary>
/// Service interface for ingesting vehicle data from various sources.
/// </summary>
public interface IDataIngestionService
{
    /// <summary>
    /// Ingests vehicle data from a CSV file.
    /// </summary>
    /// <param name="filePath">The path to the CSV file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The ingestion result with statistics and errors.</returns>
    Task<IngestionResult> IngestFromCsvAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Parses vehicle data from a CSV stream.
    /// </summary>
    /// <param name="csvStream">The CSV stream to parse.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of parsed vehicles.</returns>
    Task<IEnumerable<Vehicle>> ParseCsvAsync(Stream csvStream, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a collection of vehicles.
    /// </summary>
    /// <param name="vehicles">The vehicles to validate.</param>
    /// <returns>The validation result.</returns>
    Task<ValidationResult> ValidateDataAsync(IEnumerable<Vehicle> vehicles);
}
