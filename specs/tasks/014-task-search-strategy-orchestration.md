# Task: Search Strategy Selection & Orchestration

**Task ID:** 014  
**Feature:** Hybrid Search Orchestration  
**Type:** Backend Implementation  
**Priority:** Critical  
**Estimated Complexity:** High  
**FRD Reference:** FRD-004 (FR-1, FR-2, FR-3)  
**GitHub Issue:** [#29](https://github.com/mollie-ward/complexSearch/issues/29)

---

## Description

Implement intelligent search strategy selection that determines the optimal combination of exact match, semantic search, and filtering based on query characteristics, and orchestrates multiple search approaches.

---

## Dependencies

**Depends on:**
- Task 009: Multi-Criteria Query Composition
- Task 010: Query Embedding & Semantic Matching
- Task 005: Azure AI Search Index Setup

**Blocks:**
- Task 015: Result Ranking & Re-ranking Logic
- Task 018: Search Interface Component

---

## Technical Requirements

### Search Orchestrator Service Interface

```csharp
public interface ISearchOrchestratorService
{
    Task<SearchStrategy> DetermineStrategyAsync(ComposedQuery query);
    Task<SearchResults> ExecuteSearchAsync(ComposedQuery query, SearchStrategy strategy);
    Task<SearchResults> ExecuteHybridSearchAsync(ComposedQuery query);
}

public class SearchStrategy
{
    public StrategyType Type { get; set; }
    public List<SearchApproach> Approaches { get; set; }
    public Dictionary<SearchApproach, double> Weights { get; set; }
    public bool ShouldRerank { get; set; }
}

public enum StrategyType
{
    ExactOnly,        // Pure filtering
    SemanticOnly,     // Pure vector search
    Hybrid,           // Combined approach
    MultiStage        // Sequential refinement
}

public enum SearchApproach
{
    ExactMatch,       // Structured filtering
    SemanticSearch,   // Vector/embedding search
    FullTextSearch,   // Keyword search
    Faceted           // Faceted navigation
}

public class SearchResults
{
    public List<VehicleResult> Results { get; set; }
    public int TotalCount { get; set; }
    public SearchStrategy Strategy { get; set; }
    public TimeSpan SearchDuration { get; set; }
    public Dictionary<string, object> Metadata { get; set; }
}

public class VehicleResult
{
    public VehicleDocument Vehicle { get; set; }
    public double RelevanceScore { get; set; }
    public SearchScoreBreakdown ScoreBreakdown { get; set; }
}

public class SearchScoreBreakdown
{
    public double ExactMatchScore { get; set; }
    public double SemanticScore { get; set; }
    public double KeywordScore { get; set; }
    public double FinalScore { get; set; }
}
```

### Strategy Determination Logic

**Analyze query to select optimal strategy:**

```csharp
public async Task<SearchStrategy> DetermineStrategyAsync(ComposedQuery query)
{
    var exactConstraints = query.ConstraintGroups
        .SelectMany(g => g.Constraints)
        .Where(c => c.Type == ConstraintType.Exact || c.Type == ConstraintType.Range)
        .ToList();
    
    var semanticConstraints = query.ConstraintGroups
        .SelectMany(g => g.Constraints)
        .Where(c => c.Type == ConstraintType.Semantic)
        .ToList();
    
    // Decision tree
    if (semanticConstraints.Count == 0 && exactConstraints.Count > 0)
    {
        // Pure filtering
        return new SearchStrategy
        {
            Type = StrategyType.ExactOnly,
            Approaches = new() { SearchApproach.ExactMatch },
            Weights = new() { [SearchApproach.ExactMatch] = 1.0 },
            ShouldRerank = false
        };
    }
    else if (exactConstraints.Count == 0 && semanticConstraints.Count > 0)
    {
        // Pure semantic search
        return new SearchStrategy
        {
            Type = StrategyType.SemanticOnly,
            Approaches = new() { SearchApproach.SemanticSearch },
            Weights = new() { [SearchApproach.SemanticSearch] = 1.0 },
            ShouldRerank = false
        };
    }
    else if (exactConstraints.Count > 0 && semanticConstraints.Count > 0)
    {
        // Hybrid approach
        var exactWeight = Math.Min(0.7, exactConstraints.Count * 0.15);
        var semanticWeight = 1.0 - exactWeight;
        
        return new SearchStrategy
        {
            Type = StrategyType.Hybrid,
            Approaches = new() { SearchApproach.ExactMatch, SearchApproach.SemanticSearch },
            Weights = new()
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
        return new SearchStrategy
        {
            Type = StrategyType.SemanticOnly,
            Approaches = new() { SearchApproach.SemanticSearch },
            Weights = new() { [SearchApproach.SemanticSearch] = 1.0 },
            ShouldRerank = false
        };
    }
}
```

**Strategy Examples:**

```
Query: "BMW under £20k"
Constraints: make=BMW (exact), price<=20000 (range)
Strategy: ExactOnly (pure filtering)

Query: "reliable economical car"
Constraints: reliable (semantic), economical (semantic)
Strategy: SemanticOnly (vector search)

Query: "reliable BMW under £20k"
Constraints: BMW (exact), price<=20000 (range), reliable (semantic)
Strategy: Hybrid (70% exact, 30% semantic)
```

### Exact Match Search

**Pure filtering with Azure Search:**

```csharp
private async Task<SearchResults> ExecuteExactSearchAsync(ComposedQuery query)
{
    var odataFilter = _queryComposer.ToODataFilter(query);
    
    var searchOptions = new SearchOptions
    {
        Filter = odataFilter,
        Size = 50,
        Select =
        {
            "id", "make", "model", "derivative", "price", "mileage",
            "registrationDate", "description", "features", "saleLocation"
        },
        OrderBy = { "price asc" }  // Default ordering
    };
    
    var response = await _searchClient.SearchAsync<VehicleDocument>(
        null,  // No text search
        searchOptions
    );
    
    var results = new List<VehicleResult>();
    await foreach (var result in response.Value.GetResultsAsync())
    {
        results.Add(new VehicleResult
        {
            Vehicle = result.Document,
            RelevanceScore = 1.0,  // All exact matches equally relevant
            ScoreBreakdown = new SearchScoreBreakdown
            {
                ExactMatchScore = 1.0,
                FinalScore = 1.0
            }
        });
    }
    
    return new SearchResults
    {
        Results = results,
        TotalCount = results.Count,
        Strategy = new SearchStrategy { Type = StrategyType.ExactOnly }
    };
}
```

### Semantic Search

**Pure vector search:**

```csharp
private async Task<SearchResults> ExecuteSemanticSearchAsync(ComposedQuery query, string semanticQuery)
{
    // Generate embedding
    var embedding = await _embeddingService.GenerateQueryEmbeddingAsync(semanticQuery);
    
    var searchOptions = new SearchOptions
    {
        VectorSearch = new VectorSearchOptions
        {
            Queries =
            {
                new VectorizedQuery(embedding)
                {
                    KNearestNeighborsCount = 50,
                    Fields = { "descriptionEmbedding" }
                }
            }
        },
        Size = 50,
        Select = { "id", "make", "model", "price", "description" }
    };
    
    // Add exact filters if any
    var exactConstraints = GetExactConstraints(query);
    if (exactConstraints.Any())
    {
        searchOptions.Filter = ConvertToODataFilter(exactConstraints);
    }
    
    var response = await _searchClient.SearchAsync<VehicleDocument>(
        null,
        searchOptions
    );
    
    var results = new List<VehicleResult>();
    await foreach (var result in response.Value.GetResultsAsync())
    {
        results.Add(new VehicleResult
        {
            Vehicle = result.Document,
            RelevanceScore = result.Score ?? 0.5,
            ScoreBreakdown = new SearchScoreBreakdown
            {
                SemanticScore = result.Score ?? 0.5,
                FinalScore = result.Score ?? 0.5
            }
        });
    }
    
    return new SearchResults
    {
        Results = results,
        TotalCount = results.Count,
        Strategy = new SearchStrategy { Type = StrategyType.SemanticOnly }
    };
}
```

### Hybrid Search (RRF - Reciprocal Rank Fusion)

**Combine exact and semantic search:**

```csharp
public async Task<SearchResults> ExecuteHybridSearchAsync(ComposedQuery query)
{
    var strategy = await DetermineStrategyAsync(query);
    
    if (strategy.Type != StrategyType.Hybrid)
    {
        return await ExecuteSearchAsync(query, strategy);
    }
    
    // Extract semantic query
    var semanticTerms = query.ConstraintGroups
        .SelectMany(g => g.Constraints)
        .Where(c => c.Type == ConstraintType.Semantic)
        .Select(c => c.Value.ToString())
        .ToList();
    var semanticQuery = string.Join(" ", semanticTerms);
    
    // Generate embedding
    var embedding = await _embeddingService.GenerateQueryEmbeddingAsync(semanticQuery);
    
    // Build OData filter for exact constraints
    var exactConstraints = query.ConstraintGroups
        .SelectMany(g => g.Constraints)
        .Where(c => c.Type == ConstraintType.Exact || c.Type == ConstraintType.Range)
        .ToList();
    var odataFilter = ConvertToODataFilter(exactConstraints);
    
    // Hybrid search with Azure AI Search
    var searchOptions = new SearchOptions
    {
        Filter = odataFilter,
        VectorSearch = new VectorSearchOptions
        {
            Queries =
            {
                new VectorizedQuery(embedding)
                {
                    KNearestNeighborsCount = 50,
                    Fields = { "descriptionEmbedding" }
                }
            }
        },
        Size = 50
    };
    
    var response = await _searchClient.SearchAsync<VehicleDocument>(
        semanticQuery,  // Also use text search for keywords
        searchOptions
    );
    
    // Azure Search handles RRF automatically
    var results = new List<VehicleResult>();
    await foreach (var result in response.Value.GetResultsAsync())
    {
        results.Add(new VehicleResult
        {
            Vehicle = result.Document,
            RelevanceScore = result.Score ?? 0.5,
            ScoreBreakdown = new SearchScoreBreakdown
            {
                ExactMatchScore = exactConstraints.Any() ? 0.7 : 0.0,
                SemanticScore = result.Score ?? 0.0,
                KeywordScore = result.Score ?? 0.0,
                FinalScore = result.Score ?? 0.5
            }
        });
    }
    
    return new SearchResults
    {
        Results = results,
        TotalCount = results.Count,
        Strategy = strategy,
        Metadata = new Dictionary<string, object>
        {
            ["exactConstraints"] = exactConstraints.Count,
            ["semanticTerms"] = semanticTerms.Count,
            ["hybridWeights"] = strategy.Weights
        }
    };
}
```

### API Endpoints

**POST /api/v1/search**

Request:
```json
{
  "query": "reliable BMW under £20k",
  "sessionId": "abc123",
  "maxResults": 10
}
```

Response:
```json
{
  "results": [
    {
      "vehicle": {
        "id": "V001",
        "make": "BMW",
        "model": "320d",
        "price": 18500,
        "mileage": 35000,
        "description": "Well-maintained BMW 320d with full service history"
      },
      "relevanceScore": 0.92,
      "scoreBreakdown": {
        "exactMatchScore": 0.7,
        "semanticScore": 0.89,
        "finalScore": 0.92
      }
    }
  ],
  "totalCount": 8,
  "strategy": {
    "type": "Hybrid",
    "approaches": ["ExactMatch", "SemanticSearch"],
    "weights": {
      "ExactMatch": 0.6,
      "SemanticSearch": 0.4
    }
  },
  "searchDuration": "00:00:00.487"
}
```

---

## Acceptance Criteria

### Functional Criteria

✅ **Strategy Selection:**
- [ ] Pure exact queries use ExactOnly
- [ ] Pure semantic queries use SemanticOnly
- [ ] Mixed queries use Hybrid
- [ ] Strategy weights calculated correctly

✅ **Exact Match:**
- [ ] All filters applied
- [ ] OData syntax valid
- [ ] Results match all constraints

✅ **Semantic Search:**
- [ ] Embeddings generated
- [ ] Vector search executed
- [ ] Results ranked by similarity

✅ **Hybrid Search:**
- [ ] Combines exact + semantic correctly
- [ ] RRF fusion works
- [ ] Weights respected

### Technical Criteria

✅ **Performance:**
- [ ] Exact search <500ms
- [ ] Semantic search <800ms
- [ ] Hybrid search <1 second
- [ ] Strategy determination <50ms

✅ **Accuracy:**
- [ ] 95%+ exact match precision
- [ ] 80%+ semantic relevance
- [ ] 85%+ hybrid relevance

---

## Testing Requirements

### Unit Tests

**Test Coverage:** ≥85%

**Test Cases:**
```csharp
[Fact]
public async Task DetermineStrategy_ExactOnly_ReturnsExactStrategy()

[Fact]
public async Task DetermineStrategy_SemanticOnly_ReturnsSemanticStrategy()

[Fact]
public async Task DetermineStrategy_Mixed_ReturnsHybridStrategy()

[Fact]
public async Task ExecuteExactSearch_ValidFilter_ReturnsMatchingResults()

[Fact]
public async Task ExecuteSemanticSearch_ValidQuery_ReturnsRankedResults()

[Fact]
public async Task ExecuteHybridSearch_MixedQuery_CombinesResults()
```

### Integration Tests

- [ ] Test 30 queries from FRD (10 each type)
- [ ] Verify all strategies execute correctly
- [ ] Test with real Azure AI Search index

---

## Definition of Done

- [ ] Search orchestrator service implemented
- [ ] Strategy selection working
- [ ] Exact search working
- [ ] Semantic search working
- [ ] Hybrid search working
- [ ] API endpoint functional
- [ ] All unit tests pass (≥85% coverage)
- [ ] Integration tests with Azure Search pass
- [ ] Documentation updated
- [ ] Code reviewed and approved

---

## Related Files

**Created/Modified:**
- `src/VehicleSearch.Infrastructure/Search/SearchOrchestratorService.cs`
- `src/VehicleSearch.Infrastructure/Search/ExactSearchService.cs`
- `src/VehicleSearch.Infrastructure/Search/SemanticSearchService.cs`
- `src/VehicleSearch.Core/Interfaces/ISearchOrchestratorService.cs`
- `src/VehicleSearch.Core/Models/SearchStrategy.cs`
- `src/VehicleSearch.Api/Controllers/SearchController.cs`
- `tests/VehicleSearch.Infrastructure.Tests/SearchOrchestratorServiceTests.cs`

**References:**
- FRD-004: Hybrid Search Orchestration (FR-1, FR-2, FR-3)
- Azure AI Search Hybrid Search documentation
