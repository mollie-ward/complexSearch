using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VehicleSearch.Core.Entities;
using VehicleSearch.Core.Interfaces;
using VehicleSearch.Core.Models;

namespace VehicleSearch.Infrastructure.Search;

/// <summary>
/// Service for retrieving vehicles from Azure AI Search.
/// </summary>
public class VehicleRetrievalService : IVehicleRetrievalService
{
    private readonly SearchClient _searchClient;
    private readonly ILogger<VehicleRetrievalService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="VehicleRetrievalService"/> class.
    /// </summary>
    public VehicleRetrievalService(
        IOptions<AzureSearchConfig> config,
        ILogger<VehicleRetrievalService> logger)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(logger);

        var configValue = config.Value ?? throw new ArgumentNullException(nameof(config), "Configuration value cannot be null.");
        _logger = logger;

        if (string.IsNullOrWhiteSpace(configValue.Endpoint))
        {
            throw new InvalidOperationException("Azure Search endpoint is not configured.");
        }

        if (string.IsNullOrWhiteSpace(configValue.ApiKey))
        {
            throw new InvalidOperationException("Azure Search API key is not configured.");
        }

        _searchClient = new SearchClient(
            new Uri(configValue.Endpoint),
            configValue.IndexName,
            new AzureKeyCredential(configValue.ApiKey));
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Vehicle>> GetVehiclesByIdsAsync(
        IEnumerable<string> ids,
        CancellationToken cancellationToken = default)
    {
        var idList = ids.ToList();
        if (!idList.Any())
        {
            return Enumerable.Empty<Vehicle>();
        }

        _logger.LogInformation("Retrieving {Count} vehicles by IDs", idList.Count);

        var vehicles = new List<Vehicle>();
        
        // Retrieve documents in parallel
        var tasks = idList.Select(id => GetVehicleByIdAsync(id, cancellationToken));
        var results = await Task.WhenAll(tasks);
        
        vehicles.AddRange(results.Where(v => v != null)!);

        _logger.LogInformation("Retrieved {Count} out of {Requested} vehicles",
            vehicles.Count, idList.Count);

        return vehicles;
    }

    /// <inheritdoc/>
    public async Task<Vehicle?> GetVehicleByIdAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Vehicle ID cannot be null or empty.", nameof(id));
        }

        try
        {
            _logger.LogDebug("Retrieving vehicle {VehicleId}", id);

            var response = await _searchClient.GetDocumentAsync<VehicleSearchDocument>(
                id,
                cancellationToken: cancellationToken);

            if (response?.Value != null)
            {
                return MapToVehicle(response.Value);
            }

            return null;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            _logger.LogWarning("Vehicle {VehicleId} not found in index", id);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving vehicle {VehicleId}", id);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<int> GetTotalCountAsync()
    {
        try
        {
            var searchOptions = new SearchOptions
            {
                IncludeTotalCount = true,
                Size = 0 // We only want the count
            };

            var response = await _searchClient.SearchAsync<VehicleSearchDocument>("*", searchOptions);
            var count = (int)(response.Value.TotalCount ?? 0);

            _logger.LogInformation("Total vehicles in index: {Count}", count);

            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting total vehicle count");
            throw;
        }
    }

    /// <summary>
    /// Maps a VehicleSearchDocument to a Vehicle entity.
    /// </summary>
    private Vehicle MapToVehicle(VehicleSearchDocument doc)
    {
        return new Vehicle
        {
            Id = doc.Id,
            Make = doc.Make,
            Model = doc.Model,
            Derivative = doc.Derivative,
            Price = (decimal)doc.Price,
            Mileage = doc.Mileage,
            BodyType = doc.BodyType,
            EngineSize = (decimal?)doc.EngineSize ?? 0,
            FuelType = doc.FuelType,
            TransmissionType = doc.TransmissionType,
            Colour = doc.Colour,
            NumberOfDoors = doc.NumberOfDoors,
            RegistrationDate = doc.RegistrationDate != DateTimeOffset.MinValue
                ? doc.RegistrationDate.DateTime
                : null,
            SaleLocation = doc.SaleLocation,
            Channel = doc.Channel,
            Features = doc.Features.ToList(),
            Description = doc.Description,
            ProcessedDate = doc.ProcessedDate.DateTime
        };
    }
}
