# CSV Data Ingestion Pipeline

This document describes the CSV data ingestion pipeline for the Vehicle Search System.

## Overview

The CSV data ingestion pipeline processes vehicle inventory data from CSV files, validates it, normalizes it, and prepares it for indexing in Azure AI Search.

## Components

### Core Models (VehicleSearch.Core)

1. **Vehicle Entity** (`Entities/Vehicle.cs`)
   - Extended with all required fields (25+ properties)
   - Includes features, declarations, and computed description field

2. **Validation Models** (`Models/`)
   - `ValidationError`: Individual validation error with row/field/message
   - `ValidationResult`: Collection of validation errors
   - `IngestionResult`: Complete ingestion statistics and errors

3. **Service Interface** (`Interfaces/IDataIngestionService.cs`)
   - `IngestFromCsvAsync`: Main ingestion method
   - `ParseCsvAsync`: Parse CSV stream
   - `ValidateDataAsync`: Validate vehicles

### Infrastructure Services (VehicleSearch.Infrastructure/Data)

1. **CsvDataLoader**
   - Parses CSV files using CsvHelper library
   - Handles UK date format (dd/MM/yyyy)
   - Converts Yes/No to boolean
   - Extracts Equipment and Declarations as raw strings
   - Supports streaming for large files

2. **DataNormalizer**
   - Converts Make/Model/BodyType to Title Case
   - Normalizes colors (GREY → Grey, BLACK → Black)
   - Splits Equipment into Features list
   - Splits Declarations into list
   - Trims whitespace from all fields
   - Sets ProcessedDate

3. **DataValidator**
   - Required fields: Registration Number, Make, Model, Price > 0
   - Price: 0 < price < 200,000
   - Mileage: 0 ≤ mileage < 500,000
   - Engine Size: 0 < size < 10.0
   - Registration Date: 1990 ≤ year ≤ 2026
   - Number of Doors: 2, 3, 4, 5, or 7
   - Logs all validation errors with row numbers

4. **DataIngestionService**
   - Orchestrates loading, normalizing, and validating
   - Generates description field for semantic search
   - Returns detailed IngestionResult

### API Endpoints (VehicleSearch.Api/Endpoints)

**KnowledgeBaseEndpoints**

1. **POST /api/v1/knowledge-base/ingest**
   - Accepts: `{ "source": "csv", "filePath": "/path/to/file.csv" }`
   - Returns: IngestionResult with statistics and errors

2. **GET /api/v1/knowledge-base/status**
   - Returns: Status information about the knowledge base

## CSV Format

### Required Columns

| Column Name | Type | Description | Example |
|------------|------|-------------|---------|
| Registration Number | string | Unique vehicle ID | ABC123 |
| Make | string | Vehicle manufacturer | VOLKSWAGEN |
| Model | string | Vehicle model | GOLF |
| Buy Now Price | decimal | Sale price | 18500 |
| Mileage | int | Mileage in miles | 25000 |

### Optional Columns

| Column Name | Type | Description | Example |
|------------|------|-------------|---------|
| Derivative | string | Model variant | SE Nav |
| Body | string | Body type | Hatchback |
| Engine Size | decimal | Engine size in liters | 1.5 |
| Fuel | string | Fuel type | Petrol |
| Transmission | string | Transmission type | Manual |
| Colour | string | Vehicle color | BLUE |
| Number Of Doors | int | Number of doors | 5 |
| Registration Date | date | Registration date (dd/MM/yyyy) | 15/03/2020 |
| Sale Location | string | Sales location | London |
| Channel | string | Sales channel | Retail |
| Sale Type | string | Type of sale | Stock |
| Equipment | string | Comma-separated features | "Air Conditioning, Bluetooth" |
| Service History Present | bool | Has service history (Yes/No) | Yes |
| Number of Services | int | Service count | 3 |
| Last Service Date | date | Last service date | 10/01/2023 |
| MOT Expiry | date | MOT expiry date | 15/03/2025 |
| Grade | string | Vehicle grade | Grade A |
| VAT Type | string | VAT classification | VAT Qualifying |
| Additional Information | string | Extra info | Full service history |
| Declarations | string | Comma-separated declarations | HPI Clear |
| Cap Retail Price | decimal | CAP retail valuation | 19000 |
| Cap Clean Price | decimal | CAP clean valuation | 17500 |

## Usage Examples

### Using the API

```bash
# Ingest a CSV file
curl -X POST http://localhost:5000/api/v1/knowledge-base/ingest \
  -H "Content-Type: application/json" \
  -d '{
    "source": "csv",
    "filePath": "/path/to/vehicles.csv"
  }'

# Check status
curl http://localhost:5000/api/v1/knowledge-base/status
```

### Using the Service Directly

```csharp
var ingestionService = serviceProvider.GetRequiredService<IDataIngestionService>();

var result = await ingestionService.IngestFromCsvAsync("vehicles.csv");

Console.WriteLine($"Total: {result.TotalRows}");
Console.WriteLine($"Valid: {result.ValidRows}");
Console.WriteLine($"Invalid: {result.InvalidRows}");
Console.WriteLine($"Time: {result.ProcessingTimeMs}ms");
```

## Performance

- 60 records: < 5 seconds
- 10,000 records: < 60 seconds
- Uses streaming for memory efficiency
- No memory leaks

## Error Handling

The pipeline handles errors gracefully:
- Malformed rows are logged but don't stop processing
- Validation errors are collected and returned
- Individual row errors don't fail the entire batch
- File not found returns 404
- Other errors return 500 with generic message (no internal details exposed)

## Testing

### Unit Tests (27 tests)
- `CsvDataLoaderTests`: CSV parsing and field mapping
- `DataNormalizerTests`: Data normalization and transformation
- `DataValidatorTests`: Validation rules
- `DataIngestionServiceTests`: End-to-end ingestion

### Integration Tests (4 tests)
- `KnowledgeBaseEndpointsTests`: API endpoint tests

Run all tests:
```bash
dotnet test
```

## Sample Data

A sample CSV file is provided in `sample_vehicles.csv` with 10 vehicles for testing.

## Future Enhancements

- Support for other data formats (JSON, XML)
- Batch processing for very large files
- Progress reporting for long-running imports
- Duplicate detection
- Data enrichment from external sources
- Integration with Azure AI Search indexing
