# Task: Data Ingestion & CSV Processing

**Task ID:** 004  
**GitHub Issue:** [#9](https://github.com/mollie-ward/complexSearch/issues/9)  
**Status:** Assigned to Copilot (PR pending)  
**Feature:** Knowledge Base Integration  
**Type:** Backend Implementation  
**Priority:** High  
**Estimated Complexity:** Medium  
**FRD Reference:** FRD-005 (FR-1, FR-2)

---

## Description

Implement CSV data ingestion pipeline to parse, validate, normalize, and transform vehicle inventory data from sampleData.csv into structured entities ready for indexing in Azure AI Search.

---

## Dependencies

**Depends on:**
- Task 001: Backend API Scaffolding

**Blocks:**
- Task 005: Azure AI Search Index Setup
- Task 006: Vehicle Embedding & Indexing

---

## Technical Requirements

### CSV Parser Implementation

**Service Interface:**
```csharp
public interface IDataIngestionService
{
    Task<IngestionResult> IngestFromCsvAsync(string filePath, CancellationToken cancellationToken = default);
    Task<IEnumerable<Vehicle>> ParseCsvAsync(Stream csvStream, CancellationToken cancellationToken = default);
    Task<ValidationResult> ValidateDataAsync(IEnumerable<Vehicle> vehicles);
}
```

**CSV Parsing Requirements:**
- Read CSV files with 60+ columns
- Handle quoted fields with commas
- Parse header row to map columns
- Support streaming for large files (10,000+ records)
- Handle UTF-8 encoding
- Detect and report malformed rows

**Library:** Use CsvHelper NuGet package

### Data Schema Mapping

**Source CSV Columns → Vehicle Entity:**

**Identity Fields:**
- `Registration Number` → `Id` (use as unique identifier)
- `Make` → `Make`
- `Model` → `Model`
- `Derivative` → `Derivative`
- `Body` → `BodyType`

**Specifications:**
- `Engine Size` → `EngineSize` (decimal, parse "2.0" → 2.0)
- `Fuel` → `FuelType`
- `Transmission` → `TransmissionType`
- `Colour` → `Colour`
- `Number Of Doors` → `NumberOfDoors` (int)

**Pricing:**
- `Buy Now Price` → `Price` (parse "£ 20,700" → 20700.0)
- `Cap Retail Price` → `CapRetailPrice`
- `Cap Clean Price` → `CapCleanPrice`
- `VAT Type` → `VatType`

**Condition & History:**
- `Mileage` → `Mileage` (int, parse "51,120" → 51120)
- `Registration Date` → `RegistrationDate` (DateTime, parse "05/03/2022")
- `Service History Present` → `ServiceHistoryPresent` (bool)
- `Number of Services` → `NumberOfServices` (int)
- `Last Service Date` → `LastServiceDate` (nullable DateTime)
- `MOT Expiry` → `MotExpiryDate` (nullable DateTime)
- `Grade` → `Grade` (int)

**Location:**
- `Sale Location` → `SaleLocation`
- `Channel` → `Channel`
- `Sale Type` → `SaleType`

**Features:**
- `Equipment` → `Features` (string[], split by comma)
- `Additional Information` → `AdditionalInfo`
- `Declarations` → `Declarations` (string[])

### Data Normalization

**Text Normalization:**
- Trim whitespace from all string fields
- Convert Make/Model/BodyType to Title Case
- Normalize colors: "GREY" → "Grey", "BLACK" → "Black"
- Remove multiple spaces

**Numeric Parsing:**
- Remove currency symbols: "£ 20,700" → "20700"
- Remove thousand separators: "51,120" → "51120"
- Handle empty/null as 0 or null appropriately

**Date Parsing:**
- Parse UK format: "05/03/2022" → DateTime(2022, 3, 5)
- Handle "01/01/0001" as null (invalid date marker)
- Support format: "dd/MM/yyyy"

**Boolean Parsing:**
- "No" → false, "Yes" → true
- Empty/null → false

**Array Fields:**
- Split Equipment by comma + space: ", "
- Trim each item
- Remove empty items
- Example: "Nav, Leather, Sensors" → ["Nav", "Leather", "Sensors"]

### Data Validation

**Required Fields:**
- Registration Number (must be unique)
- Make
- Model
- Price (must be > 0)

**Optional Fields:**
- All other fields can be null/empty

**Validation Rules:**
- Price: 0 < price < 200,000
- Mileage: 0 <= mileage < 500,000
- Engine Size: 0 < size < 10.0
- Registration Date: 1990 <= year <= 2026
- Doors: 2, 3, 4, 5, or 7

**Error Handling:**
- Log validation errors with row number
- Skip invalid rows (don't fail entire batch)
- Report total processed vs. total valid
- Return validation summary

### Vehicle Entity

**Core Entity Definition:**
```csharp
public class Vehicle
{
    public string Id { get; set; } // Registration Number
    public string Make { get; set; }
    public string Model { get; set; }
    public string? Derivative { get; set; }
    public decimal Price { get; set; }
    public int Mileage { get; set; }
    public string BodyType { get; set; }
    public decimal? EngineSize { get; set; }
    public string FuelType { get; set; }
    public string TransmissionType { get; set; }
    public string? Colour { get; set; }
    public int? NumberOfDoors { get; set; }
    public DateTime RegistrationDate { get; set; }
    public string SaleLocation { get; set; }
    public string Channel { get; set; }
    public List<string> Features { get; set; } = new();
    public int? Grade { get; set; }
    public bool ServiceHistoryPresent { get; set; }
    public int? NumberOfServices { get; set; }
    public DateTime? LastServiceDate { get; set; }
    public DateTime? MotExpiryDate { get; set; }
    public string? VatType { get; set; }
    public string? AdditionalInfo { get; set; }
    public List<string> Declarations { get; set; } = new();
    
    // Computed/derived fields
    public string Description { get; set; } // For embedding generation
    public DateTime ProcessedDate { get; set; }
}
```

### Description Generation

**For Semantic Search:**
Combine fields into searchable text description:

```
"{Make} {Model} {Derivative}, {EngineSize}L {FuelType} {TransmissionType}, 
{BodyType}, {Colour}, {Mileage} miles, £{Price}, 
registered {RegistrationDate:MMM yyyy}, {SaleLocation}.
Features: {string.Join(", ", Features)}"
```

Example:
```
"BMW 3 Series 320d M Sport, 2.0L Diesel Automatic, Saloon, Blue, 
42,135 miles, £23,250, registered Sep 2021, Manchester.
Features: Navigation HDD, Parking Sensors, Climate Control, Leather Trim"
```

### API Endpoints

**POST /api/v1/knowledge-base/ingest**

Request:
```json
{
  "source": "csv",
  "filePath": "/path/to/sampleData.csv"
}
```

Response:
```json
{
  "totalRows": 60,
  "validRows": 58,
  "invalidRows": 2,
  "processingTime": 1234,
  "errors": [
    {
      "row": 15,
      "field": "Price",
      "error": "Invalid price value: 'N/A'"
    }
  ]
}
```

**GET /api/v1/knowledge-base/status**

Response:
```json
{
  "totalVehicles": 58,
  "lastIngestionDate": "2026-01-27T10:00:00Z",
  "dataSource": "sampleData.csv"
}
```

---

## Acceptance Criteria

### Functional Criteria

✅ **CSV Parsing:**
- [ ] All 60 rows from sampleData.csv parsed successfully
- [ ] All 60 columns mapped to Vehicle entity
- [ ] No data loss during parsing
- [ ] Header row correctly identified and skipped

✅ **Data Normalization:**
- [ ] Price values parsed correctly (£ symbols removed)
- [ ] Mileage values parsed correctly (commas removed)
- [ ] Dates parsed correctly (UK format → DateTime)
- [ ] Features split into string arrays
- [ ] Text fields trimmed and normalized

✅ **Data Validation:**
- [ ] Invalid prices rejected (negative, zero, > 200k)
- [ ] Invalid dates rejected (before 1990, after 2026)
- [ ] Missing required fields flagged
- [ ] Validation errors logged with row numbers
- [ ] Valid records processed despite invalid rows

✅ **Description Generation:**
- [ ] Every vehicle has generated description
- [ ] Description includes make, model, price, mileage, features
- [ ] Description is human-readable
- [ ] Description suitable for embedding

### Technical Criteria

✅ **Performance:**
- [ ] Process 60 records in <5 seconds
- [ ] Process 10,000 records in <60 seconds
- [ ] Memory efficient (streaming for large files)
- [ ] No memory leaks

✅ **Error Handling:**
- [ ] Malformed CSV rows handled gracefully
- [ ] File not found returns proper error
- [ ] Partial success reported (X of Y records)
- [ ] All errors logged with context

✅ **Code Quality:**
- [ ] Follows naming conventions
- [ ] XML documentation on public methods
- [ ] No hardcoded values
- [ ] Async/await used correctly

---

## Testing Requirements

### Unit Tests

**Test Coverage:** ≥85%

**Test Cases:**
```csharp
[Fact]
public async Task ParseCsv_ValidFile_ReturnsAllVehicles()
[Fact]
public async Task ParseCsv_InvalidPrice_SkipsRow()
[Fact]
public async Task NormalizePrice_WithCurrencySymbol_RemovesSymbol()
[Fact]
public async Task ParseDate_UKFormat_ConvertsCorrectly()
[Fact]
public async Task SplitFeatures_CommaDelimited_CreatesArray()
[Fact]
public async Task ValidateVehicle_MissingMake_ReturnsError()
[Fact]
public async Task GenerateDescription_AllFields_CreatesReadableText()
```

**Test Data:**
- Use subset of sampleData.csv (5-10 rows)
- Create test CSV with edge cases
- Test malformed data

### Integration Tests

**Test Cases:**
- [ ] Ingest full sampleData.csv successfully
- [ ] API endpoint accepts file upload
- [ ] Status endpoint returns correct counts
- [ ] Duplicate registration numbers handled
- [ ] Large file (1000+ rows) processes correctly

---

## Implementation Notes

### DO:
- ✅ Use CsvHelper for robust CSV parsing
- ✅ Stream large files (don't load all into memory)
- ✅ Log all validation errors with context
- ✅ Generate descriptions synchronously with parsing
- ✅ Handle UK-specific formats (dates, currency)
- ✅ Make parsers configurable (field mappings)

### DON'T:
- ❌ Fail entire batch for single row error
- ❌ Modify source CSV file
- ❌ Hardcode column mappings
- ❌ Skip validation (assume all data is clean)
- ❌ Load entire file into memory

### Performance Tips:
- Use async I/O for file operations
- Process in batches of 100 records
- Use StringBuilder for description generation
- Cache parsed values where appropriate

---

## Definition of Done

- [ ] CSV parser implemented and tested
- [ ] All 60 fields mapped correctly
- [ ] Data normalization working
- [ ] Validation rules implemented
- [ ] Description generation working
- [ ] API endpoints functional
- [ ] All unit tests pass (≥85% coverage)
- [ ] Integration test with full dataset passes
- [ ] Error logging comprehensive
- [ ] Code reviewed and approved
- [ ] Documentation updated

---

## Related Files

**Created/Modified:**
- `src/VehicleSearch.Infrastructure/Data/CsvDataLoader.cs`
- `src/VehicleSearch.Infrastructure/Data/DataNormalizer.cs`
- `src/VehicleSearch.Infrastructure/Data/DataValidator.cs`
- `src/VehicleSearch.Core/Entities/Vehicle.cs`
- `src/VehicleSearch.Core/Interfaces/IDataIngestionService.cs`
- `src/VehicleSearch.Api/Controllers/KnowledgeBaseController.cs`
- `tests/VehicleSearch.Infrastructure.Tests/CsvDataLoaderTests.cs`

**References:**
- FRD-005: Knowledge Base Integration (FR-1, FR-2)
- sampleData.csv (source data)
- AGENTS.md (coding standards)
