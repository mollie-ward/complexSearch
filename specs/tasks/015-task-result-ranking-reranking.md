# Task: Result Ranking & Re-ranking Logic

**Task ID:** 015  
**Feature:** Hybrid Search Orchestration  
**Type:** Backend Implementation  
**Priority:** High  
**Estimated Complexity:** Medium  
**FRD Reference:** FRD-004 (FR-4, FR-5, FR-6, FR-7)  
**GitHub Issue:** [#31](https://github.com/mollie-ward/complexSearch/issues/31)

---

## Description

Implement advanced result ranking and re-ranking logic to combine scores from multiple search strategies, apply business rules, and optimize result relevance using machine learning or rule-based approaches.

---

## Dependencies

**Depends on:**
- Task 014: Search Strategy Selection & Orchestration
- Task 011: Conceptual Understanding & Similarity Scoring

**Blocks:**
- Task 018: Search Interface Component (frontend needs ranked results)

---

## Technical Requirements

### Re-ranking Service Interface

```csharp
public interface IResultRankingService
{
    Task<List<VehicleResult>> RankResultsAsync(List<VehicleResult> results, ComposedQuery query);
    Task<List<VehicleResult>> RerankResultsAsync(List<VehicleResult> results, RerankingStrategy strategy);
    Task<double> ComputeBusinessScore(VehicleDocument vehicle, ComposedQuery query);
}

public class RerankingStrategy
{
    public RerankingApproach Approach { get; set; }
    public Dictionary<RankingFactor, double> FactorWeights { get; set; }
    public List<BusinessRule> BusinessRules { get; set; }
}

public enum RerankingApproach
{
    WeightedScore,      // Weighted combination of factors
    LearningToRank,     // ML-based ranking (future)
    BusinessRules,      // Rule-based reordering
    Hybrid              // Combination
}

public enum RankingFactor
{
    SemanticRelevance,
    ExactMatchCount,
    PriceCompetitiveness,
    VehicleCondition,
    Recency,
    Popularity,
    LocationProximity
}

public class BusinessRule
{
    public string Name { get; set; }
    public Func<VehicleDocument, bool> Condition { get; set; }
    public double ScoreAdjustment { get; set; }  // -1.0 to 1.0
}
```

### Weighted Score Ranking

**Combine multiple relevance signals:**

```csharp
public async Task<List<VehicleResult>> RankResultsAsync(
    List<VehicleResult> results,
    ComposedQuery query)
{
    var strategy = new RerankingStrategy
    {
        Approach = RerankingApproach.WeightedScore,
        FactorWeights = new Dictionary<RankingFactor, double>
        {
            [RankingFactor.SemanticRelevance] = 0.40,
            [RankingFactor.ExactMatchCount] = 0.25,
            [RankingFactor.PriceCompetitiveness] = 0.15,
            [RankingFactor.VehicleCondition] = 0.10,
            [RankingFactor.Recency] = 0.10
        }
    };
    
    foreach (var result in results)
    {
        var scores = new Dictionary<RankingFactor, double>();
        
        // Semantic relevance (from search)
        scores[RankingFactor.SemanticRelevance] = result.ScoreBreakdown.SemanticScore;
        
        // Exact match count
        scores[RankingFactor.ExactMatchCount] = ComputeExactMatchScore(result.Vehicle, query);
        
        // Price competitiveness
        scores[RankingFactor.PriceCompetitiveness] = ComputePriceScore(result.Vehicle, results);
        
        // Vehicle condition
        scores[RankingFactor.VehicleCondition] = ComputeConditionScore(result.Vehicle);
        
        // Recency
        scores[RankingFactor.Recency] = ComputeRecencyScore(result.Vehicle);
        
        // Weighted average
        var finalScore = strategy.FactorWeights
            .Sum(kvp => scores[kvp.Key] * kvp.Value);
        
        result.RelevanceScore = finalScore;
        result.ScoreBreakdown.FinalScore = finalScore;
    }
    
    return results.OrderByDescending(r => r.RelevanceScore).ToList();
}

private double ComputeExactMatchScore(VehicleDocument vehicle, ComposedQuery query)
{
    var exactConstraints = query.ConstraintGroups
        .SelectMany(g => g.Constraints)
        .Where(c => c.Type == ConstraintType.Exact)
        .ToList();
    
    if (!exactConstraints.Any()) return 0.5;  // Neutral if no exact constraints
    
    var matchCount = exactConstraints.Count(c => MatchesConstraint(vehicle, c));
    return (double)matchCount / exactConstraints.Count;
}

private bool MatchesConstraint(VehicleDocument vehicle, SearchConstraint constraint)
{
    var value = GetPropertyValue(vehicle, constraint.FieldName);
    
    return constraint.Operator switch
    {
        ConstraintOperator.Equals => value?.Equals(constraint.Value) == true,
        ConstraintOperator.LessThanOrEqual => Convert.ToDouble(value) <= Convert.ToDouble(constraint.Value),
        ConstraintOperator.GreaterThanOrEqual => Convert.ToDouble(value) >= Convert.ToDouble(constraint.Value),
        ConstraintOperator.Contains => value?.ToString()?.Contains(constraint.Value.ToString()) == true,
        _ => false
    };
}

private double ComputePriceScore(VehicleDocument vehicle, List<VehicleResult> allResults)
{
    // Lower price = higher score (assuming budget-conscious users)
    var prices = allResults.Select(r => r.Vehicle.Price).ToList();
    var minPrice = prices.Min();
    var maxPrice = prices.Max();
    
    if (maxPrice == minPrice) return 0.5;
    
    // Normalize: 1.0 for min price, 0.0 for max price
    return 1.0 - ((vehicle.Price - minPrice) / (maxPrice - minPrice));
}

private double ComputeConditionScore(VehicleDocument vehicle)
{
    var score = 0.0;
    
    // Service history
    if (vehicle.ServiceHistoryPresent == true) score += 0.3;
    
    // Low mileage (< 50k)
    if (vehicle.Mileage < 50000) score += 0.2;
    else if (vehicle.Mileage < 80000) score += 0.1;
    
    // MOT validity (> 3 months)
    var motDaysRemaining = (vehicle.MotExpiryDate - DateTime.Now).TotalDays;
    if (motDaysRemaining > 90) score += 0.2;
    else if (motDaysRemaining > 30) score += 0.1;
    
    // Few owners (≤ 2)
    if (vehicle.NumberOfPreviousOwners <= 2) score += 0.2;
    
    // No damage
    if (vehicle.DamagePresent == false) score += 0.1;
    
    return Math.Min(1.0, score);
}

private double ComputeRecencyScore(VehicleDocument vehicle)
{
    var age = DateTime.Now.Year - vehicle.RegistrationDate.Year;
    
    if (age <= 1) return 1.0;      // < 1 year
    if (age <= 3) return 0.8;      // 1-3 years
    if (age <= 5) return 0.6;      // 3-5 years
    if (age <= 10) return 0.4;     // 5-10 years
    return 0.2;                     // > 10 years
}
```

### Business Rules

**Apply domain-specific ranking adjustments:**

```csharp
private static readonly List<BusinessRule> DefaultBusinessRules = new()
{
    new BusinessRule
    {
        Name = "Boost Premium Makes",
        Condition = v => new[] { "BMW", "Mercedes-Benz", "Audi" }.Contains(v.Make),
        ScoreAdjustment = 0.05
    },
    new BusinessRule
    {
        Name = "Penalize High Mileage",
        Condition = v => v.Mileage > 100000,
        ScoreAdjustment = -0.15
    },
    new BusinessRule
    {
        Name = "Boost Full Service History",
        Condition = v => v.ServiceHistoryPresent == true,
        ScoreAdjustment = 0.10
    },
    new BusinessRule
    {
        Name = "Penalize Accident Damage",
        Condition = v => v.DamagePresent == true,
        ScoreAdjustment = -0.20
    },
    new BusinessRule
    {
        Name = "Boost Electric/Hybrid",
        Condition = v => new[] { "Electric", "Hybrid" }.Contains(v.FuelType),
        ScoreAdjustment = 0.08
    },
    new BusinessRule
    {
        Name = "Boost Near Expiry MOT Discount",
        Condition = v => (v.MotExpiryDate - DateTime.Now).TotalDays < 30,
        ScoreAdjustment = -0.10  // Potential negotiation leverage
    }
};

public async Task<List<VehicleResult>> ApplyBusinessRulesAsync(
    List<VehicleResult> results,
    List<BusinessRule> rules)
{
    foreach (var result in results)
    {
        var adjustment = 0.0;
        
        foreach (var rule in rules)
        {
            if (rule.Condition(result.Vehicle))
            {
                adjustment += rule.ScoreAdjustment;
                _logger.LogDebug(
                    "Applied rule '{RuleName}' to vehicle {VehicleId}: {Adjustment}",
                    rule.Name,
                    result.Vehicle.Id,
                    rule.ScoreAdjustment
                );
            }
        }
        
        // Apply adjustment (clamped to 0-1)
        result.RelevanceScore = Math.Clamp(result.RelevanceScore + adjustment, 0.0, 1.0);
    }
    
    return results.OrderByDescending(r => r.RelevanceScore).ToList();
}
```

### Reciprocal Rank Fusion (RRF)

**Combine results from multiple search strategies:**

```csharp
public List<VehicleResult> MergeWithRRF(
    List<VehicleResult> list1,
    List<VehicleResult> list2,
    double weight1 = 0.5,
    double weight2 = 0.5,
    int k = 60)
{
    var scores = new Dictionary<string, double>();
    
    // RRF formula: score = Σ (weight / (k + rank))
    for (int i = 0; i < list1.Count; i++)
    {
        var id = list1[i].Vehicle.Id;
        scores[id] = scores.GetValueOrDefault(id, 0) + weight1 / (k + i + 1);
    }
    
    for (int i = 0; i < list2.Count; i++)
    {
        var id = list2[i].Vehicle.Id;
        scores[id] = scores.GetValueOrDefault(id, 0) + weight2 / (k + i + 1);
    }
    
    // Merge and sort by RRF score
    var allVehicles = list1.Concat(list2)
        .DistinctBy(r => r.Vehicle.Id)
        .ToDictionary(r => r.Vehicle.Id);
    
    var merged = scores
        .OrderByDescending(kvp => kvp.Value)
        .Select(kvp =>
        {
            var result = allVehicles[kvp.Key];
            result.RelevanceScore = kvp.Value;
            return result;
        })
        .ToList();
    
    return merged;
}
```

### Diversity Enhancement

**Ensure result diversity (avoid showing 10 identical models):**

```csharp
public List<VehicleResult> EnsureDiversity(
    List<VehicleResult> results,
    int maxPerMake = 3,
    int maxPerModel = 2)
{
    var diverse = new List<VehicleResult>();
    var makeCount = new Dictionary<string, int>();
    var modelCount = new Dictionary<string, int>();
    
    foreach (var result in results.OrderByDescending(r => r.RelevanceScore))
    {
        var make = result.Vehicle.Make;
        var model = result.Vehicle.Model;
        
        var currentMakeCount = makeCount.GetValueOrDefault(make, 0);
        var currentModelCount = modelCount.GetValueOrDefault($"{make}:{model}", 0);
        
        if (currentMakeCount < maxPerMake && currentModelCount < maxPerModel)
        {
            diverse.Add(result);
            makeCount[make] = currentMakeCount + 1;
            modelCount[$"{make}:{model}"] = currentModelCount + 1;
        }
        
        if (diverse.Count >= 10) break;  // Limit to top 10
    }
    
    return diverse;
}
```

### API Endpoints

**POST /api/v1/search/rerank**

Request:
```json
{
  "results": [ /* vehicle results */ ],
  "strategy": {
    "approach": "WeightedScore",
    "factorWeights": {
      "SemanticRelevance": 0.4,
      "ExactMatchCount": 0.3,
      "VehicleCondition": 0.3
    }
  }
}
```

Response:
```json
{
  "results": [
    {
      "vehicle": { /* ... */ },
      "relevanceScore": 0.94,
      "scoreBreakdown": {
        "semanticScore": 0.89,
        "exactMatchScore": 1.0,
        "conditionScore": 0.95,
        "finalScore": 0.94
      }
    }
  ]
}
```

---

## Acceptance Criteria

### Functional Criteria

✅ **Weighted Ranking:**
- [ ] Multiple factors combined correctly
- [ ] Weights sum to 1.0
- [ ] Scores normalized 0-1
- [ ] Results ranked by final score

✅ **Business Rules:**
- [ ] All 6 default rules apply correctly
- [ ] Score adjustments work
- [ ] Rules logged for transparency

✅ **RRF Fusion:**
- [ ] Multiple result lists merged
- [ ] Weights respected
- [ ] No duplicate results

✅ **Diversity:**
- [ ] Max vehicles per make enforced
- [ ] Max vehicles per model enforced
- [ ] Diversity improves user experience

### Technical Criteria

✅ **Performance:**
- [ ] Re-ranking <100ms for 50 results
- [ ] Scales to 100+ results

✅ **Accuracy:**
- [ ] Ranking improves relevance (≥90% user satisfaction)
- [ ] Business rules work as expected

---

## Testing Requirements

### Unit Tests

**Test Coverage:** ≥85%

**Test Cases:**
```csharp
[Fact]
public async Task RankResults_MultipleFactors_CombinesCorrectly()

[Fact]
public async Task ComputeExactMatchScore_AllMatch_Returns1()

[Fact]
public async Task ComputePriceScore_CheapestVehicle_Returns1()

[Fact]
public async Task ComputeConditionScore_PerfectCondition_Returns1()

[Fact]
public async Task ApplyBusinessRules_MatchingCondition_AdjustsScore()

[Fact]
public async Task MergeWithRRF_TwoLists_MergesCorrectly()

[Fact]
public async Task EnsureDiversity_IdenticalMakes_LimitsPerMake()
```

### Integration Tests

- [ ] Test ranking with 30 real vehicles
- [ ] Compare ranking against manual evaluation
- [ ] Test diversity algorithm
- [ ] Test all business rules

---

## Definition of Done

- [ ] Result ranking service implemented
- [ ] Weighted scoring working
- [ ] Business rules applied
- [ ] RRF fusion functional
- [ ] Diversity enhancement working
- [ ] API endpoint functional
- [ ] All unit tests pass (≥85% coverage)
- [ ] Manual evaluation confirms improved relevance
- [ ] Documentation updated
- [ ] Code reviewed and approved

---

## Related Files

**Created/Modified:**
- `src/VehicleSearch.Infrastructure/Search/ResultRankingService.cs`
- `src/VehicleSearch.Infrastructure/Search/RRFMerger.cs`
- `src/VehicleSearch.Core/Interfaces/IResultRankingService.cs`
- `src/VehicleSearch.Core/Models/RerankingStrategy.cs`
- `src/VehicleSearch.Api/Controllers/SearchController.cs`
- `tests/VehicleSearch.Infrastructure.Tests/ResultRankingServiceTests.cs`

**References:**
- FRD-004: Hybrid Search Orchestration (FR-4, FR-5, FR-6, FR-7)
- Task 014: Search orchestration
- Task 011: Similarity scoring
