using System.Diagnostics;
using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Microsoft.Extensions.Logging;
using VehicleSearch.Core.Entities;
using VehicleSearch.Core.Interfaces;
using VehicleSearch.Core.Models;

namespace VehicleSearch.Infrastructure.Search;

/// <summary>
/// Service for performing semantic search using vector embeddings.
/// </summary>
public class SemanticSearchService : ISemanticSearchService
{
    private readonly IEmbeddingService _embeddingService;
    private readonly AzureSearchClient _searchClient;
    private readonly ILogger<SemanticSearchService> _logger;
    
    private const double MinimumRelevanceScore = 0.50;
    private const int OverqueryMultiplier = 3;

    /// <summary>
    /// Initializes a new instance of the <see cref="SemanticSearchService"/> class.
    /// </summary>
    public SemanticSearchService(
        IEmbeddingService embeddingService,
        AzureSearchClient searchClient,
        ILogger<SemanticSearchService> logger)
    {
        _embeddingService = embeddingService ?? throw new ArgumentNullException(nameof(embeddingService));
        _searchClient = searchClient ?? throw new ArgumentNullException(nameof(searchClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<SemanticSearchResponse> SearchAsync(
        SemanticSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Query))
        {
            throw new ArgumentException("Query cannot be empty.", nameof(request));
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Starting semantic search for query: {Query}", request.Query);

            // Prepare and enrich the query
            var enrichedQuery = AI.CachedEmbeddingService.PrepareQueryForEmbedding(request.Query);
            _logger.LogDebug("Enriched query: {EnrichedQuery}", enrichedQuery);

            // Generate embedding for the query
            var embedding = await _embeddingService.GenerateEmbeddingAsync(enrichedQuery, cancellationToken);
            _logger.LogDebug("Generated embedding with {Dimensions} dimensions", embedding.Length);

            // Perform vector search
            var matches = await SearchByEmbeddingAsync(
                embedding,
                request.MaxResults,
                request.Filters,
                cancellationToken);

            stopwatch.Stop();

            // Calculate average score
            var averageScore = matches.Any() ? matches.Average(m => m.SimilarityScore) : 0.0;

            var response = new SemanticSearchResponse
            {
                Matches = matches,
                AverageScore = averageScore,
                SearchDuration = stopwatch.Elapsed
            };

            _logger.LogInformation(
                "Semantic search completed. Found {Count} matches with average score {AvgScore:F3} in {Duration}ms",
                matches.Count,
                averageScore,
                stopwatch.ElapsedMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing semantic search for query: {Query}", request.Query);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<List<VehicleMatch>> SearchByEmbeddingAsync(
        float[] embedding,
        int maxResults,
        List<SearchConstraint>? filters = null,
        CancellationToken cancellationToken = default)
    {
        if (embedding == null || embedding.Length == 0)
        {
            throw new ArgumentException("Embedding cannot be null or empty.", nameof(embedding));
        }

        if (maxResults <= 0)
        {
            throw new ArgumentException("MaxResults must be greater than 0.", nameof(maxResults));
        }

        try
        {
            // Use overquery to get more results for filtering
            var overquerySize = maxResults * OverqueryMultiplier;

            // Build vector search options
            var vectorQuery = new VectorizedQuery(embedding)
            {
                // Search in the DescriptionVector field
                KNearestNeighborsCount = overquerySize,
                Fields = { "descriptionVector" }
            };

            var searchOptions = new SearchOptions
            {
                Size = overquerySize,
                Select = { "id", "make", "model", "derivative", "price", "mileage", 
                          "bodyType", "engineSize", "fuelType", "transmissionType", 
                          "colour", "numberOfDoors", "registrationDate", "saleLocation",
                          "channel", "features", "description" }
            };

            searchOptions.VectorSearch = new VectorSearchOptions
            {
                Queries = { vectorQuery }
            };

            // Add filters if provided
            if (filters != null && filters.Any())
            {
                var filterString = BuildODataFilter(filters);
                if (!string.IsNullOrEmpty(filterString))
                {
                    searchOptions.Filter = filterString;
                    _logger.LogDebug("Applied filter: {Filter}", filterString);
                }
            }

            // Execute vector search
            var searchResults = await _searchClient.Client.SearchAsync<VehicleSearchDocument>(
                null, // No text search, only vector
                searchOptions,
                cancellationToken);

            var matches = new List<VehicleMatch>();

            await foreach (var result in searchResults.Value.GetResultsAsync())
            {
                var document = result.Document;
                var score = result.Score ?? 0.0;

                // Filter by minimum relevance score
                if (score < MinimumRelevanceScore)
                {
                    _logger.LogDebug(
                        "Skipping result {VehicleId} with score {Score:F3} (below minimum {MinScore:F3})",
                        document.Id,
                        score,
                        MinimumRelevanceScore);
                    continue;
                }

                // Convert document to Vehicle entity
                var vehicle = MapToVehicle(document);

                var match = new VehicleMatch
                {
                    VehicleId = document.Id,
                    Vehicle = vehicle,
                    SimilarityScore = score,
                    NormalizedScore = (int)Math.Round(score * 100)
                };

                matches.Add(match);

                // Stop if we have enough results
                if (matches.Count >= maxResults)
                {
                    break;
                }
            }

            _logger.LogInformation(
                "Vector search returned {Count} matches above threshold {MinScore:F2}",
                matches.Count,
                MinimumRelevanceScore);

            return matches;
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Azure Search request failed with status {Status}", ex.Status);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing vector search");
            throw;
        }
    }

    /// <summary>
    /// Builds an OData filter string from search constraints with field name validation.
    /// </summary>
    private string BuildODataFilter(List<SearchConstraint> constraints)
    {
        // Whitelist of allowed field names for security
        var allowedFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "id", "make", "model", "derivative", "price", "mileage", "bodyType",
            "engineSize", "fuelType", "transmissionType", "colour", "numberOfDoors",
            "registrationDate", "saleLocation", "channel"
        };

        var filters = new List<string>();

        foreach (var constraint in constraints)
        {
            // Validate field name against whitelist
            if (!allowedFields.Contains(constraint.FieldName))
            {
                _logger.LogWarning("Attempted to use invalid field name in filter: {FieldName}", constraint.FieldName);
                continue; // Skip invalid field names
            }

            var filter = constraint.Operator switch
            {
                ConstraintOperator.Equals => $"{constraint.FieldName} eq {FormatValue(constraint.Value)}",
                ConstraintOperator.NotEquals => $"{constraint.FieldName} ne {FormatValue(constraint.Value)}",
                ConstraintOperator.GreaterThan => $"{constraint.FieldName} gt {FormatValue(constraint.Value)}",
                ConstraintOperator.GreaterThanOrEqual => $"{constraint.FieldName} ge {FormatValue(constraint.Value)}",
                ConstraintOperator.LessThan => $"{constraint.FieldName} lt {FormatValue(constraint.Value)}",
                ConstraintOperator.LessThanOrEqual => $"{constraint.FieldName} le {FormatValue(constraint.Value)}",
                ConstraintOperator.In => BuildInFilter(constraint.FieldName, constraint.Value),
                _ => null
            };

            if (!string.IsNullOrEmpty(filter))
            {
                filters.Add(filter);
            }
        }

        return filters.Any() ? string.Join(" and ", filters) : string.Empty;
    }

    /// <summary>
    /// Builds an "in" filter for OData with proper escaping.
    /// </summary>
    private string BuildInFilter(string fieldName, object value)
    {
        if (value is IEnumerable<string> values)
        {
            var conditions = values.Select(v => $"{fieldName} eq '{v.Replace("'", "''")}'"); // Escape single quotes
            return $"({string.Join(" or ", conditions)})";
        }

        return string.Empty;
    }

    /// <summary>
    /// Formats a value for OData filter with proper escaping.
    /// </summary>
    private string FormatValue(object value)
    {
        return value switch
        {
            string s => $"'{s.Replace("'", "''")}'", // Escape single quotes for OData
            bool b => b.ToString().ToLowerInvariant(),
            _ => value.ToString() ?? string.Empty
        };
    }

    /// <summary>
    /// Maps a VehicleSearchDocument to a Vehicle entity.
    /// </summary>
    private Vehicle MapToVehicle(VehicleSearchDocument document)
    {
        return new Vehicle
        {
            Id = document.Id,
            Make = document.Make,
            Model = document.Model,
            Derivative = document.Derivative,
            Price = (decimal)document.Price,
            Mileage = document.Mileage,
            BodyType = document.BodyType,
            EngineSize = document.EngineSize.HasValue ? (decimal)document.EngineSize.Value : 0m,
            FuelType = document.FuelType,
            TransmissionType = document.TransmissionType,
            Colour = document.Colour,
            NumberOfDoors = document.NumberOfDoors,
            RegistrationDate = document.RegistrationDate != DateTimeOffset.MinValue 
                ? document.RegistrationDate.DateTime 
                : null,
            SaleLocation = document.SaleLocation,
            Channel = document.Channel,
            Features = document.Features.ToList(),
            Description = document.Description
        };
    }
}
