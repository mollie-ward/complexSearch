using VehicleSearch.Core.Entities;
using VehicleSearch.Core.Models;

namespace VehicleSearch.Core.Interfaces;

/// <summary>
/// Service interface for indexing vehicles in Azure AI Search.
/// </summary>
public interface IVehicleIndexingService
{
    /// <summary>
    /// Indexes multiple vehicles in the search index.
    /// </summary>
    /// <param name="vehicles">The vehicles to index.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the indexing operation.</returns>
    Task<IndexingResult> IndexVehiclesAsync(
        IEnumerable<Vehicle> vehicles,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Indexes a single vehicle in the search index.
    /// </summary>
    /// <param name="vehicle">The vehicle to index.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the indexing operation.</returns>
    Task<IndexingResult> IndexVehicleAsync(
        Vehicle vehicle,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a vehicle from the search index.
    /// </summary>
    /// <param name="vehicleId">The ID of the vehicle to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the vehicle was deleted, false otherwise.</returns>
    Task<bool> DeleteVehicleAsync(
        string vehicleId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets statistics about the search index.
    /// </summary>
    /// <returns>Index statistics.</returns>
    Task<IndexStats> GetIndexStatsAsync();
}
