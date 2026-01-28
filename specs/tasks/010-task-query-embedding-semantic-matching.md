# Task: Query Embedding & Semantic Matching

**Task ID:** 010  
**Feature:** Semantic Search Engine  
**Type:** Backend Implementation  
**Priority:** High  
**Estimated Complexity:** Medium  
**FRD Reference:** FRD-002 (FR-1, FR-2)  
**GitHub Issue:** [#21](https://github.com/mollie-ward/complexSearch/issues/21)

---

## Description

Generate vector embeddings for user queries and implement semantic matching against indexed vehicle embeddings to find conceptually similar vehicles beyond keyword matches.

---

## Dependencies

**Depends on:**
- Task 006: Vehicle Embedding & Indexing (indexed vectors exist)
- Task 007: Query Intent Classification (to extract semantic terms)

**Blocks:**
- Task 011: Conceptual Understanding & Similarity Scoring
- Task 014: Search Strategy Selection & Orchestration

---

## Technical Requirements

### Embedding Service Interface

```csharp
public interface IEmbeddingService
{
    Task<float[]> GenerateQueryEmbeddingAsync(string query);
    Task<float[][]> GenerateBatchEmbeddingsAsync(string[] texts);
    Task<SemanticSearchResult> SearchByEmbeddingAsync(float[] embedding, int maxResults = 10);
}

public class SemanticSearchResult
{
    public List<VehicleMatch> Matches { get; set; }
    public double AverageScore { get; set; }
    public TimeSpan SearchDuration { get; set; }
}

public class VehicleMatch
{
    public string VehicleId { get; set; }
    public VehicleDocument Vehicle { get; set; }
    public double SimilarityScore { get; set; }  // 0-1 (cosine similarity)
    public double NormalizedScore { get; set; }  // 0-100
}
```

### Query Embedding Generation

**Use Azure OpenAI Embeddings API:**
- Model: `text-embedding-ada-002` or `text-embedding-3-small`
- Dimensions: 1536 (ada-002) or configurable (3-small)
- Input: Natural language query or semantic terms

**Query Preprocessing:**
```csharp
public string PrepareQueryForEmbedding(ParsedQuery parsedQuery)
{
    // Extract semantic components
    var components = new List<string>();
    
    // Add original query
    components.Add(parsedQuery.OriginalQuery);
    
    // Add explicit entities
    foreach (var entity in parsedQuery.Entities.Where(e => e.Type == EntityType.QualitativeTerm))
    {
        components.Add(entity.Value);
    }
    
    // Add contextual enrichment
    if (parsedQuery.Entities.Any(e => e.Type == EntityType.Make))
    {
        var make = parsedQuery.Entities.First(e => e.Type == EntityType.Make).Value;
        components.Add($"{make} vehicles");
    }
    
    return string.Join(" ", components);
}
```

**Example Transformations:**
```
Original: "reliable economical car"
→ Embedding Input: "reliable economical car reliable vehicles economical vehicles"

Original: "family car with space"
→ Embedding Input: "family car with space family vehicle spacious practical"

Original: "sporty BMW"
→ Embedding Input: "sporty BMW sporty vehicles BMW vehicles performance"
```

### Semantic Search with Azure AI Search

**Vector Search Configuration:**

```csharp
public async Task<SemanticSearchResult> SearchByEmbeddingAsync(
    float[] queryEmbedding,
    int maxResults = 10,
    SearchConstraint[] filters = null)
{
    var searchOptions = new SearchOptions
    {
        VectorSearch = new VectorSearchOptions
        {
            Queries =
            {
                new VectorizedQuery(queryEmbedding)
                {
                    KNearestNeighborsCount = maxResults * 3,  // Overquery for filtering
                    Fields = { "descriptionEmbedding" }
                }
            }
        },
        Size = maxResults,
        Select = { "id", "make", "model", "price", "mileage", "description" }
    };
    
    // Add filters if provided
    if (filters?.Any() == true)
    {
        searchOptions.Filter = ConvertToODataFilter(filters);
    }
    
    var response = await _searchClient.SearchAsync<VehicleDocument>(
        null,  // No text search, pure vector
        searchOptions
    );
    
    var matches = new List<VehicleMatch>();
    await foreach (var result in response.Value.GetResultsAsync())
    {
        matches.Add(new VehicleMatch
        {
            VehicleId = result.Document.Id,
            Vehicle = result.Document,
            SimilarityScore = result.Score ?? 0,
            NormalizedScore = (result.Score ?? 0) * 100
        });
    }
    
    return new SemanticSearchResult
    {
        Matches = matches,
        AverageScore = matches.Average(m => m.SimilarityScore)
    };
}
```

### Cosine Similarity Scoring

**Azure AI Search handles cosine similarity internally:**
- Returns scores 0-1 (1 = identical, 0 = orthogonal)
- Default algorithm: HNSW (Hierarchical Navigable Small World)
- Approximate nearest neighbors (fast, ~99% accuracy)

**Score Interpretation:**
- ≥ 0.85: Highly relevant
- 0.70-0.84: Relevant
- 0.50-0.69: Somewhat relevant
- < 0.50: Low relevance

### Embedding Caching

**Cache embeddings for identical queries:**

```csharp
public class CachedEmbeddingService : IEmbeddingService
{
    private readonly IMemoryCache _cache;
    private readonly IEmbeddingService _innerService;
    
    public async Task<float[]> GenerateQueryEmbeddingAsync(string query)
    {
        var cacheKey = $"embedding:{query.ToLowerInvariant()}";
        
        if (_cache.TryGetValue<float[]>(cacheKey, out var cached))
        {
            return cached;
        }
        
        var embedding = await _innerService.GenerateQueryEmbeddingAsync(query);
        
        _cache.Set(cacheKey, embedding, TimeSpan.FromHours(24));
        
        return embedding;
    }
}
```

**Cache Strategy:**
- Key: Lowercase normalized query
- Expiry: 24 hours
- Max size: 1000 entries (LRU eviction)

### Combining Semantic Search with Filters

**Hybrid approach: Vector search + constraints**

```
Query: "economical BMW under £20k"

1. Extract semantic: "economical"
2. Extract filters: make="BMW", price<=20000
3. Generate embedding for "economical BMW"
4. Search with filters:
   - Vector search with embedding
   - Filter: make='BMW' and price le 20000
```

**Pre-filtering vs Post-filtering:**
- Pre-filter: Apply constraints to search → faster, fewer results
- Post-filter: Search broadly, then filter → more results, slower

**Strategy:** Use pre-filtering for exact constraints, post-filtering for soft preferences

### API Endpoints

**POST /api/v1/search/semantic**

Request:
```json
{
  "query": "reliable economical car",
  "maxResults": 10,
  "filters": [
    { "fieldName": "price", "operator": "LessThanOrEqual", "value": 20000 }
  ]
}
```

Response:
```json
{
  "matches": [
    {
      "vehicleId": "V001",
      "vehicle": {
        "make": "Toyota",
        "model": "Prius",
        "price": 18500,
        "description": "Hybrid electric vehicle, excellent fuel economy"
      },
      "similarityScore": 0.89,
      "normalizedScore": 89
    },
    {
      "vehicleId": "V002",
      "vehicle": {
        "make": "Honda",
        "model": "Civic",
        "price": 16000,
        "description": "Fuel-efficient compact car"
      },
      "similarityScore": 0.82,
      "normalizedScore": 82
    }
  ],
  "averageScore": 0.855,
  "searchDuration": "00:00:00.243"
}
```

---

## Acceptance Criteria

### Functional Criteria

✅ **Embedding Generation:**
- [ ] Query embeddings generated successfully
- [ ] Dimension matches indexed vectors (1536)
- [ ] Preprocessing enhances semantic quality
- [ ] Embeddings cached for identical queries

✅ **Semantic Search:**
- [ ] Vector search returns relevant vehicles
- [ ] Cosine similarity scores accurate
- [ ] Results ranked by relevance
- [ ] Top 10 results returned

✅ **Conceptual Matching:**
- [ ] "economical" matches hybrid/electric vehicles
- [ ] "reliable" matches well-maintained vehicles
- [ ] "family car" matches SUVs/MPVs
- [ ] "sporty" matches performance cars

✅ **Filtered Semantic Search:**
- [ ] Combines vector search with exact constraints
- [ ] "economical BMW" returns BMW hybrids
- [ ] Price filters apply correctly
- [ ] All filters respected

### Technical Criteria

✅ **Performance:**
- [ ] Embedding generation <300ms
- [ ] Vector search <500ms
- [ ] Total semantic search <1 second
- [ ] Cache hit rate >50% in production

✅ **Accuracy:**
- [ ] Semantic relevance ≥70% (user eval)
- [ ] Top 5 results always relevant
- [ ] Handles typos/variations

---

## Testing Requirements

### Unit Tests

**Test Coverage:** ≥85%

**Test Cases:**
```csharp
[Fact]
public async Task GenerateQueryEmbedding_ValidQuery_Returns1536Dimensions()

[Fact]
public async Task PrepareQuery_QualitativeTerms_AddsEnrichment()

[Fact]
public async Task SearchByEmbedding_NoFilters_ReturnsTop10()

[Fact]
public async Task SearchByEmbedding_WithFilters_RespectsConstraints()

[Fact]
public async Task CachedEmbedding_IdenticalQuery_ReturnsCachedValue()

[Theory]
[InlineData("economical", "Toyota Prius", 0.85)]
[InlineData("reliable", "Honda Civic", 0.80)]
public async Task SemanticMatch_QualitativeTerm_MatchesExpectedVehicle(
    string query, string expectedMake, double minScore)
```

### Integration Tests

**Test Cases:**
- [ ] Generate embeddings via Azure OpenAI
- [ ] Search against real indexed vehicles
- [ ] Test 20 semantic queries from FRD
- [ ] Verify all scores > 0.50
- [ ] Test caching behavior

---

## Implementation Notes

### DO:
- ✅ Use Azure OpenAI embedding API
- ✅ Cache embeddings for performance
- ✅ Preprocess queries for better semantic quality
- ✅ Combine with exact filters when available
- ✅ Log embedding API calls (cost tracking)
- ✅ Handle rate limiting gracefully

### DON'T:
- ❌ Skip caching (embeddings are expensive)
- ❌ Generate embeddings for every request
- ❌ Ignore filter constraints in vector search
- ❌ Return low-relevance results (<0.50)

### Azure OpenAI Configuration:
```json
{
  "AzureOpenAI": {
    "Endpoint": "https://YOUR_RESOURCE.openai.azure.com/",
    "EmbeddingDeploymentName": "text-embedding-ada-002",
    "ApiKey": "***",
    "MaxTokens": 8191,
    "RateLimitPerMinute": 60
  }
}
```

---

## Definition of Done

- [ ] Embedding service implemented
- [ ] Query preprocessing working
- [ ] Semantic search with Azure AI Search functional
- [ ] Caching implemented
- [ ] Filtered semantic search working
- [ ] API endpoint functional
- [ ] All unit tests pass (≥85% coverage)
- [ ] Integration tests with Azure OpenAI pass
- [ ] Documentation updated
- [ ] Code reviewed and approved

---

## Related Files

**Created/Modified:**
- `src/VehicleSearch.Infrastructure/AI/EmbeddingService.cs`
- `src/VehicleSearch.Infrastructure/AI/CachedEmbeddingService.cs`
- `src/VehicleSearch.Infrastructure/Search/SemanticSearchService.cs`
- `src/VehicleSearch.Core/Interfaces/IEmbeddingService.cs`
- `src/VehicleSearch.Core/Models/SemanticSearchResult.cs`
- `src/VehicleSearch.Api/Controllers/SearchController.cs`
- `tests/VehicleSearch.Infrastructure.Tests/EmbeddingServiceTests.cs`

**References:**
- FRD-002: Semantic Search Engine (FR-1, FR-2)
- Task 006: Indexed vehicle embeddings
- Azure OpenAI Embeddings API documentation
