namespace VehicleSearch.Core.Models;

/// <summary>
/// Represents the selected search strategy with approaches and weights.
/// </summary>
public class SearchStrategy
{
    /// <summary>
    /// Gets or sets the type of search strategy.
    /// </summary>
    public StrategyType Type { get; set; }

    /// <summary>
    /// Gets or sets the list of search approaches to use.
    /// </summary>
    public List<SearchApproach> Approaches { get; set; } = new();

    /// <summary>
    /// Gets or sets the weights for each search approach.
    /// </summary>
    public Dictionary<SearchApproach, double> Weights { get; set; } = new();

    /// <summary>
    /// Gets or sets whether re-ranking should be applied.
    /// </summary>
    public bool ShouldRerank { get; set; }
}

/// <summary>
/// Represents the type of search strategy.
/// </summary>
public enum StrategyType
{
    /// <summary>
    /// Pure filtering with exact match constraints.
    /// </summary>
    ExactOnly,

    /// <summary>
    /// Pure vector/semantic search.
    /// </summary>
    SemanticOnly,

    /// <summary>
    /// Combined exact and semantic search with RRF fusion.
    /// </summary>
    Hybrid,

    /// <summary>
    /// Sequential refinement with multiple stages.
    /// </summary>
    MultiStage
}

/// <summary>
/// Represents different search approaches that can be combined.
/// </summary>
public enum SearchApproach
{
    /// <summary>
    /// Structured filtering with exact match.
    /// </summary>
    ExactMatch,

    /// <summary>
    /// Vector/embedding-based semantic search.
    /// </summary>
    SemanticSearch,

    /// <summary>
    /// Keyword-based full-text search.
    /// </summary>
    FullTextSearch,

    /// <summary>
    /// Faceted navigation search.
    /// </summary>
    Faceted
}
