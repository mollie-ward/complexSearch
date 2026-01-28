# Task: Conceptual Understanding & Similarity Scoring

**Task ID:** 011  
**Feature:** Semantic Search Engine  
**Type:** Backend Implementation  
**Priority:** Medium  
**Estimated Complexity:** High  
**FRD Reference:** FRD-002 (FR-3, FR-4, FR-5, FR-6)  
**GitHub Issue:** [#23](https://github.com/mollie-ward/complexSearch/issues/23)

---

## Description

Implement advanced conceptual understanding to map qualitative terms ("reliable", "economical") to measurable vehicle attributes, compute multi-factor similarity scores, and provide explainable relevance scoring.

---

## Dependencies

**Depends on:**
- Task 010: Query Embedding & Semantic Matching
- Task 008: Attribute Mapping (for qualitative defaults)

**Blocks:**
- Task 015: Result Ranking & Re-ranking Logic
- Task 014: Search Strategy Selection & Orchestration

---

## Technical Requirements

### Conceptual Mapper Service Interface

```csharp
public interface IConceptualMapperService
{
    Task<ConceptualMapping> MapConceptToAttributesAsync(string concept);
    Task<SimilarityScore> ComputeSimilarityAsync(VehicleDocument vehicle, ConceptualMapping concept);
    Task<ExplainedScore> ExplainRelevanceAsync(VehicleDocument vehicle, ParsedQuery query);
}

public class ConceptualMapping
{
    public string Concept { get; set; }
    public List<AttributeWeight> AttributeWeights { get; set; }
    public List<string> PositiveIndicators { get; set; }
    public List<string> NegativeIndicators { get; set; }
}

public class AttributeWeight
{
    public string Attribute { get; set; }
    public double Weight { get; set; }        // 0-1
    public object TargetValue { get; set; }
    public string ComparisonType { get; set; } // "greater", "less", "equals", "contains"
}

public class SimilarityScore
{
    public double OverallScore { get; set; }  // 0-1
    public Dictionary<string, double> ComponentScores { get; set; }
    public List<string> MatchingAttributes { get; set; }
    public List<string> MismatchingAttributes { get; set; }
}

public class ExplainedScore
{
    public double Score { get; set; }
    public string Explanation { get; set; }
    public List<ScoreComponent> Components { get; set; }
}

public class ScoreComponent
{
    public string Factor { get; set; }
    public double Score { get; set; }
    public double Weight { get; set; }
    public string Reason { get; set; }
}
```

### Concept-to-Attribute Mappings

**Qualitative Concept Library:**

```csharp
private static readonly Dictionary<string, ConceptualMapping> ConceptMappings = new()
{
    ["reliable"] = new ConceptualMapping
    {
        Concept = "reliable",
        AttributeWeights = new List<AttributeWeight>
        {
            new() { Attribute = "mileage", Weight = 0.3, TargetValue = 60000, ComparisonType = "less" },
            new() { Attribute = "serviceHistoryPresent", Weight = 0.3, TargetValue = true, ComparisonType = "equals" },
            new() { Attribute = "numberOfPreviousOwners", Weight = 0.2, TargetValue = 2, ComparisonType = "less" },
            new() { Attribute = "motExpiryDate", Weight = 0.2, TargetValue = 90, ComparisonType = "daysFromNow_greater" }
        },
        PositiveIndicators = new() { "full service history", "one owner", "low mileage", "warranty" },
        NegativeIndicators = new() { "accident damage", "high mileage", "no service history" }
    },
    
    ["economical"] = new ConceptualMapping
    {
        Concept = "economical",
        AttributeWeights = new List<AttributeWeight>
        {
            new() { Attribute = "fuelType", Weight = 0.4, TargetValue = new[] { "Electric", "Hybrid", "Petrol" }, ComparisonType = "in" },
            new() { Attribute = "engineSize", Weight = 0.3, TargetValue = 2.0, ComparisonType = "less" },
            new() { Attribute = "price", Weight = 0.3, TargetValue = 20000, ComparisonType = "less" }
        },
        PositiveIndicators = new() { "low tax", "cheap to run", "fuel efficient", "hybrid", "electric" },
        NegativeIndicators = new() { "v8", "v6", "high performance", "sports" }
    },
    
    ["family car"] = new ConceptualMapping
    {
        Concept = "family car",
        AttributeWeights = new List<AttributeWeight>
        {
            new() { Attribute = "numberOfDoors", Weight = 0.3, TargetValue = 5, ComparisonType = "greater_equal" },
            new() { Attribute = "numberOfSeats", Weight = 0.3, TargetValue = 5, ComparisonType = "greater_equal" },
            new() { Attribute = "bodyType", Weight = 0.4, TargetValue = new[] { "SUV", "MPV", "Estate", "Hatchback" }, ComparisonType = "in" }
        },
        PositiveIndicators = new() { "spacious", "practical", "boot space", "child seats" },
        NegativeIndicators = new() { "2-door", "coupe", "sports car" }
    },
    
    ["sporty"] = new ConceptualMapping
    {
        Concept = "sporty",
        AttributeWeights = new List<AttributeWeight>
        {
            new() { Attribute = "engineSize", Weight = 0.4, TargetValue = 2.0, ComparisonType = "greater" },
            new() { Attribute = "bodyType", Weight = 0.3, TargetValue = new[] { "Coupe", "Convertible", "Hatchback" }, ComparisonType = "in" },
            new() { Attribute = "transmission", Weight = 0.3, TargetValue = "Manual", ComparisonType = "equals" }
        },
        PositiveIndicators = new() { "turbo", "performance", "sport", "fast", "alloy wheels" },
        NegativeIndicators = new() { "economical", "fuel efficient", "mpv" }
    },
    
    ["luxury"] = new ConceptualMapping
    {
        Concept = "luxury",
        AttributeWeights = new List<AttributeWeight>
        {
            new() { Attribute = "price", Weight = 0.3, TargetValue = 30000, ComparisonType = "greater" },
            new() { Attribute = "make", Weight = 0.4, TargetValue = new[] { "BMW", "Mercedes-Benz", "Audi", "Jaguar", "Lexus" }, ComparisonType = "in" }
        },
        PositiveIndicators = new() { "leather", "navigation", "premium sound", "heated seats", "sunroof" },
        NegativeIndicators = new() { "basic", "standard", "budget" }
    },
    
    ["practical"] = new ConceptualMapping
    {
        Concept = "practical",
        AttributeWeights = new List<AttributeWeight>
        {
            new() { Attribute = "bodyType", Weight = 0.4, TargetValue = new[] { "Estate", "MPV", "SUV", "Hatchback" }, ComparisonType = "in" },
            new() { Attribute = "numberOfDoors", Weight = 0.3, TargetValue = 4, ComparisonType = "greater_equal" },
            new() { Attribute = "fuelType", Weight = 0.3, TargetValue = new[] { "Diesel", "Hybrid", "Petrol" }, ComparisonType = "in" }
        },
        PositiveIndicators = new() { "boot space", "storage", "versatile", "mpv" },
        NegativeIndicators = new() { "coupe", "sports car", "2-door" }
    }
};
```

### Multi-Factor Similarity Computation

**Weighted scoring algorithm:**

```csharp
public async Task<SimilarityScore> ComputeSimilarityAsync(
    VehicleDocument vehicle,
    ConceptualMapping concept)
{
    var componentScores = new Dictionary<string, double>();
    var matchingAttributes = new List<string>();
    var mismatchingAttributes = new List<string>();
    
    foreach (var weight in concept.AttributeWeights)
    {
        var attributeScore = ComputeAttributeScore(vehicle, weight);
        componentScores[weight.Attribute] = attributeScore;
        
        if (attributeScore >= 0.7)
            matchingAttributes.Add(weight.Attribute);
        else if (attributeScore < 0.3)
            mismatchingAttributes.Add(weight.Attribute);
    }
    
    // Weighted average
    var overallScore = concept.AttributeWeights
        .Sum(w => componentScores[w.Attribute] * w.Weight);
    
    // Boost for positive indicators in description
    var descriptionBoost = ComputeDescriptionBoost(vehicle, concept);
    overallScore = Math.Min(1.0, overallScore + descriptionBoost);
    
    return new SimilarityScore
    {
        OverallScore = overallScore,
        ComponentScores = componentScores,
        MatchingAttributes = matchingAttributes,
        MismatchingAttributes = mismatchingAttributes
    };
}

private double ComputeAttributeScore(VehicleDocument vehicle, AttributeWeight weight)
{
    var value = GetAttributeValue(vehicle, weight.Attribute);
    
    return weight.ComparisonType switch
    {
        "less" => ComputeLessScore(value, weight.TargetValue),
        "greater" => ComputeGreaterScore(value, weight.TargetValue),
        "equals" => value?.Equals(weight.TargetValue) == true ? 1.0 : 0.0,
        "in" => ((IEnumerable<object>)weight.TargetValue).Contains(value) ? 1.0 : 0.0,
        "contains" => value?.ToString()?.Contains(weight.TargetValue.ToString()) == true ? 1.0 : 0.0,
        _ => 0.0
    };
}

private double ComputeLessScore(object actual, object target)
{
    if (actual == null) return 0.0;
    var actualNum = Convert.ToDouble(actual);
    var targetNum = Convert.ToDouble(target);
    
    if (actualNum <= targetNum * 0.7) return 1.0;  // Well below target
    if (actualNum <= targetNum) return 0.8;        // At target
    if (actualNum <= targetNum * 1.3) return 0.5;  // Slightly above
    return 0.2;                                     // Far above
}

private double ComputeDescriptionBoost(VehicleDocument vehicle, ConceptualMapping concept)
{
    var description = vehicle.Description?.ToLower() ?? "";
    var positiveCount = concept.PositiveIndicators.Count(ind => description.Contains(ind.ToLower()));
    var negativeCount = concept.NegativeIndicators.Count(ind => description.Contains(ind.ToLower()));
    
    return (positiveCount * 0.05) - (negativeCount * 0.1);  // Max boost: ±0.5
}
```

### Explainable Relevance Scoring

**Generate human-readable explanations:**

```csharp
public async Task<ExplainedScore> ExplainRelevanceAsync(
    VehicleDocument vehicle,
    ParsedQuery query)
{
    var components = new List<ScoreComponent>();
    var totalScore = 0.0;
    var totalWeight = 0.0;
    
    // Exact match score
    foreach (var entity in query.Entities.Where(e => e.Type != EntityType.QualitativeTerm))
    {
        var score = ComputeExactMatchScore(vehicle, entity);
        var weight = 0.4;
        components.Add(new ScoreComponent
        {
            Factor = $"{entity.Type} Match",
            Score = score,
            Weight = weight,
            Reason = score > 0.8 
                ? $"Exact match for {entity.Value}" 
                : $"Partial match for {entity.Value}"
        });
        totalScore += score * weight;
        totalWeight += weight;
    }
    
    // Semantic/conceptual score
    foreach (var concept in query.Entities.Where(e => e.Type == EntityType.QualitativeTerm))
    {
        if (ConceptMappings.TryGetValue(concept.Value.ToLower(), out var mapping))
        {
            var similarity = await ComputeSimilarityAsync(vehicle, mapping);
            var weight = 0.3;
            components.Add(new ScoreComponent
            {
                Factor = $"Conceptual: {concept.Value}",
                Score = similarity.OverallScore,
                Weight = weight,
                Reason = GenerateConceptReason(similarity, mapping)
            });
            totalScore += similarity.OverallScore * weight;
            totalWeight += weight;
        }
    }
    
    // Vector similarity score (if available)
    if (query.Metadata.ContainsKey("VectorScore"))
    {
        var vectorScore = (double)query.Metadata["VectorScore"];
        var weight = 0.3;
        components.Add(new ScoreComponent
        {
            Factor = "Semantic Similarity",
            Score = vectorScore,
            Weight = weight,
            Reason = $"Description semantically similar (score: {vectorScore:F2})"
        });
        totalScore += vectorScore * weight;
        totalWeight += weight;
    }
    
    var finalScore = totalWeight > 0 ? totalScore / totalWeight : 0.0;
    
    return new ExplainedScore
    {
        Score = finalScore,
        Explanation = GenerateOverallExplanation(components, finalScore),
        Components = components
    };
}

private string GenerateConceptReason(SimilarityScore similarity, ConceptualMapping mapping)
{
    if (similarity.OverallScore >= 0.8)
        return $"Strongly matches '{mapping.Concept}' criteria: {string.Join(", ", similarity.MatchingAttributes)}";
    if (similarity.OverallScore >= 0.6)
        return $"Partially matches '{mapping.Concept}' criteria";
    return $"Weakly matches '{mapping.Concept}' criteria";
}
```

### API Endpoints

**POST /api/v1/search/similarity**

Request:
```json
{
  "vehicleId": "V001",
  "concept": "reliable"
}
```

Response:
```json
{
  "overallScore": 0.87,
  "componentScores": {
    "mileage": 0.95,
    "serviceHistoryPresent": 1.0,
    "numberOfPreviousOwners": 0.8,
    "motExpiryDate": 0.75
  },
  "matchingAttributes": ["mileage", "serviceHistoryPresent", "motExpiryDate"],
  "mismatchingAttributes": []
}
```

**POST /api/v1/search/explain**

Response:
```json
{
  "score": 0.89,
  "explanation": "This vehicle strongly matches your search for a reliable BMW under £20,000",
  "components": [
    {
      "factor": "Make Match",
      "score": 1.0,
      "weight": 0.4,
      "reason": "Exact match for BMW"
    },
    {
      "factor": "Conceptual: reliable",
      "score": 0.87,
      "weight": 0.3,
      "reason": "Strongly matches 'reliable' criteria: mileage, serviceHistoryPresent, motExpiryDate"
    },
    {
      "factor": "Semantic Similarity",
      "score": 0.82,
      "weight": 0.3,
      "reason": "Description semantically similar (score: 0.82)"
    }
  ]
}
```

---

## Acceptance Criteria

### Functional Criteria

✅ **Conceptual Mapping:**
- [ ] All 6 core concepts mapped ("reliable", "economical", "family car", "sporty", "luxury", "practical")
- [ ] Each concept has 2-4 weighted attributes
- [ ] Positive/negative indicators defined

✅ **Similarity Scoring:**
- [ ] Multi-factor scores computed accurately
- [ ] Weights sum to 1.0 per concept
- [ ] Description boost applied (±0.5)
- [ ] Overall scores 0-1 range

✅ **Explainable Results:**
- [ ] Score breakdown available for all results
- [ ] Explanations are human-readable
- [ ] All score components identified
- [ ] Reasons make sense

### Technical Criteria

✅ **Performance:**
- [ ] Similarity computation <100ms per vehicle
- [ ] Explanation generation <50ms
- [ ] Handles 100 vehicles concurrently

✅ **Accuracy:**
- [ ] Conceptual scores correlate with human judgment (≥80%)
- [ ] Explainability useful for users (subjective evaluation)

---

## Testing Requirements

### Unit Tests

**Test Coverage:** ≥85%

**Test Cases:**
```csharp
[Theory]
[InlineData("reliable", 0.87)]
[InlineData("economical", 0.92)]
public async Task ComputeSimilarity_KnownConcept_ReturnsExpectedScore(string concept, double expected)

[Fact]
public async Task ComputeAttributeScore_LessThan_ReturnsCorrectScore()

[Fact]
public async Task ComputeDescriptionBoost_PositiveIndicators_AddsBoost()

[Fact]
public async Task ExplainRelevance_MultipleFactors_GeneratesExplanation()

[Fact]
public async Task MapConcept_AllConcepts_HaveWeightsSummingToOne()
```

### Integration Tests

- [ ] Test all 6 core concepts with real vehicles
- [ ] Verify scores align with manual evaluation
- [ ] Test explanation generation for 20 queries

---

## Definition of Done

- [ ] Conceptual mapper service implemented
- [ ] All 6 concepts mapped with weights
- [ ] Similarity scoring working
- [ ] Explainable scoring functional
- [ ] API endpoints functional
- [ ] All unit tests pass (≥85% coverage)
- [ ] Human evaluation of concepts (≥80% agreement)
- [ ] Documentation updated
- [ ] Code reviewed and approved

---

## Related Files

**Created/Modified:**
- `src/VehicleSearch.Infrastructure/AI/ConceptualMapperService.cs`
- `src/VehicleSearch.Infrastructure/AI/SimilarityScorer.cs`
- `src/VehicleSearch.Core/Interfaces/IConceptualMapperService.cs`
- `src/VehicleSearch.Core/Models/ConceptualMapping.cs`
- `src/VehicleSearch.Api/Controllers/SearchController.cs`
- `tests/VehicleSearch.Infrastructure.Tests/ConceptualMapperServiceTests.cs`

**References:**
- FRD-002: Semantic Search Engine (FR-3, FR-4, FR-5, FR-6)
- Task 010: Semantic matching
- Task 008: Qualitative defaults
