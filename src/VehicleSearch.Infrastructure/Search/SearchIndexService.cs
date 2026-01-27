using Azure;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VehicleSearch.Core.Interfaces;
using VehicleSearch.Core.Models;

namespace VehicleSearch.Infrastructure.Search;

/// <summary>
/// Service for managing Azure AI Search index operations.
/// </summary>
public class SearchIndexService : ISearchIndexService
{
    private readonly SearchIndexClient _indexClient;
    private readonly AzureSearchConfig _config;
    private readonly ILogger<SearchIndexService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SearchIndexService"/> class.
    /// </summary>
    /// <param name="config">Azure Search configuration.</param>
    /// <param name="logger">Logger instance.</param>
    public SearchIndexService(
        IOptions<AzureSearchConfig> config,
        ILogger<SearchIndexService> logger)
    {
        _config = config.Value ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (string.IsNullOrWhiteSpace(_config.Endpoint))
        {
            throw new InvalidOperationException("Azure Search endpoint is not configured.");
        }

        if (string.IsNullOrWhiteSpace(_config.ApiKey))
        {
            throw new InvalidOperationException("Azure Search API key is not configured.");
        }

        _indexClient = new SearchIndexClient(
            new Uri(_config.Endpoint),
            new AzureKeyCredential(_config.ApiKey));
    }

    /// <inheritdoc/>
    public async Task CreateIndexAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating search index: {IndexName}", _config.IndexName);

            var index = BuildIndexDefinition();
            await _indexClient.CreateIndexAsync(index, cancellationToken);

            _logger.LogInformation("Successfully created search index: {IndexName}", _config.IndexName);
        }
        catch (RequestFailedException ex) when (ex.Status == 409)
        {
            _logger.LogWarning("Index {IndexName} already exists", _config.IndexName);
            throw new InvalidOperationException($"Index '{_config.IndexName}' already exists.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create search index: {IndexName}", _config.IndexName);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task DeleteIndexAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Deleting search index: {IndexName}", _config.IndexName);

            await _indexClient.DeleteIndexAsync(_config.IndexName, cancellationToken);

            _logger.LogInformation("Successfully deleted search index: {IndexName}", _config.IndexName);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            _logger.LogWarning("Index {IndexName} not found", _config.IndexName);
            throw new InvalidOperationException($"Index '{_config.IndexName}' does not exist.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete search index: {IndexName}", _config.IndexName);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> IndexExistsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var indexNames = _indexClient.GetIndexNamesAsync(cancellationToken);
            await foreach (var indexName in indexNames)
            {
                if (indexName == _config.IndexName)
                {
                    return true;
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if index exists: {IndexName}", _config.IndexName);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task UpdateIndexSchemaAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Updating search index schema: {IndexName}", _config.IndexName);

            var index = BuildIndexDefinition();
            await _indexClient.CreateOrUpdateIndexAsync(index, cancellationToken: cancellationToken);

            _logger.LogInformation("Successfully updated search index schema: {IndexName}", _config.IndexName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update search index schema: {IndexName}", _config.IndexName);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IndexStatus> GetIndexStatusAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var exists = await IndexExistsAsync(cancellationToken);

            if (!exists)
            {
                return new IndexStatus
                {
                    Exists = false,
                    IndexName = _config.IndexName,
                    DocumentCount = 0,
                    StorageSize = "0 B"
                };
            }

            var index = await _indexClient.GetIndexAsync(_config.IndexName, cancellationToken);
            var statistics = await _indexClient.GetIndexStatisticsAsync(_config.IndexName, cancellationToken);

            var documentCount = statistics.Value.DocumentCount;
            var storageSize = FormatBytes(statistics.Value.StorageSize);

            return new IndexStatus
            {
                Exists = true,
                IndexName = _config.IndexName,
                DocumentCount = documentCount,
                StorageSize = storageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get index status: {IndexName}", _config.IndexName);
            throw;
        }
    }

    /// <summary>
    /// Builds the complete index definition with all fields, vector search, and semantic configuration.
    /// </summary>
    private SearchIndex BuildIndexDefinition()
    {
        var fields = new List<SearchField>
        {
            // Identity & Text Fields
            new SearchField("id", SearchFieldDataType.String) { IsKey = true, IsSearchable = false },
            new SearchField("make", SearchFieldDataType.String) { IsSearchable = true, IsFilterable = true, IsFacetable = true },
            new SearchField("model", SearchFieldDataType.String) { IsSearchable = true, IsFilterable = true },
            new SearchField("derivative", SearchFieldDataType.String) { IsSearchable = true, IsFilterable = false },
            new SearchField("bodyType", SearchFieldDataType.String) { IsFilterable = true, IsFacetable = true },
            new SearchField("colour", SearchFieldDataType.String) { IsFilterable = true, IsFacetable = true },

            // Numeric Fields
            new SearchField("price", SearchFieldDataType.Double) { IsFilterable = true, IsSortable = true, IsFacetable = true },
            new SearchField("mileage", SearchFieldDataType.Int32) { IsFilterable = true, IsSortable = true },
            new SearchField("engineSize", SearchFieldDataType.Double) { IsFilterable = true },
            new SearchField("numberOfDoors", SearchFieldDataType.Int32) { IsFilterable = true, IsFacetable = true },

            // Date Fields
            new SearchField("registrationDate", SearchFieldDataType.DateTimeOffset) { IsFilterable = true, IsSortable = true },
            new SearchField("motExpiryDate", SearchFieldDataType.DateTimeOffset) { IsFilterable = true },

            // Categorical Fields
            new SearchField("fuelType", SearchFieldDataType.String) { IsFilterable = true, IsFacetable = true },
            new SearchField("transmissionType", SearchFieldDataType.String) { IsFilterable = true, IsFacetable = true },
            new SearchField("saleLocation", SearchFieldDataType.String) { IsFilterable = true, IsFacetable = true },

            // Array Fields
            new SearchField("features", SearchFieldDataType.Collection(SearchFieldDataType.String)) { IsSearchable = true, IsFilterable = true },

            // Full-Text Search Field
            new SearchField("description", SearchFieldDataType.String) { IsSearchable = true, AnalyzerName = LexicalAnalyzerName.EnMicrosoft },

            // Vector Field for Semantic Search
            new SearchField("descriptionVector", SearchFieldDataType.Collection(SearchFieldDataType.Single))
            {
                IsSearchable = true,
                VectorSearchDimensions = _config.VectorDimensions,
                VectorSearchProfileName = "vector-profile"
            }
        };

        // Vector Search Configuration
        var vectorSearch = new VectorSearch();
        
        // HNSW Algorithm Configuration
        var hnswConfig = new HnswAlgorithmConfiguration("vector-config")
        {
            Parameters = new HnswParameters
            {
                Metric = VectorSearchAlgorithmMetric.Cosine,
                M = 4,
                EfConstruction = 400,
                EfSearch = 500
            }
        };
        vectorSearch.Algorithms.Add(hnswConfig);

        // Vector Profile
        var vectorProfile = new VectorSearchProfile("vector-profile", "vector-config");
        vectorSearch.Profiles.Add(vectorProfile);

        // Semantic Configuration
        var semanticConfig = new SemanticConfiguration("semantic-config", new SemanticPrioritizedFields
        {
            TitleField = new SemanticField("make"),
            ContentFields =
            {
                new SemanticField("description"),
                new SemanticField("model"),
                new SemanticField("features")
            },
            KeywordsFields =
            {
                new SemanticField("make"),
                new SemanticField("fuelType"),
                new SemanticField("bodyType")
            }
        });

        var semanticSearch = new SemanticSearch();
        semanticSearch.Configurations.Add(semanticConfig);

        // Build the index
        var index = new SearchIndex(_config.IndexName)
        {
            Fields = fields,
            VectorSearch = vectorSearch,
            SemanticSearch = semanticSearch
        };

        return index;
    }

    /// <summary>
    /// Formats bytes to human-readable format.
    /// </summary>
    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}
