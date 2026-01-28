using System.Diagnostics;
using Microsoft.Extensions.Logging;
using VehicleSearch.Core.Interfaces;
using VehicleSearch.Core.Models;

namespace VehicleSearch.Infrastructure.Search;

/// <summary>
/// Service for orchestrating search operations across multiple strategies.
/// </summary>
public class SearchOrchestratorService : ISearchOrchestratorService
{
    private readonly ExactSearchExecutor _exactExecutor;
    private readonly SemanticSearchExecutor _semanticExecutor;
    private readonly HybridSearchExecutor _hybridExecutor;
    private readonly ILogger<SearchOrchestratorService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SearchOrchestratorService"/> class.
    /// </summary>
    public SearchOrchestratorService(
        ExactSearchExecutor exactExecutor,
        SemanticSearchExecutor semanticExecutor,
        HybridSearchExecutor hybridExecutor,
        ILogger<SearchOrchestratorService> logger)
    {
        _exactExecutor = exactExecutor ?? throw new ArgumentNullException(nameof(exactExecutor));
        _semanticExecutor = semanticExecutor ?? throw new ArgumentNullException(nameof(semanticExecutor));
        _hybridExecutor = hybridExecutor ?? throw new ArgumentNullException(nameof(hybridExecutor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public Task<SearchStrategy> DetermineStrategyAsync(
        ComposedQuery query,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            // Extract constraint types
            var exactConstraints = query.ConstraintGroups
                .SelectMany(g => g.Constraints)
                .Where(c => c.Type == ConstraintType.Exact || c.Type == ConstraintType.Range)
                .ToList();

            var semanticConstraints = query.ConstraintGroups
                .SelectMany(g => g.Constraints)
                .Where(c => c.Type == ConstraintType.Semantic)
                .ToList();

            _logger.LogInformation(
                "Determining strategy: {ExactCount} exact, {SemanticCount} semantic constraints",
                exactConstraints.Count,
                semanticConstraints.Count);

            SearchStrategy strategy;

            // Decision tree based on constraint types
            if (semanticConstraints.Count == 0 && exactConstraints.Count > 0)
            {
                // Pure filtering - ExactOnly strategy
                _logger.LogInformation("Selected ExactOnly strategy (pure filtering)");
                strategy = new SearchStrategy
                {
                    Type = StrategyType.ExactOnly,
                    Approaches = new List<SearchApproach> { SearchApproach.ExactMatch },
                    Weights = new Dictionary<SearchApproach, double>
                    {
                        [SearchApproach.ExactMatch] = 1.0
                    },
                    ShouldRerank = false
                };
            }
            else if (exactConstraints.Count == 0 && semanticConstraints.Count > 0)
            {
                // Pure semantic search - SemanticOnly strategy
                _logger.LogInformation("Selected SemanticOnly strategy (pure vector search)");
                strategy = new SearchStrategy
                {
                    Type = StrategyType.SemanticOnly,
                    Approaches = new List<SearchApproach> { SearchApproach.SemanticSearch },
                    Weights = new Dictionary<SearchApproach, double>
                    {
                        [SearchApproach.SemanticSearch] = 1.0
                    },
                    ShouldRerank = false
                };
            }
            else if (exactConstraints.Count > 0 && semanticConstraints.Count > 0)
            {
                // Hybrid approach - combine exact and semantic
                // Calculate weights based on constraint counts
                // More exact constraints = higher exact weight (up to 70%)
                var exactWeight = Math.Min(0.7, exactConstraints.Count * 0.15);
                var semanticWeight = 1.0 - exactWeight;

                _logger.LogInformation(
                    "Selected Hybrid strategy (exact weight: {ExactWeight:F2}, semantic weight: {SemanticWeight:F2})",
                    exactWeight,
                    semanticWeight);

                strategy = new SearchStrategy
                {
                    Type = StrategyType.Hybrid,
                    Approaches = new List<SearchApproach>
                    {
                        SearchApproach.ExactMatch,
                        SearchApproach.SemanticSearch
                    },
                    Weights = new Dictionary<SearchApproach, double>
                    {
                        [SearchApproach.ExactMatch] = exactWeight,
                        [SearchApproach.SemanticSearch] = semanticWeight
                    },
                    ShouldRerank = true
                };
            }
            else
            {
                // Fallback: semantic search
                _logger.LogInformation("Selected SemanticOnly strategy (fallback - no constraints)");
                strategy = new SearchStrategy
                {
                    Type = StrategyType.SemanticOnly,
                    Approaches = new List<SearchApproach> { SearchApproach.SemanticSearch },
                    Weights = new Dictionary<SearchApproach, double>
                    {
                        [SearchApproach.SemanticSearch] = 1.0
                    },
                    ShouldRerank = false
                };
            }

            stopwatch.Stop();
            _logger.LogDebug("Strategy determined in {Duration}ms", stopwatch.ElapsedMilliseconds);

            return Task.FromResult(strategy);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error determining search strategy");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<SearchResults> ExecuteSearchAsync(
        ComposedQuery query,
        SearchStrategy strategy,
        int maxResults = 10,
        CancellationToken cancellationToken = default)
    {
        if (query == null)
        {
            throw new ArgumentNullException(nameof(query));
        }

        if (strategy == null)
        {
            throw new ArgumentNullException(nameof(strategy));
        }

        if (maxResults <= 0 || maxResults > 100)
        {
            throw new ArgumentException("MaxResults must be between 1 and 100.", nameof(maxResults));
        }

        _logger.LogInformation(
            "Executing search with {StrategyType} strategy, max results: {MaxResults}",
            strategy.Type,
            maxResults);

        try
        {
            return strategy.Type switch
            {
                StrategyType.ExactOnly => await _exactExecutor.ExecuteAsync(query, maxResults, cancellationToken),
                StrategyType.SemanticOnly => await _semanticExecutor.ExecuteAsync(query, maxResults, cancellationToken),
                StrategyType.Hybrid => await _hybridExecutor.ExecuteAsync(query, strategy, maxResults, cancellationToken),
                _ => throw new NotSupportedException($"Strategy type {strategy.Type} is not supported")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing search with strategy {StrategyType}", strategy.Type);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<SearchResults> ExecuteHybridSearchAsync(
        ComposedQuery query,
        int maxResults = 10,
        CancellationToken cancellationToken = default)
    {
        if (query == null)
        {
            throw new ArgumentNullException(nameof(query));
        }

        _logger.LogInformation("Executing hybrid search with auto-strategy determination");

        // Determine the optimal strategy
        var strategy = await DetermineStrategyAsync(query, cancellationToken);

        // If the determined strategy is hybrid, execute it
        // Otherwise, execute with the determined strategy
        return await ExecuteSearchAsync(query, strategy, maxResults, cancellationToken);
    }
}
