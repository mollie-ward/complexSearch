# Task: Azure AI Search Index Setup

**Task ID:** 005  
**GitHub Issue:** [#11](https://github.com/mollie-ward/complexSearch/issues/11)  
**Status:** Assigned to Copilot (PR pending)  
**Feature:** Knowledge Base Integration  
**Type:** Backend Implementation  
**Priority:** High  
**Estimated Complexity:** Medium  
**FRD Reference:** FRD-005 (FR-3)

---

## Description

Configure Azure AI Search index with schema optimized for hybrid search (vector + keyword + filters), including vector fields for semantic search, structured fields for exact matching, and filterable fields for multi-criteria queries.

---

## Dependencies

**Depends on:**
- Task 004: Data Ingestion & CSV Processing

**Blocks:**
- Task 006: Vehicle Embedding & Indexing
- Task 010: Query Embedding & Semantic Matching

---

## Technical Requirements

### Azure AI Search Index Schema

**Index Name:** `vehicles-index`

**Field Definitions:**

**Identity & Text Fields:**
```json
{
  "name": "id",
  "type": "Edm.String",
  "key": true,
  "searchable": false
},
{
  "name": "make",
  "type": "Edm.String",
  "searchable": true,
  "filterable": true,
  "facetable": true
},
{
  "name": "model",
  "type": "Edm.String",
  "searchable": true,
  "filterable": true
},
{
  "name": "derivative",
  "type": "Edm.String",
  "searchable": true,
  "filterable": false
},
{
  "name": "bodyType",
  "type": "Edm.String",
  "filterable": true,
  "facetable": true
},
{
  "name": "colour",
  "type": "Edm.String",
  "filterable": true,
  "facetable": true
}
```

**Numeric Fields:**
```json
{
  "name": "price",
  "type": "Edm.Double",
  "filterable": true,
  "sortable": true,
  "facetable": true
},
{
  "name": "mileage",
  "type": "Edm.Int32",
  "filterable": true,
  "sortable": true
},
{
  "name": "engineSize",
  "type": "Edm.Double",
  "filterable": true
},
{
  "name": "numberOfDoors",
  "type": "Edm.Int32",
  "filterable": true,
  "facetable": true
}
```

**Date Fields:**
```json
{
  "name": "registrationDate",
  "type": "Edm.DateTimeOffset",
  "filterable": true,
  "sortable": true
},
{
  "name": "motExpiryDate",
  "type": "Edm.DateTimeOffset",
  "filterable": true
}
```

**Categorical Fields:**
```json
{
  "name": "fuelType",
  "type": "Edm.String",
  "filterable": true,
  "facetable": true
},
{
  "name": "transmissionType",
  "type": "Edm.String",
  "filterable": true,
  "facetable": true
},
{
  "name": "saleLocation",
  "type": "Edm.String",
  "filterable": true,
  "facetable": true
}
```

**Array Fields:**
```json
{
  "name": "features",
  "type": "Collection(Edm.String)",
  "searchable": true,
  "filterable": true
}
```

**Full-Text Search:**
```json
{
  "name": "description",
  "type": "Edm.String",
  "searchable": true,
  "analyzer": "en.microsoft"
}
```

**Vector Field (for semantic search):**
```json
{
  "name": "descriptionVector",
  "type": "Collection(Edm.Single)",
  "searchable": true,
  "dimensions": 1536,
  "vectorSearchProfile": "vector-profile"
}
```

### Vector Search Configuration

**Vector Search Algorithm:**
```json
{
  "algorithmConfigurations": [
    {
      "name": "vector-config",
      "kind": "hnsw",
      "hnswParameters": {
        "metric": "cosine",
        "m": 4,
        "efConstruction": 400,
        "efSearch": 500
      }
    }
  ]
}
```

**Vector Profile:**
```json
{
  "profiles": [
    {
      "name": "vector-profile",
      "algorithm": "vector-config"
    }
  ]
}
```

### Semantic Configuration

**Semantic Ranking:**
```json
{
  "semantic": {
    "configurations": [
      {
        "name": "semantic-config",
        "prioritizedFields": {
          "titleField": {
            "fieldName": "make"
          },
          "contentFields": [
            { "fieldName": "description" },
            { "fieldName": "model" },
            { "fieldName": "features" }
          ],
          "keywordFields": [
            { "fieldName": "make" },
            { "fieldName": "fuelType" },
            { "fieldName": "bodyType" }
          ]
        }
      }
    ]
  }
}
```

### Analyzers & Tokenizers

**Custom Analyzer (optional for UK-specific):**
```json
{
  "analyzers": [
    {
      "name": "vehicle-analyzer",
      "@odata.type": "#Microsoft.Azure.Search.CustomAnalyzer",
      "tokenizer": "standard_v2",
      "tokenFilters": ["lowercase", "asciifolding"],
      "charFilters": []
    }
  ]
}
```

### Index Service Implementation

**Service Interface:**
```csharp
public interface ISearchIndexService
{
    Task CreateIndexAsync(CancellationToken cancellationToken = default);
    Task DeleteIndexAsync(CancellationToken cancellationToken = default);
    Task<bool> IndexExistsAsync(CancellationToken cancellationToken = default);
    Task UpdateIndexSchemaAsync(CancellationToken cancellationToken = default);
}
```

**Configuration:**
```csharp
public class AzureSearchConfig
{
    public string Endpoint { get; set; }
    public string ApiKey { get; set; }
    public string IndexName { get; set; }
    public int VectorDimensions { get; set; } = 1536;
}
```

### Index Management API

**POST /api/v1/knowledge-base/index/create**

Creates the search index with full schema.

Response:
```json
{
  "indexName": "vehicles-index",
  "fieldsCount": 25,
  "vectorFieldsCount": 1,
  "created": true,
  "timestamp": "2026-01-27T10:00:00Z"
}
```

**DELETE /api/v1/knowledge-base/index**

Deletes the search index.

**GET /api/v1/knowledge-base/index/status**

Response:
```json
{
  "exists": true,
  "indexName": "vehicles-index",
  "documentCount": 58,
  "storageSize": "2.5 MB"
}
```

---

## Acceptance Criteria

### Functional Criteria

✅ **Index Creation:**
- [ ] Index created successfully in Azure AI Search
- [ ] All 25+ fields defined correctly
- [ ] Vector field configured with 1536 dimensions
- [ ] Index accessible via Azure portal

✅ **Field Configuration:**
- [ ] Text fields are searchable
- [ ] Numeric fields are filterable
- [ ] Date fields are sortable
- [ ] Array fields support collections
- [ ] Vector field properly configured

✅ **Search Capabilities:**
- [ ] Full-text search works on description
- [ ] Filters work on make, price, mileage, fuel type
- [ ] Facets return for make, body type, fuel type
- [ ] Sorting works on price, mileage, date
- [ ] Vector search field ready (tested with sample vector)

✅ **Semantic Configuration:**
- [ ] Semantic ranking configured
- [ ] Title, content, and keyword fields set
- [ ] Semantic search enabled

### Technical Criteria

✅ **Performance:**
- [ ] Index creation completes in <30 seconds
- [ ] Empty index queryable immediately
- [ ] No errors during schema creation

✅ **Configuration:**
- [ ] Endpoint and API key from configuration
- [ ] Index name configurable
- [ ] Vector dimensions configurable

✅ **Error Handling:**
- [ ] Index already exists handled gracefully
- [ ] Invalid credentials return clear error
- [ ] Network errors handled with retry logic

---

## Testing Requirements

### Unit Tests

**Test Coverage:** ≥85%

**Test Cases:**
```csharp
[Fact]
public async Task CreateIndex_ValidConfig_CreatesSuccessfully()
[Fact]
public async Task CreateIndex_AlreadyExists_ReturnsError()
[Fact]
public async Task IndexExists_IndexPresent_ReturnsTrue()
[Fact]
public async Task DeleteIndex_ExistingIndex_DeletesSuccessfully()
[Fact]
public async Task GetIndexSchema_ReturnsAllFields()
```

### Integration Tests

**Test Cases:**
- [ ] Create index in test Azure Search instance
- [ ] Verify all fields present via Azure SDK
- [ ] Test basic text search on empty index
- [ ] Test filter queries
- [ ] Delete index successfully

**Test Environment:**
- Use separate test Azure Search instance
- Or use Azure Search emulator if available

---

## Implementation Notes

### DO:
- ✅ Use Azure.Search.Documents SDK (latest)
- ✅ Configure index for hybrid search from start
- ✅ Enable semantic search configuration
- ✅ Make schema extensible (easy to add fields)
- ✅ Use HNSW algorithm for vector search
- ✅ Set appropriate vector dimensions (1536 for ada-002)

### DON'T:
- ❌ Hardcode index schema (use configuration)
- ❌ Skip vector field configuration
- ❌ Ignore error handling for Azure calls
- ❌ Create production index in dev/test
- ❌ Expose API keys in logs

### Best Practices:
- Use managed identity for Azure auth (v2)
- Configure index analyzers for better search
- Enable CORS if frontend calls directly
- Set appropriate tier (Basic for dev, Standard for prod)
- Monitor index storage and query costs

### Vector Search Notes:
- 1536 dimensions for OpenAI ada-002 embeddings
- Cosine similarity for distance metric
- HNSW for fast approximate nearest neighbor
- Adjust efSearch for recall vs. latency trade-off

---

## Definition of Done

- [ ] Search index service implemented
- [ ] Index schema defined with all fields
- [ ] Vector field configured correctly
- [ ] Semantic configuration added
- [ ] API endpoints functional
- [ ] Index creates successfully
- [ ] All unit tests pass (≥85% coverage)
- [ ] Integration test with Azure succeeds
- [ ] Configuration externalized
- [ ] Error handling comprehensive
- [ ] Code reviewed and approved
- [ ] Documentation updated

---

## Related Files

**Created/Modified:**
- `src/VehicleSearch.Infrastructure/Search/AzureSearchClient.cs`
- `src/VehicleSearch.Infrastructure/Search/SearchIndexService.cs`
- `src/VehicleSearch.Core/Interfaces/ISearchIndexService.cs`
- `src/VehicleSearch.Api/Controllers/KnowledgeBaseController.cs`
- `src/VehicleSearch.Api/appsettings.json` (Azure Search config)
- `tests/VehicleSearch.Infrastructure.Tests/SearchIndexServiceTests.cs`

**References:**
- FRD-005: Knowledge Base Integration (FR-3)
- AGENTS.md (Azure AI Search configuration)
- Azure AI Search documentation
- Task 004 (Vehicle entity schema)
