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
/// Executor for semantic searches using vector embeddings.
/// </summary>
public class SemanticSearchExecutor
{
    private readonly IEmbeddingService _embeddingService;
    private readonly AzureSearchClient _searchClient;
    private readonly ILogger<SemanticSearchExecutor> _logger;

    private const double MinimumRelevanceScore = 0.50;

    /// <summary>
    /// Initializes a new instance of the <see cref="SemanticSearchExecutor"/> class.
    /// </summary>
    public SemanticSearchExecutor(
        IEmbeddingService embeddingService,
        AzureSearchClient searchClient,
        ILogger<SemanticSearchExecutor> logger)
    {
        _embeddingService = embeddingService ?? throw new ArgumentNullException(nameof(embeddingService));
        _searchClient = searchClient ?? throw new ArgumentNullException(nameof(searchClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Executes a semantic search using vector embeddings.
    /// </summary>
    public async Task<SearchResults> ExecuteAsync(
        ComposedQuery query,
        int maxResults,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Extract semantic constraints to build the semantic query
            var semanticConstraints = query.ConstraintGroups
                .SelectMany(g => g.Constraints)
                .Where(c => c.Type == ConstraintType.Semantic)
                .ToList();

            if (!semanticConstraints.Any())
            {
                _logger.LogWarning("No semantic constraints found in query");
                return new SearchResults
                {
                    Results = new List<VehicleResult>(),
                    TotalCount = 0,
                    Strategy = new SearchStrategy { Type = StrategyType.SemanticOnly },
                    SearchDuration = stopwatch.Elapsed
                };
            }

            // Build semantic query from constraints
            var semanticQuery = string.Join(" ", semanticConstraints.Select(c => c.Value?.ToString() ?? ""));
            _logger.LogInformation("Executing semantic search for query: {Query}", semanticQuery);

            // Prepare and enrich the query
            var enrichedQuery = AI.CachedEmbeddingService.PrepareQueryForEmbedding(semanticQuery);
            _logger.LogDebug("Enriched query: {EnrichedQuery}", enrichedQuery);

            // Generate embedding for the query
            var embedding = await _embeddingService.GenerateEmbeddingAsync(enrichedQuery, cancellationToken);
            _logger.LogDebug("Generated embedding with {Dimensions} dimensions", embedding.Length);

            // Extract exact constraints for filtering (if any)
            var exactConstraints = query.ConstraintGroups
                .SelectMany(g => g.Constraints)
                .Where(c => c.Type == ConstraintType.Exact || c.Type == ConstraintType.Range)
                .ToList();

            // Build vector search options
            var vectorQuery = new VectorizedQuery(embedding)
            {
                KNearestNeighborsCount = maxResults * 3, // Overquery for filtering
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

            // Add exact filters if any
            if (exactConstraints.Any() && !string.IsNullOrWhiteSpace(query.ODataFilter))
            {
                searchOptions.Filter = query.ODataFilter;
                _logger.LogDebug("Applied filter: {Filter}", query.ODataFilter);
            }

            // Execute vector search
            var response = await _searchClient.Client.SearchAsync<VehicleSearchDocument>(
                null, // No text search, only vector
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
                results.Add(new VehicleResult
                {
                    Vehicle = vehicle,
                    Score = score,
                    ScoreBreakdown = new SearchScoreBreakdown
                    {
                        ExactMatchScore = 0.0,
                        SemanticScore = score,
                        KeywordScore = 0.0,
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
                "Semantic search completed. Found {Count} matches with average score {AvgScore:F3} in {Duration}ms",
                results.Count,
                averageScore,
                stopwatch.ElapsedMilliseconds);

            return new SearchResults
            {
                Results = results,
                TotalCount = results.Count,
                Strategy = new SearchStrategy
                {
                    Type = StrategyType.SemanticOnly,
                    Approaches = new List<SearchApproach> { SearchApproach.SemanticSearch },
                    Weights = new Dictionary<SearchApproach, double>
                    {
                        [SearchApproach.SemanticSearch] = 1.0
                    },
                    ShouldRerank = false
                },
                SearchDuration = stopwatch.Elapsed,
                Metadata = new Dictionary<string, object>
                {
                    ["semanticQuery"] = semanticQuery,
                    ["averageScore"] = averageScore,
                    ["semanticConstraints"] = semanticConstraints.Count
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
            _logger.LogError(ex, "Error executing semantic search");
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
