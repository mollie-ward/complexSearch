using Azure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Embeddings;
using VehicleSearch.Core.Entities;
using VehicleSearch.Core.Interfaces;
using VehicleSearch.Core.Models;

namespace VehicleSearch.Infrastructure.AI;

/// <summary>
/// Service for generating embeddings using Azure OpenAI.
/// </summary>
public class EmbeddingService : IEmbeddingService
{
    private readonly EmbeddingClient _embeddingClient;
    private readonly AzureOpenAIConfig _config;
    private readonly ILogger<EmbeddingService> _logger;
    private readonly SemaphoreSlim _semaphore;

    /// <summary>
    /// Initializes a new instance of the <see cref="EmbeddingService"/> class.
    /// </summary>
    public EmbeddingService(
        IOptions<AzureOpenAIConfig> config,
        ILogger<EmbeddingService> logger)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(logger);

        _config = config.Value ?? throw new ArgumentNullException(nameof(config), "Configuration value cannot be null.");
        _logger = logger;

        if (string.IsNullOrWhiteSpace(_config.Endpoint))
        {
            throw new InvalidOperationException("Azure OpenAI endpoint is not configured.");
        }

        if (string.IsNullOrWhiteSpace(_config.ApiKey))
        {
            throw new InvalidOperationException("Azure OpenAI API key is not configured.");
        }

        // Create OpenAI client with Azure endpoint
        var openAIClient = new OpenAIClient(
            new AzureKeyCredential(_config.ApiKey),
            new OpenAIClientOptions
            {
                Endpoint = new Uri(_config.Endpoint)
            });

        _embeddingClient = openAIClient.GetEmbeddingClient(_config.EmbeddingDeploymentName);
        _semaphore = new SemaphoreSlim(_config.MaxConcurrentRequests);
    }

    /// <inheritdoc/>
    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Text cannot be null or empty.", nameof(text));
        }

        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var retryCount = 0;
            while (retryCount <= _config.MaxRetries)
            {
                try
                {
                    var response = await _embeddingClient.GenerateEmbeddingAsync(text, cancellationToken: cancellationToken);
                    var embedding = response.Value.ToFloats().ToArray();

                    _logger.LogDebug("Generated embedding with {Dimensions} dimensions", embedding.Length);
                    return embedding;
                }
                catch (Exception ex) when (IsRateLimitException(ex) && retryCount < _config.MaxRetries)
                {
                    // Rate limit exceeded, use exponential backoff
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, retryCount));
                    _logger.LogWarning("Rate limit exceeded. Retrying after {Delay}s (attempt {Attempt}/{MaxRetries})",
                        delay.TotalSeconds, retryCount + 1, _config.MaxRetries);
                    await Task.Delay(delay, cancellationToken);
                    retryCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error generating embedding");
                    throw;
                }
            }

            throw new InvalidOperationException($"Failed to generate embedding after {_config.MaxRetries} retries.");
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<VehicleEmbedding>> GenerateBatchEmbeddingsAsync(
        IEnumerable<Vehicle> vehicles,
        int batchSize = 100,
        CancellationToken cancellationToken = default)
    {
        var vehicleList = vehicles.ToList();
        if (!vehicleList.Any())
        {
            return Enumerable.Empty<VehicleEmbedding>();
        }

        _logger.LogInformation("Generating embeddings for {Count} vehicles in batches of {BatchSize}",
            vehicleList.Count, batchSize);

        var embeddings = new List<VehicleEmbedding>();
        var batches = vehicleList.Chunk(batchSize).ToList();

        for (int i = 0; i < batches.Count; i++)
        {
            var batch = batches[i];
            _logger.LogInformation("Processing batch {Current}/{Total} ({Count} vehicles)",
                i + 1, batches.Count, batch.Length);

            var batchTasks = batch.Select(async vehicle =>
            {
                try
                {
                    var vector = await GenerateEmbeddingAsync(vehicle.Description, cancellationToken);
                    return new VehicleEmbedding
                    {
                        VehicleId = vehicle.Id,
                        Vector = vector,
                        Dimensions = vector.Length
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to generate embedding for vehicle {VehicleId}", vehicle.Id);
                    return null;
                }
            });

            var batchResults = await Task.WhenAll(batchTasks);
            embeddings.AddRange(batchResults.Where(e => e != null)!);

            // Small delay between batches to avoid overwhelming the API
            if (i < batches.Count - 1)
            {
                await Task.Delay(100, cancellationToken);
            }
        }

        _logger.LogInformation("Successfully generated {Count} embeddings out of {Total} vehicles",
            embeddings.Count, vehicleList.Count);

        return embeddings;
    }

    /// <summary>
    /// Determines if the exception is a rate limit exception.
    /// </summary>
    private static bool IsRateLimitException(Exception ex)
    {
        // Check for Azure RequestFailedException with 429 status
        if (ex is Azure.RequestFailedException rfe && rfe.Status == 429)
        {
            return true;
        }
        
        // Fallback to message check for other exception types
        return ex.Message.Contains("429") || ex.Message.Contains("rate limit", StringComparison.OrdinalIgnoreCase);
    }
}
