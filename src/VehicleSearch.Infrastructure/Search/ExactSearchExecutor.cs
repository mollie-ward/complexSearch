using System.Diagnostics;
using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Microsoft.Extensions.Logging;
using VehicleSearch.Core.Entities;
using VehicleSearch.Core.Models;

namespace VehicleSearch.Infrastructure.Search;

/// <summary>
/// Executor for exact match searches using OData filters.
/// </summary>
public class ExactSearchExecutor
{
    private readonly AzureSearchClient _searchClient;
    private readonly ILogger<ExactSearchExecutor> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExactSearchExecutor"/> class.
    /// </summary>
    public ExactSearchExecutor(
        AzureSearchClient searchClient,
        ILogger<ExactSearchExecutor> logger)
    {
        _searchClient = searchClient ?? throw new ArgumentNullException(nameof(searchClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Executes an exact match search using OData filters.
    /// </summary>
    public async Task<SearchResults> ExecuteAsync(
        ComposedQuery query,
        int maxResults,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Executing exact match search with {Count} constraint groups", query.ConstraintGroups.Count);

            // Use the OData filter from the composed query
            var odataFilter = query.ODataFilter;

            if (string.IsNullOrWhiteSpace(odataFilter))
            {
                _logger.LogWarning("No OData filter provided in composed query");
                return new SearchResults
                {
                    Results = new List<VehicleResult>(),
                    TotalCount = 0,
                    Strategy = new SearchStrategy { Type = StrategyType.ExactOnly },
                    SearchDuration = stopwatch.Elapsed
                };
            }

            var searchOptions = new SearchOptions
            {
                Filter = odataFilter,
                Size = maxResults,
                Select =
                {
                    "id", "make", "model", "derivative", "price", "mileage",
                    "bodyType", "engineSize", "fuelType", "transmissionType",
                    "colour", "numberOfDoors", "registrationDate", "saleLocation",
                    "channel", "features", "description"
                },
                OrderBy = { "price asc" } // Default ordering
            };

            _logger.LogDebug("Executing search with filter: {Filter}", odataFilter);

            var response = await _searchClient.Client.SearchAsync<VehicleSearchDocument>(
                null, // No text search for exact match
                searchOptions,
                cancellationToken);

            var results = new List<VehicleResult>();
            await foreach (var result in response.Value.GetResultsAsync())
            {
                var vehicle = MapToVehicle(result.Document);
                results.Add(new VehicleResult
                {
                    Vehicle = vehicle,
                    Score = 1.0, // All exact matches equally relevant
                    ScoreBreakdown = new SearchScoreBreakdown
                    {
                        ExactMatchScore = 1.0,
                        SemanticScore = 0.0,
                        KeywordScore = 0.0,
                        FinalScore = 1.0
                    }
                });
            }

            stopwatch.Stop();

            _logger.LogInformation(
                "Exact match search completed. Found {Count} results in {Duration}ms",
                results.Count,
                stopwatch.ElapsedMilliseconds);

            return new SearchResults
            {
                Results = results,
                TotalCount = results.Count,
                Strategy = new SearchStrategy
                {
                    Type = StrategyType.ExactOnly,
                    Approaches = new List<SearchApproach> { SearchApproach.ExactMatch },
                    Weights = new Dictionary<SearchApproach, double>
                    {
                        [SearchApproach.ExactMatch] = 1.0
                    },
                    ShouldRerank = false
                },
                SearchDuration = stopwatch.Elapsed,
                Metadata = new Dictionary<string, object>
                {
                    ["odataFilter"] = odataFilter,
                    ["constraintGroups"] = query.ConstraintGroups.Count
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
            _logger.LogError(ex, "Error executing exact match search");
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
