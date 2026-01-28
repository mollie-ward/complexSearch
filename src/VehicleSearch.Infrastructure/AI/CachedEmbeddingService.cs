using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using VehicleSearch.Core.Entities;
using VehicleSearch.Core.Interfaces;
using VehicleSearch.Core.Models;

namespace VehicleSearch.Infrastructure.AI;

/// <summary>
/// Decorator for IEmbeddingService that adds caching capabilities.
/// </summary>
public class CachedEmbeddingService : IEmbeddingService
{
    private readonly IEmbeddingService _innerService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<CachedEmbeddingService> _logger;
    
    private const int CacheExpirationHours = 24;
    private const int MaxCacheSize = 1000;

    /// <summary>
    /// Initializes a new instance of the <see cref="CachedEmbeddingService"/> class.
    /// </summary>
    public CachedEmbeddingService(
        IEmbeddingService innerService,
        IMemoryCache cache,
        ILogger<CachedEmbeddingService> logger)
    {
        _innerService = innerService ?? throw new ArgumentNullException(nameof(innerService));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Text cannot be null or empty.", nameof(text));
        }

        // Normalize cache key (lowercase, trim)
        var cacheKey = $"embedding:{text.Trim().ToLowerInvariant()}";

        // Try to get from cache
        if (_cache.TryGetValue<float[]>(cacheKey, out var cachedEmbedding) && cachedEmbedding != null)
        {
            _logger.LogDebug("Cache hit for embedding: {Text}", text[..Math.Min(50, text.Length)]);
            return cachedEmbedding;
        }

        _logger.LogDebug("Cache miss for embedding: {Text}", text[..Math.Min(50, text.Length)]);

        // Generate embedding via inner service
        var embedding = await _innerService.GenerateEmbeddingAsync(text, cancellationToken);

        // Store in cache with expiration
        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(CacheExpirationHours),
            Size = 1 // Each entry counts as 1 towards the size limit
        };

        _cache.Set(cacheKey, embedding, cacheOptions);
        _logger.LogDebug("Cached embedding for: {Text}", text[..Math.Min(50, text.Length)]);

        return embedding;
    }

    /// <inheritdoc/>
    public Task<IEnumerable<VehicleEmbedding>> GenerateBatchEmbeddingsAsync(
        IEnumerable<Vehicle> vehicles,
        int batchSize = 100,
        CancellationToken cancellationToken = default)
    {
        // For batch operations, delegate directly to inner service
        // Caching individual vehicle embeddings would be complex and less beneficial
        return _innerService.GenerateBatchEmbeddingsAsync(vehicles, batchSize, cancellationToken);
    }

    /// <summary>
    /// Prepares a query for embedding by adding contextual enrichment.
    /// </summary>
    /// <param name="query">The original query.</param>
    /// <returns>The enriched query text.</returns>
    public static string PrepareQueryForEmbedding(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return query;
        }

        // Normalize the query
        var normalizedQuery = query.Trim();

        // Add contextual enrichment for better semantic matching
        // Map qualitative terms to their broader concepts
        var enrichments = new List<string>();

        var lowerQuery = normalizedQuery.ToLowerInvariant();

        if (lowerQuery.Contains("economical") || lowerQuery.Contains("fuel efficient"))
        {
            enrichments.Add("economical vehicles");
            enrichments.Add("fuel-efficient cars");
        }

        if (lowerQuery.Contains("reliable"))
        {
            enrichments.Add("reliable vehicles");
            enrichments.Add("well-maintained cars");
        }

        if (lowerQuery.Contains("family"))
        {
            enrichments.Add("family vehicle");
            enrichments.Add("spacious practical");
        }

        if (lowerQuery.Contains("sporty") || lowerQuery.Contains("performance"))
        {
            enrichments.Add("sporty performance vehicles");
        }

        // Combine original query with enrichments
        if (enrichments.Any())
        {
            return $"{normalizedQuery} {string.Join(" ", enrichments)}";
        }

        return normalizedQuery;
    }
}
