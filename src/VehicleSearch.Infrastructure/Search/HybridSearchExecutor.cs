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
/// Executor for hybrid searches combining exact and semantic approaches using RRF.
/// </summary>
public class HybridSearchExecutor
{
    private readonly IEmbeddingService _embeddingService;
    private readonly AzureSearchClient _searchClient;
    private readonly ILogger<HybridSearchExecutor> _logger;

    private const double MinimumRelevanceScore = 0.50;

    /// <summary>
    /// Initializes a new instance of the <see cref="HybridSearchExecutor"/> class.
    /// </summary>
    public HybridSearchExecutor(
        IEmbeddingService embeddingService,
        AzureSearchClient searchClient,
        ILogger<HybridSearchExecutor> logger)
    {
        _embeddingService = embeddingService ?? throw new ArgumentNullException(nameof(embeddingService));
        _searchClient = searchClient ?? throw new ArgumentNullException(nameof(searchClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Executes a hybrid search combining exact and semantic approaches.
    /// </summary>
    public async Task<SearchResults> ExecuteAsync(
        ComposedQuery query,
        SearchStrategy strategy,
        int maxResults,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Extract semantic and exact constraints
            var semanticConstraints = query.ConstraintGroups
                .SelectMany(g => g.Constraints)
                .Where(c => c.Type == ConstraintType.Semantic)
                .ToList();

            var exactConstraints = query.ConstraintGroups
                .SelectMany(g => g.Constraints)
                .Where(c => c.Type == ConstraintType.Exact || c.Type == ConstraintType.Range)
                .ToList();

            if (!semanticConstraints.Any() || !exactConstraints.Any())
            {
                _logger.LogWarning("Hybrid search requires both semantic and exact constraints");
                return new SearchResults
                {
                    Results = new List<VehicleResult>(),
                    TotalCount = 0,
                    Strategy = strategy,
                    SearchDuration = stopwatch.Elapsed
                };
            }

            // Build semantic query
            var semanticQuery = string.Join(" ", semanticConstraints.Select(c => c.Value?.ToString() ?? ""));
            _logger.LogInformation(
                "Executing hybrid search with {ExactCount} exact and {SemanticCount} semantic constraints",
                exactConstraints.Count,
                semanticConstraints.Count);

            // Prepare and enrich the query
            var enrichedQuery = AI.CachedEmbeddingService.PrepareQueryForEmbedding(semanticQuery);
            _logger.LogDebug("Enriched query: {EnrichedQuery}", enrichedQuery);

            // Generate embedding for the semantic component
            var embedding = await _embeddingService.GenerateEmbeddingAsync(enrichedQuery, cancellationToken);
            _logger.LogDebug("Generated embedding with {Dimensions} dimensions", embedding.Length);

            // Build vector search options
            var vectorQuery = new VectorizedQuery(embedding)
            {
                KNearestNeighborsCount = maxResults * 3,
                Fields = { "descriptionVector" }
            };

            var searchOptions = new SearchOptions
            {
                Size = maxResults * 3,
                Select =
                {
                    "id", "make", "model", "derivative", "price", "mileage",
                    "bodyType", "engineSize", "fuelType", "transmissionType",
                    "colour", "numberOfDoors", "registrationDate", "saleLocation",
                    "channel", "features", "description"
                }
            };

            searchOptions.VectorSearch = new VectorSearchOptions
            {
                Queries = { vectorQuery }
            };

            // Add exact filters from OData filter
            if (!string.IsNullOrWhiteSpace(query.ODataFilter))
            {
                searchOptions.Filter = query.ODataFilter;
                _logger.LogDebug("Applied filter: {Filter}", query.ODataFilter);
            }

            // Execute hybrid search - Azure Search handles RRF automatically
            // When both text search and vector search are provided, Azure AI Search
            // uses Reciprocal Rank Fusion (RRF) to combine the results
            var response = await _searchClient.Client.SearchAsync<VehicleSearchDocument>(
                semanticQuery, // Text search for keyword matching
                searchOptions,
                cancellationToken);

            var results = new List<VehicleResult>();
            await foreach (var result in response.Value.GetResultsAsync())
            {
                var score = result.Score ?? 0.0;

                // Filter by minimum relevance score
                if (score < MinimumRelevanceScore)
                {
                    _logger.LogDebug(
                        "Skipping result {VehicleId} with score {Score:F3} (below minimum {MinScore:F3})",
                        result.Document.Id,
                        score,
                        MinimumRelevanceScore);
                    continue;
                }

                var vehicle = MapToVehicle(result.Document);

                // Get weights from strategy
                var exactWeight = strategy.Weights.TryGetValue(SearchApproach.ExactMatch, out var ew) ? ew : 0.6;
                var semanticWeight = strategy.Weights.TryGetValue(SearchApproach.SemanticSearch, out var sw) ? sw : 0.4;

                results.Add(new VehicleResult
                {
                    Vehicle = vehicle,
                    Score = score,
                    ScoreBreakdown = new SearchScoreBreakdown
                    {
                        ExactMatchScore = exactWeight,
                        SemanticScore = score, // Combined score from Azure RRF
                        KeywordScore = score,
                        FinalScore = score
                    }
                });

                // Stop if we have enough results
                if (results.Count >= maxResults)
                {
                    break;
                }
            }

            stopwatch.Stop();

            var averageScore = results.Any() ? results.Average(r => r.Score) : 0.0;

            _logger.LogInformation(
                "Hybrid search completed. Found {Count} matches with average score {AvgScore:F3} in {Duration}ms",
                results.Count,
                averageScore,
                stopwatch.ElapsedMilliseconds);

            return new SearchResults
            {
                Results = results,
                TotalCount = results.Count,
                Strategy = strategy,
                SearchDuration = stopwatch.Elapsed,
                Metadata = new Dictionary<string, object>
                {
                    ["semanticQuery"] = semanticQuery,
                    ["averageScore"] = averageScore,
                    ["exactConstraints"] = exactConstraints.Count,
                    ["semanticConstraints"] = semanticConstraints.Count,
                    ["hybridWeights"] = strategy.Weights
                }
            };
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Azure Search request failed with status {Status}", ex.Status);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing hybrid search");
            throw;
        }
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
