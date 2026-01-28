using Microsoft.Extensions.Logging;
using VehicleSearch.Core.Models;

namespace VehicleSearch.Infrastructure.Search;

/// <summary>
/// Service for enhancing result diversity to avoid showing too many similar vehicles.
/// </summary>
public class DiversityEnhancer
{
    private readonly ILogger<DiversityEnhancer> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DiversityEnhancer"/> class.
    /// </summary>
    public DiversityEnhancer(ILogger<DiversityEnhancer> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Ensures diversity in results by limiting vehicles per make and model.
    /// </summary>
    /// <param name="results">The results to diversify.</param>
    /// <param name="maxPerMake">Maximum vehicles per make (default 3).</param>
    /// <param name="maxPerModel">Maximum vehicles per model (default 2).</param>
    /// <param name="maxResults">Maximum total results to return (default no limit).</param>
    /// <returns>Diversified results.</returns>
    public List<VehicleResult> EnsureDiversity(
        List<VehicleResult> results,
        int maxPerMake = 3,
        int maxPerModel = 2,
        int? maxResults = null)
    {
        if (results == null || !results.Any())
        {
            return new List<VehicleResult>();
        }

        _logger.LogInformation(
            "Applying diversity enhancement to {Count} results (max per make: {MaxMake}, max per model: {MaxModel})",
            results.Count,
            maxPerMake,
            maxPerModel);

        var diverse = new List<VehicleResult>();
        var makeCount = new Dictionary<string, int>();
        var modelCount = new Dictionary<string, int>();

        // Process results in order of relevance score
        foreach (var result in results.OrderByDescending(r => r.Score))
        {
            var make = result.Vehicle.Make;
            var model = result.Vehicle.Model;
            var modelKey = $"{make}:{model}";

            var currentMakeCount = makeCount.GetValueOrDefault(make, 0);
            var currentModelCount = modelCount.GetValueOrDefault(modelKey, 0);

            // Check if we can add this vehicle without violating diversity constraints
            if (currentMakeCount < maxPerMake && currentModelCount < maxPerModel)
            {
                diverse.Add(result);
                makeCount[make] = currentMakeCount + 1;
                modelCount[modelKey] = currentModelCount + 1;

                _logger.LogTrace(
                    "Added {VehicleId} ({Make} {Model}): Make count={MakeCount}, Model count={ModelCount}",
                    result.Vehicle.Id,
                    make,
                    model,
                    makeCount[make],
                    modelCount[modelKey]);

                // Stop if we've reached the maximum number of results
                if (maxResults.HasValue && diverse.Count >= maxResults.Value)
                {
                    break;
                }
            }
            else
            {
                _logger.LogTrace(
                    "Skipped {VehicleId} ({Make} {Model}) due to diversity constraints",
                    result.Vehicle.Id,
                    make,
                    model);
            }
        }

        _logger.LogInformation(
            "Diversity enhancement complete: {Original} -> {Diverse} results",
            results.Count,
            diverse.Count);

        // Log diversity statistics
        var makeDistribution = diverse
            .GroupBy(r => r.Vehicle.Make)
            .Select(g => new { Make = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(5)
            .ToList();

        if (makeDistribution.Any())
        {
            _logger.LogDebug(
                "Top makes in results: {Distribution}",
                string.Join(", ", makeDistribution.Select(x => $"{x.Make}({x.Count})")));
        }

        return diverse;
    }

    /// <summary>
    /// Analyzes diversity of results without modifying them.
    /// </summary>
    /// <param name="results">The results to analyze.</param>
    /// <returns>Diversity statistics.</returns>
    public DiversityStats AnalyzeDiversity(List<VehicleResult> results)
    {
        if (results == null || !results.Any())
        {
            return new DiversityStats();
        }

        var makeGroups = results.GroupBy(r => r.Vehicle.Make).ToList();
        var modelGroups = results.GroupBy(r => $"{r.Vehicle.Make}:{r.Vehicle.Model}").ToList();

        var stats = new DiversityStats
        {
            TotalResults = results.Count,
            UniqueMakes = makeGroups.Count,
            UniqueModels = modelGroups.Count,
            MaxVehiclesPerMake = makeGroups.Max(g => g.Count()),
            MaxVehiclesPerModel = modelGroups.Max(g => g.Count()),
            AverageVehiclesPerMake = makeGroups.Average(g => g.Count()),
            AverageVehiclesPerModel = modelGroups.Average(g => g.Count())
        };

        _logger.LogDebug(
            "Diversity analysis: {Total} results, {Makes} makes, {Models} models",
            stats.TotalResults,
            stats.UniqueMakes,
            stats.UniqueModels);

        return stats;
    }
}

/// <summary>
/// Statistics about result diversity.
/// </summary>
public class DiversityStats
{
    /// <summary>
    /// Gets or sets the total number of results.
    /// </summary>
    public int TotalResults { get; set; }

    /// <summary>
    /// Gets or sets the number of unique makes.
    /// </summary>
    public int UniqueMakes { get; set; }

    /// <summary>
    /// Gets or sets the number of unique models.
    /// </summary>
    public int UniqueModels { get; set; }

    /// <summary>
    /// Gets or sets the maximum vehicles from a single make.
    /// </summary>
    public int MaxVehiclesPerMake { get; set; }

    /// <summary>
    /// Gets or sets the maximum vehicles from a single model.
    /// </summary>
    public int MaxVehiclesPerModel { get; set; }

    /// <summary>
    /// Gets or sets the average vehicles per make.
    /// </summary>
    public double AverageVehiclesPerMake { get; set; }

    /// <summary>
    /// Gets or sets the average vehicles per model.
    /// </summary>
    public double AverageVehiclesPerModel { get; set; }
}
