namespace VehicleSearch.Core.Models;

/// <summary>
/// Strategy configuration for re-ranking results.
/// </summary>
public class RerankingStrategy
{
    /// <summary>
    /// Gets or sets the re-ranking approach.
    /// </summary>
    public RerankingApproach Approach { get; set; }

    /// <summary>
    /// Gets or sets the weights for each ranking factor (must sum to 1.0).
    /// </summary>
    public Dictionary<RankingFactor, double> FactorWeights { get; set; } = new();

    /// <summary>
    /// Gets or sets the business rules to apply.
    /// </summary>
    public List<BusinessRule> BusinessRules { get; set; } = new();

    /// <summary>
    /// Gets or sets whether to apply diversity enhancement.
    /// </summary>
    public bool ApplyDiversity { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum vehicles per make.
    /// </summary>
    public int MaxPerMake { get; set; } = 3;

    /// <summary>
    /// Gets or sets the maximum vehicles per model.
    /// </summary>
    public int MaxPerModel { get; set; } = 2;
}
