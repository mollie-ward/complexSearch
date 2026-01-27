# Task: Vehicle Embedding & Indexing

**Task ID:** 006  
**Feature:** Knowledge Base Integration  
**Type:** Backend Implementation  
**Priority:** High  
**Estimated Complexity:** Medium  
**FRD Reference:** FRD-005 (FR-3, FR-4)

---

## Description

Generate semantic vector embeddings for all vehicle records using Azure OpenAI embedding model, index vehicles in Azure AI Search with both structured data and vectors, and implement efficient retrieval service.

---

## Dependencies

**Depends on:**
- Task 004: Data Ingestion & CSV Processing
- Task 005: Azure AI Search Index Setup

**Blocks:**
- Task 010: Query Embedding & Semantic Matching
- Task 014: Search Strategy Selection & Orchestration

---

## Technical Requirements

### Embedding Generation Service

**Service Interface:**
```csharp
public interface IEmbeddingService
{
    Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default);
    Task<IEnumerable<VehicleEmbedding>> GenerateBatchEmbeddingsAsync(
        IEnumerable<Vehicle> vehicles, 
        int batchSize = 100,
        CancellationToken cancellationToken = default);
}

public class VehicleEmbedding
{
    public string VehicleId { get; set; }
    public float[] Vector { get; set; }
    public int Dimensions { get; set; }
}
```

**Embedding Configuration:**
- Model: `text-embedding-ada-002` or `text-embedding-3-small`
- Dimensions: 1536 (ada-002) or configurable (3-small)
- Max tokens per request: 8191
- Batch processing: 100 vehicles per batch

**Text Preparation for Embedding:**
Use vehicle description from Task 004:
```
"{Make} {Model} {Derivative}, {EngineSize}L {FuelType} {TransmissionType}, 
{BodyType}, {Colour}, {Mileage} miles, £{Price}, 
registered {RegistrationDate:MMM yyyy}, {SaleLocation}.
Features: {string.Join(", ", Features)}"
```

### Indexing Service

**Service Interface:**
```csharp
public interface IVehicleIndexingService
{
    Task<IndexingResult> IndexVehiclesAsync(
        IEnumerable<Vehicle> vehicles,
        CancellationToken cancellationToken = default);
    Task<IndexingResult> IndexVehicleAsync(
        Vehicle vehicle,
        CancellationToken cancellationToken = default);
    Task<bool> DeleteVehicleAsync(
        string vehicleId,
        CancellationToken cancellationToken = default);
    Task<IndexStats> GetIndexStatsAsync();
}

public class IndexingResult
{
    public int TotalVehicles { get; set; }
    public int Succeeded { get; set; }
    public int Failed { get; set; }
    public List<IndexingError> Errors { get; set; }
    public TimeSpan Duration { get; set; }
}
```

**Indexing Process:**
1. Load vehicles from data ingestion
2. Generate embeddings for each vehicle
3. Map Vehicle entity to search document
4. Upload documents to Azure AI Search
5. Return indexing results

**Search Document Mapping:**
```csharp
public class VehicleSearchDocument
{
    public string Id { get; set; }
    public string Make { get; set; }
    public string Model { get; set; }
    public string Derivative { get; set; }
    public double Price { get; set; }
    public int Mileage { get; set; }
    public string BodyType { get; set; }
    public double? EngineSize { get; set; }
    public string FuelType { get; set; }
    public string TransmissionType { get; set; }
    public string Colour { get; set; }
    public int? NumberOfDoors { get; set; }
    public DateTimeOffset RegistrationDate { get; set; }
    public string SaleLocation { get; set; }
    public string Channel { get; set; }
    public string[] Features { get; set; }
    public string Description { get; set; }
    public float[] DescriptionVector { get; set; } // Embedding
    public DateTimeOffset ProcessedDate { get; set; }
}
```

### Batch Processing

**For Large Datasets:**
- Process in batches of 100 vehicles
- Generate embeddings in parallel (with rate limiting)
- Upload to Azure Search in batches of 1000
- Track progress and resume on failure

**Rate Limiting:**
- Azure OpenAI: 3,000 requests/minute (TPM: 120k)
- Implement exponential backoff on 429 errors
- Use semaphore to limit concurrent requests

### Retrieval Service

**Service Interface:**
```csharp
public interface IVehicleRetrievalService
{
    Task<IEnumerable<Vehicle>> GetVehiclesByIdsAsync(
        IEnumerable<string> ids,
        CancellationToken cancellationToken = default);
    Task<Vehicle> GetVehicleByIdAsync(
        string id,
        CancellationToken cancellationToken = default);
    Task<int> GetTotalCountAsync();
}
```

### API Endpoints

**POST /api/v1/knowledge-base/index-vehicles**

Request:
```json
{
  "source": "csv",
  "generateEmbeddings": true,
  "batchSize": 100
}
```

Response:
```json
{
  "totalVehicles": 58,
  "succeeded": 58,
  "failed": 0,
  "embeddingsGenerated": 58,
  "indexingTime": 45000,
  "errors": []
}
```

**GET /api/v1/vehicles/{id}**

Response:
```json
{
  "id": "SY22GAU",
  "make": "ALFA ROMEO",
  "model": "STELVIO",
  "price": 19350,
  "mileage": 51120,
  "features": ["Navigation", "Parking Sensors", "Leather Trim"]
}
```

---

## Acceptance Criteria

### Functional Criteria

✅ **Embedding Generation:**
- [ ] All 58 vehicles have embeddings generated
- [ ] Embeddings are 1536-dimensional vectors
- [ ] Embedding generation uses Azure OpenAI
- [ ] Batch processing works for 100+ vehicles
- [ ] Rate limiting prevents API throttling

✅ **Indexing:**
- [ ] All vehicles indexed in Azure AI Search
- [ ] Both structured data and vectors indexed
- [ ] Vector field populated correctly
- [ ] Search documents retrievable by ID

✅ **Data Completeness:**
- [ ] No data loss during indexing
- [ ] All fields from Vehicle entity preserved
- [ ] Features array indexed correctly
- [ ] Dates converted to DateTimeOffset

✅ **Performance:**
- [ ] 58 vehicles indexed in <2 minutes
- [ ] 1000 vehicles indexed in <10 minutes
- [ ] Parallel embedding generation
- [ ] Batch uploads to search

### Technical Criteria

✅ **Error Handling:**
- [ ] Failed embeddings don't stop batch
- [ ] Retry logic for transient errors
- [ ] Clear error messages for each failure
- [ ] Resume capability for large batches

✅ **Configuration:**
- [ ] Embedding model configurable
- [ ] Batch size configurable
- [ ] Rate limits configurable
- [ ] API keys from secure config

---

## Testing Requirements

### Unit Tests

**Test Coverage:** ≥85%

**Test Cases:**
```csharp
[Fact]
public async Task GenerateEmbedding_ValidText_Returns1536Dimensions()
[Fact]
public async Task GenerateBatchEmbeddings_100Vehicles_ProcessesAll()
[Fact]
public async Task IndexVehicles_ValidData_IndexesSuccessfully()
[Fact]
public async Task IndexVehicles_WithRetry_RecoversFromTransientError()
[Fact]
public async Task MapToSearchDocument_AllFields_MapsCorrectly()
[Fact]
public async Task GetVehicleById_ExistingId_ReturnsVehicle()
```

**Mocking:**
- Mock Azure OpenAI client for embeddings
- Mock Azure Search client for indexing
- Use test vectors for assertions

### Integration Tests

**Test Cases:**
- [ ] Generate real embeddings for 5 test vehicles
- [ ] Index test vehicles in Azure Search
- [ ] Retrieve indexed vehicles by ID
- [ ] Verify vector field populated
- [ ] Test batch processing with 50 vehicles

**Test Data:**
- Use subset of sampleData.csv
- Verify against actual Azure services

---

## Implementation Notes

### DO:
- ✅ Use Azure OpenAI SDK for embeddings
- ✅ Implement retry logic with exponential backoff
- ✅ Process embeddings in parallel (with limits)
- ✅ Cache embeddings if re-indexing same data
- ✅ Log progress for long-running operations
- ✅ Validate vector dimensions before indexing

### DON'T:
- ❌ Generate embeddings synchronously one-by-one
- ❌ Index without embeddings (vector field required)
- ❌ Ignore rate limiting (will cause failures)
- ❌ Store API keys in code
- ❌ Skip error handling for batch operations

### Performance Optimization:
- Use SemaphoreSlim for concurrency control
- Batch API calls (100 per batch)
- Use async/await throughout
- Monitor OpenAI token usage
- Consider caching embeddings

### Cost Considerations:
- ada-002: ~$0.0001 per 1K tokens
- 58 vehicles × ~100 tokens = ~5.8K tokens ≈ $0.0006
- Track and log token usage

---

## Definition of Done

- [ ] Embedding service implemented
- [ ] Batch embedding generation working
- [ ] Indexing service implemented
- [ ] All 58 vehicles indexed successfully
- [ ] Retrieval service working
- [ ] API endpoints functional
- [ ] Rate limiting implemented
- [ ] Error handling comprehensive
- [ ] All unit tests pass (≥85% coverage)
- [ ] Integration tests with Azure pass
- [ ] Performance benchmarks met
- [ ] Code reviewed and approved
- [ ] Documentation updated

---

## Related Files

**Created/Modified:**
- `src/VehicleSearch.Infrastructure/AI/EmbeddingService.cs`
- `src/VehicleSearch.Infrastructure/Search/VehicleIndexingService.cs`
- `src/VehicleSearch.Infrastructure/Search/VehicleRetrievalService.cs`
- `src/VehicleSearch.Core/Interfaces/IEmbeddingService.cs`
- `src/VehicleSearch.Core/Interfaces/IVehicleIndexingService.cs`
- `src/VehicleSearch.Api/Controllers/KnowledgeBaseController.cs`
- `src/VehicleSearch.Api/Controllers/VehiclesController.cs`
- `tests/VehicleSearch.Infrastructure.Tests/EmbeddingServiceTests.cs`

**References:**
- FRD-005: Knowledge Base Integration (FR-3, FR-4)
- Task 004: Vehicle entity and description
- Task 005: Search index schema
- Azure OpenAI Embeddings documentation
