using System.Diagnostics;
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
/// Service for indexing vehicles in Azure AI Search.
/// </summary>
public class VehicleIndexingService : IVehicleIndexingService
{
    private readonly SearchClient _searchClient;
    private readonly IEmbeddingService _embeddingService;
    private readonly AzureSearchConfig _config;
    private readonly ILogger<VehicleIndexingService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="VehicleIndexingService"/> class.
    /// </summary>
    public VehicleIndexingService(
        IOptions<AzureSearchConfig> config,
        IEmbeddingService embeddingService,
        ILogger<VehicleIndexingService> logger)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(embeddingService);
        ArgumentNullException.ThrowIfNull(logger);

        _config = config.Value ?? throw new ArgumentNullException(nameof(config), "Configuration value cannot be null.");
        _embeddingService = embeddingService;
        _logger = logger;

        if (string.IsNullOrWhiteSpace(_config.Endpoint))
        {
            throw new InvalidOperationException("Azure Search endpoint is not configured.");
        }

        if (string.IsNullOrWhiteSpace(_config.ApiKey))
        {
            throw new InvalidOperationException("Azure Search API key is not configured.");
        }

        _searchClient = new SearchClient(
            new Uri(_config.Endpoint),
            _config.IndexName,
            new AzureKeyCredential(_config.ApiKey));
    }

    /// <inheritdoc/>
    public async Task<Core.Models.IndexingResult> IndexVehiclesAsync(
        IEnumerable<Vehicle> vehicles,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new Core.Models.IndexingResult();
        var vehicleList = vehicles.ToList();

        result.TotalVehicles = vehicleList.Count;

        if (!vehicleList.Any())
        {
            _logger.LogWarning("No vehicles to index");
            return result;
        }

        _logger.LogInformation("Starting indexing of {Count} vehicles", vehicleList.Count);

        try
        {
            // Generate embeddings for all vehicles
            var embeddings = await _embeddingService.GenerateBatchEmbeddingsAsync(
                vehicleList,
                cancellationToken: cancellationToken);
            var embeddingsDict = embeddings.ToDictionary(e => e.VehicleId, e => e.Vector);
            result.EmbeddingsGenerated = embeddingsDict.Count;

            _logger.LogInformation("Generated {Count} embeddings", result.EmbeddingsGenerated);

            // Map vehicles to search documents
            var documents = new List<VehicleSearchDocument>();
            foreach (var vehicle in vehicleList)
            {
                if (embeddingsDict.TryGetValue(vehicle.Id, out var vector))
                {
                    var doc = MapToSearchDocument(vehicle, vector);
                    documents.Add(doc);
                }
                else
                {
                    _logger.LogWarning("No embedding found for vehicle {VehicleId}, skipping", vehicle.Id);
                    result.Failed++;
                    result.Errors.Add(new IndexingError
                    {
                        VehicleId = vehicle.Id,
                        Message = "Failed to generate embedding",
                        Timestamp = DateTime.UtcNow
                    });
                }
            }

            // Upload documents to search index in batches
            const int uploadBatchSize = 1000;
            
            foreach (var batch in documents.Chunk(uploadBatchSize))
            {
                try
                {
                    var response = await _searchClient.IndexDocumentsAsync(
                        IndexDocumentsBatch.Upload(batch),
                        cancellationToken: cancellationToken);

                    foreach (var indexResult in response.Value.Results)
                    {
                        if (indexResult.Succeeded)
                        {
                            result.Succeeded++;
                        }
                        else
                        {
                            result.Failed++;
                            var errorMessage = SanitizeErrorMessage(indexResult.ErrorMessage ?? "Unknown error");
                            result.Errors.Add(new IndexingError
                            {
                                VehicleId = indexResult.Key,
                                Message = errorMessage,
                                Timestamp = DateTime.UtcNow
                            });
                            _logger.LogError("Failed to index vehicle {VehicleId}: {Error}",
                                indexResult.Key, indexResult.ErrorMessage);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error uploading batch to search index");
                    var sanitizedMessage = SanitizeErrorMessage(ex.Message);
                    result.Failed += batch.Length;
                    foreach (var doc in batch)
                    {
                        result.Errors.Add(new IndexingError
                        {
                            VehicleId = doc.Id,
                            Message = sanitizedMessage,
                            ExceptionType = ex.GetType().Name,
                            Timestamp = DateTime.UtcNow
                        });
                    }
                }
            }

            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;

            _logger.LogInformation(
                "Indexing completed. Total: {Total}, Succeeded: {Succeeded}, Failed: {Failed}, Duration: {Duration}ms",
                result.TotalVehicles, result.Succeeded, result.Failed, result.Duration.TotalMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during vehicle indexing");
            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Core.Models.IndexingResult> IndexVehicleAsync(
        Vehicle vehicle,
        CancellationToken cancellationToken = default)
    {
        return await IndexVehiclesAsync(new[] { vehicle }, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteVehicleAsync(
        string vehicleId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Deleting vehicle {VehicleId} from index", vehicleId);

            var response = await _searchClient.DeleteDocumentsAsync(
                "id",
                new[] { vehicleId },
                cancellationToken: cancellationToken);

            var result = response.Value.Results.FirstOrDefault();
            if (result?.Succeeded == true)
            {
                _logger.LogInformation("Successfully deleted vehicle {VehicleId}", vehicleId);
                return true;
            }

            _logger.LogWarning("Failed to delete vehicle {VehicleId}: {Error}",
                vehicleId, result?.ErrorMessage);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting vehicle {VehicleId}", vehicleId);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<IndexStats> GetIndexStatsAsync()
    {
        try
        {
            // Get document count using a search query
            var searchOptions = new SearchOptions
            {
                IncludeTotalCount = true,
                Size = 0 // We only want the count, not the documents
            };

            var response = await _searchClient.SearchAsync<VehicleSearchDocument>("*", searchOptions);
            var count = response.Value.TotalCount ?? 0;

            return new IndexStats
            {
                DocumentCount = count,
                IndexName = _config.IndexName,
                StorageSize = "Unknown", // Azure SDK doesn't provide direct access to storage size from SearchClient
                LastUpdated = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting index stats");
            throw;
        }
    }

    /// <summary>
    /// Maps a Vehicle entity to a VehicleSearchDocument.
    /// </summary>
    private VehicleSearchDocument MapToSearchDocument(Vehicle vehicle, float[] embedding)
    {
        return new VehicleSearchDocument
        {
            Id = vehicle.Id,
            Make = vehicle.Make,
            Model = vehicle.Model,
            Derivative = vehicle.Derivative,
            Price = (double)vehicle.Price,
            Mileage = vehicle.Mileage,
            BodyType = vehicle.BodyType,
            EngineSize = (double?)vehicle.EngineSize,
            FuelType = vehicle.FuelType,
            TransmissionType = vehicle.TransmissionType,
            Colour = vehicle.Colour,
            NumberOfDoors = vehicle.NumberOfDoors,
            RegistrationDate = vehicle.RegistrationDate.HasValue
                ? new DateTimeOffset(vehicle.RegistrationDate.Value)
                : DateTimeOffset.MinValue,
            SaleLocation = vehicle.SaleLocation,
            Channel = vehicle.Channel,
            Features = vehicle.Features.ToArray(),
            Description = vehicle.Description,
            DescriptionVector = embedding,
            ProcessedDate = new DateTimeOffset(vehicle.ProcessedDate)
        };
    }

    /// <summary>
    /// Sanitizes error messages to prevent leaking sensitive information.
    /// </summary>
    private static string SanitizeErrorMessage(string errorMessage)
    {
        // Remove potential connection strings, URLs, or other sensitive data
        if (string.IsNullOrWhiteSpace(errorMessage))
        {
            return "An error occurred during indexing";
        }

        // If the message contains authentication or connection info, use generic message
        if (errorMessage.Contains("AccountKey", StringComparison.OrdinalIgnoreCase) ||
            errorMessage.Contains("ConnectionString", StringComparison.OrdinalIgnoreCase) ||
            errorMessage.Contains("password", StringComparison.OrdinalIgnoreCase))
        {
            return "An error occurred during indexing. Check logs for details.";
        }

        return errorMessage;
    }
}
