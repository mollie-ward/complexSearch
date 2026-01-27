# Azure AI Search Index Integration Testing Guide

## Overview

This document provides guidance on integration testing the Azure AI Search index functionality with real Azure resources.

## Prerequisites

- Azure subscription with Azure AI Search service
- Azure AI Search service created and running
- API key with admin permissions
- .NET 8.0 SDK installed

## Setup

### 1. Create Azure AI Search Service

```bash
# Create a resource group (if needed)
az group create --name vehicle-search-rg --location eastus

# Create Azure AI Search service
az search service create \
  --name vehicle-search-service \
  --resource-group vehicle-search-rg \
  --sku basic \
  --location eastus
```

### 2. Get API Key

```bash
# Get the admin API key
az search admin-key show \
  --resource-group vehicle-search-rg \
  --service-name vehicle-search-service
```

### 3. Configure Test Environment

Create a file `tests/VehicleSearch.Infrastructure.Tests/appsettings.Test.json`:

```json
{
  "AzureAISearch": {
    "Endpoint": "https://your-search-service.search.windows.net",
    "ApiKey": "your-api-key-here",
    "IndexName": "test-vehicles-index",
    "VectorDimensions": 1536
  }
}
```

**⚠️ IMPORTANT:** Never commit API keys to source control. Add `appsettings.Test.json` to `.gitignore`.

### 4. Alternative: Use Environment Variables

Instead of a configuration file, you can set environment variables:

```bash
export AzureAISearch__Endpoint="https://your-search-service.search.windows.net"
export AzureAISearch__ApiKey="your-api-key-here"
export AzureAISearch__IndexName="test-vehicles-index"
export AzureAISearch__VectorDimensions=1536
```

## Running Integration Tests

### Run All Tests

```bash
dotnet test
```

### Run Only SearchIndexService Integration Tests

```bash
dotnet test --filter "FullyQualifiedName~SearchIndexServiceIntegrationTests"
```

### Run with Verbose Output

```bash
dotnet test -v detailed
```

## Integration Test Scenarios

### Test 1: Create Index

```csharp
[Fact]
public async Task CreateIndex_WithValidConfig_CreatesSuccessfully()
{
    // Creates the index in Azure
    // Verifies index exists
    // Checks field count and configuration
}
```

**Expected Result:**
- Index created in < 30 seconds
- All 18 fields present
- Vector field configured with 1536 dimensions
- Semantic configuration applied

### Test 2: Index Status

```csharp
[Fact]
public async Task GetIndexStatus_AfterCreation_ReturnsCorrectInfo()
{
    // Gets index statistics
    // Verifies document count
    // Checks storage size
}
```

**Expected Result:**
- Index exists
- Document count = 0 (empty index)
- Storage size reported

### Test 3: Delete Index

```csharp
[Fact]
public async Task DeleteIndex_ExistingIndex_DeletesSuccessfully()
{
    // Deletes the index
    // Verifies index no longer exists
}
```

**Expected Result:**
- Index deleted successfully
- Subsequent status check returns `Exists = false`

### Test 4: Schema Validation

```csharp
[Fact]
public async Task IndexSchema_HasCorrectFields()
{
    // Retrieves index definition
    // Validates field types and properties
    // Checks vector search configuration
}
```

**Expected Result:**
- All required fields present
- Correct field types (String, Int32, Double, DateTimeOffset)
- Filterable, facetable, searchable properties set correctly
- Vector search profile configured

## Performance Benchmarks

Based on the PRD requirements:

| Operation | Target | Typical |
|-----------|--------|---------|
| Index Creation | < 30s | 5-10s |
| Query Empty Index | < 100ms | 10-20ms |
| Get Index Status | < 100ms | 15-30ms |
| Delete Index | < 10s | 2-5s |

## Troubleshooting

### Authentication Errors

**Problem:** `401 Unauthorized`

**Solution:**
- Verify API key is correct
- Ensure API key has admin permissions
- Check endpoint URL is correct

### Index Already Exists

**Problem:** `409 Conflict - Index already exists`

**Solution:**
```bash
# Delete the existing index manually
az search index delete \
  --name test-vehicles-index \
  --service-name vehicle-search-service \
  --resource-group vehicle-search-rg
```

### Network Errors

**Problem:** `Unable to connect to the remote server`

**Solution:**
- Check firewall settings
- Verify Azure Search service is running
- Ensure endpoint URL is accessible

### Rate Limiting

**Problem:** `429 Too Many Requests`

**Solution:**
- Wait before retrying
- Increase delay between operations
- Upgrade to higher tier if needed

## Cleanup

After testing, clean up resources:

```bash
# Delete the test index
az search index delete \
  --name test-vehicles-index \
  --service-name vehicle-search-service \
  --resource-group vehicle-search-rg

# Optionally, delete the entire search service
az search service delete \
  --name vehicle-search-service \
  --resource-group vehicle-search-rg
```

## Best Practices

1. **Use Test-Specific Index Names:** Always use `test-` prefix for test indices
2. **Clean Up After Tests:** Delete test indices after test runs
3. **Isolate Tests:** Each integration test should be independent
4. **Use Basic Tier:** For testing, Basic tier is sufficient and cost-effective
5. **Monitor Costs:** Keep track of Azure costs during testing
6. **Secure Credentials:** Never commit API keys to source control
7. **Run Selectively:** Integration tests should be opt-in, not part of regular CI

## CI/CD Integration

For automated testing in CI/CD pipelines:

```yaml
# Example GitHub Actions workflow
- name: Run Integration Tests
  if: ${{ github.event_name == 'pull_request' && contains(github.event.pull_request.labels.*.name, 'integration-tests') }}
  env:
    AzureAISearch__Endpoint: ${{ secrets.AZURE_SEARCH_ENDPOINT }}
    AzureAISearch__ApiKey: ${{ secrets.AZURE_SEARCH_API_KEY }}
    AzureAISearch__IndexName: "ci-test-vehicles-index"
  run: |
    dotnet test --filter "Category=Integration"
```

## Additional Resources

- [Azure AI Search Documentation](https://docs.microsoft.com/azure/search/)
- [Azure Search SDK for .NET](https://docs.microsoft.com/dotnet/api/azure.search.documents)
- [Vector Search in Azure AI Search](https://learn.microsoft.com/azure/search/vector-search-overview)
- [Hybrid Search Concepts](https://learn.microsoft.com/azure/search/hybrid-search-overview)
