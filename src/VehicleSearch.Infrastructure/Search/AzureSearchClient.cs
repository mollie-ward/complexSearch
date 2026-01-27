using Azure;
using Azure.Search.Documents;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VehicleSearch.Core.Models;

namespace VehicleSearch.Infrastructure.Search;

/// <summary>
/// Client for Azure Search operations.
/// Provides access to Azure AI Search for document operations.
/// </summary>
public class AzureSearchClient
{
    private readonly SearchClient _searchClient;
    private readonly AzureSearchConfig _config;
    private readonly ILogger<AzureSearchClient> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureSearchClient"/> class.
    /// </summary>
    /// <param name="config">Azure Search configuration.</param>
    /// <param name="logger">Logger instance.</param>
    public AzureSearchClient(
        IOptions<AzureSearchConfig> config,
        ILogger<AzureSearchClient> logger)
    {
        _config = config.Value ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (string.IsNullOrWhiteSpace(_config.Endpoint))
        {
            _logger.LogWarning("Azure Search endpoint is not configured. Search functionality will be limited.");
            _searchClient = null!;
            return;
        }

        if (string.IsNullOrWhiteSpace(_config.ApiKey))
        {
            _logger.LogWarning("Azure Search API key is not configured. Search functionality will be limited.");
            _searchClient = null!;
            return;
        }

        _searchClient = new SearchClient(
            new Uri(_config.Endpoint),
            _config.IndexName,
            new AzureKeyCredential(_config.ApiKey));

        _logger.LogInformation("Azure Search client initialized for index: {IndexName}", _config.IndexName);
    }

    /// <summary>
    /// Gets the search client for document operations.
    /// </summary>
    public SearchClient Client => _searchClient;
}
