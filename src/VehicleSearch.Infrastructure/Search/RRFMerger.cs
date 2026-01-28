using Microsoft.Extensions.Logging;
using VehicleSearch.Core.Models;

namespace VehicleSearch.Infrastructure.Search;

/// <summary>
/// Service for merging multiple result sets using Reciprocal Rank Fusion (RRF).
/// </summary>
public class RRFMerger
{
    private readonly ILogger<RRFMerger> _logger;
    private const int DefaultK = 60;

    /// <summary>
    /// Initializes a new instance of the <see cref="RRFMerger"/> class.
    /// </summary>
    public RRFMerger(ILogger<RRFMerger> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Merges two result lists using Reciprocal Rank Fusion.
    /// </summary>
    /// <param name="list1">First result list.</param>
    /// <param name="list2">Second result list.</param>
    /// <param name="weight1">Weight for first list (default 0.5).</param>
    /// <param name="weight2">Weight for second list (default 0.5).</param>
    /// <param name="k">RRF constant (default 60).</param>
    /// <returns>Merged and deduplicated results.</returns>
    public List<VehicleResult> MergeWithRRF(
        List<VehicleResult> list1,
        List<VehicleResult> list2,
        double weight1 = 0.5,
        double weight2 = 0.5,
        int k = DefaultK)
    {
        if (list1 == null) throw new ArgumentNullException(nameof(list1));
        if (list2 == null) throw new ArgumentNullException(nameof(list2));

        _logger.LogInformation(
            "Merging {Count1} and {Count2} results using RRF (k={K}, weights={W1}/{W2})",
            list1.Count,
            list2.Count,
            k,
            weight1,
            weight2);

        var scores = new Dictionary<string, double>();
        var vehicleMap = new Dictionary<string, VehicleResult>();

        // Apply RRF formula: score = Σ (weight / (k + rank))
        for (int i = 0; i < list1.Count; i++)
        {
            var id = list1[i].Vehicle.Id;
            var rrfScore = weight1 / (k + i + 1);
            scores[id] = scores.GetValueOrDefault(id, 0) + rrfScore;
            vehicleMap[id] = list1[i];

            _logger.LogTrace(
                "List1[{Rank}]: {VehicleId} -> RRF score: {Score:F6}",
                i,
                id,
                rrfScore);
        }

        for (int i = 0; i < list2.Count; i++)
        {
            var id = list2[i].Vehicle.Id;
            var rrfScore = weight2 / (k + i + 1);
            scores[id] = scores.GetValueOrDefault(id, 0) + rrfScore;
            
            // Use result from list2 if not in list1
            if (!vehicleMap.ContainsKey(id))
            {
                vehicleMap[id] = list2[i];
            }

            _logger.LogTrace(
                "List2[{Rank}]: {VehicleId} -> RRF score: {Score:F6}, Total: {Total:F6}",
                i,
                id,
                rrfScore,
                scores[id]);
        }

        // Merge and sort by RRF score
        var merged = scores
            .OrderByDescending(kvp => kvp.Value)
            .Select(kvp =>
            {
                var result = vehicleMap[kvp.Key];
                result.Score = kvp.Value;
                return result;
            })
            .ToList();

        _logger.LogInformation(
            "RRF merge complete: {Original} unique vehicles from {Total} total",
            merged.Count,
            list1.Count + list2.Count);

        return merged;
    }

    /// <summary>
    /// Merges multiple result lists using Reciprocal Rank Fusion.
    /// </summary>
    /// <param name="lists">List of result lists with their weights.</param>
    /// <param name="k">RRF constant (default 60).</param>
    /// <returns>Merged and deduplicated results.</returns>
    public List<VehicleResult> MergeMultipleWithRRF(
        List<(List<VehicleResult> Results, double Weight)> lists,
        int k = DefaultK)
    {
        if (lists == null || !lists.Any())
        {
            return new List<VehicleResult>();
        }

        // Normalize weights
        var totalWeight = lists.Sum(l => l.Weight);
        var normalizedLists = lists
            .Select(l => (l.Results, Weight: l.Weight / totalWeight))
            .ToList();

        _logger.LogInformation(
            "Merging {ListCount} result lists using RRF (k={K})",
            lists.Count,
            k);

        var scores = new Dictionary<string, double>();
        var vehicleMap = new Dictionary<string, VehicleResult>();

        foreach (var (results, weight) in normalizedLists)
        {
            for (int i = 0; i < results.Count; i++)
            {
                var id = results[i].Vehicle.Id;
                var rrfScore = weight / (k + i + 1);
                scores[id] = scores.GetValueOrDefault(id, 0) + rrfScore;
                
                if (!vehicleMap.ContainsKey(id))
                {
                    vehicleMap[id] = results[i];
                }
            }
        }

        // Merge and sort by RRF score
        var merged = scores
            .OrderByDescending(kvp => kvp.Value)
            .Select(kvp =>
            {
                var result = vehicleMap[kvp.Key];
                result.Score = kvp.Value;
                return result;
            })
            .ToList();

        _logger.LogInformation(
            "Multi-list RRF merge complete: {Unique} unique vehicles",
            merged.Count);

        return merged;
    }
}
