using VehicleSearch.Core.Entities;

namespace VehicleSearch.Core.Interfaces;

/// <summary>
/// Service interface for retrieving vehicles from the search index.
/// </summary>
public interface IVehicleRetrievalService
{
    /// <summary>
    /// Gets multiple vehicles by their IDs.
    /// </summary>
    /// <param name="ids">The vehicle IDs to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of vehicles.</returns>
    Task<IEnumerable<Vehicle>> GetVehiclesByIdsAsync(
        IEnumerable<string> ids,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a single vehicle by its ID.
    /// </summary>
    /// <param name="id">The vehicle ID to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The vehicle, or null if not found.</returns>
    Task<Vehicle?> GetVehicleByIdAsync(
        string id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total count of vehicles in the index.
    /// </summary>
    /// <returns>The total number of vehicles.</returns>
    Task<int> GetTotalCountAsync();
}
