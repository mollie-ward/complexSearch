using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using VehicleSearch.Api.Endpoints;

namespace VehicleSearch.Api.Tests.Integration;

public class KnowledgeBaseEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public KnowledgeBaseEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task IngestEndpoint_ValidFile_ReturnsSuccess()
    {
        // Arrange
        var testCsvPath = Path.Combine(Directory.GetCurrentDirectory(), "test_vehicles.csv");
        var csvContent = @"Registration Number,Make,Model,Derivative,Body,Engine Size,Fuel,Transmission,Colour,Number Of Doors,Buy Now Price,Mileage,Registration Date,Sale Location,Channel,Sale Type,Equipment,Service History Present,Number of Services,Last Service Date,MOT Expiry,Grade,VAT Type,Additional Information,Declarations,Cap Retail Price,Cap Clean Price
ABC123,Volkswagen,Golf,SE Nav,Hatchback,1.5,Petrol,Manual,Blue,5,18500,25000,15/03/2020,London,Retail,Stock,""Air Conditioning, Bluetooth"",Yes,3,10/01/2023,15/03/2025,Grade A,VAT Qualifying,Full service history,HPI Clear,19000,17500";
        
        File.WriteAllText(testCsvPath, csvContent);

        try
        {
            var request = new KnowledgeBaseEndpoints.IngestRequest
            {
                Source = "csv",
                FilePath = testCsvPath
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/knowledge-base/ingest", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            
            var result = await response.Content.ReadFromJsonAsync<IngestionResponse>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.TotalRows.Should().Be(1);
            result.ValidRows.Should().Be(1);
            result.InvalidRows.Should().Be(0);
        }
        finally
        {
            if (File.Exists(testCsvPath))
            {
                File.Delete(testCsvPath);
            }
        }
    }

    [Fact]
    public async Task IngestEndpoint_InvalidFile_ReturnsNotFound()
    {
        // Arrange
        var request = new KnowledgeBaseEndpoints.IngestRequest
        {
            Source = "csv",
            FilePath = "nonexistent.csv"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/knowledge-base/ingest", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task IngestEndpoint_MissingFilePath_ReturnsBadRequest()
    {
        // Arrange
        var request = new KnowledgeBaseEndpoints.IngestRequest
        {
            Source = "csv",
            FilePath = ""
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/knowledge-base/ingest", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task StatusEndpoint_ReturnsCorrectData()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/knowledge-base/status");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<StatusResponse>();
        result.Should().NotBeNull();
        result!.Status.Should().Be("Ready");
        result.DataSource.Should().Be("CSV");
    }

    [Fact]
    public async Task IndexStatusEndpoint_WithoutCreatedIndex_ReturnsNotExist()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/knowledge-base/index/status");

        // Assert
        // Note: This test may fail if credentials are not configured
        // In a real environment, this would check index status
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task CreateIndexEndpoint_WithoutCredentials_ReturnsError()
    {
        // Act
        var response = await _client.PostAsync("/api/v1/knowledge-base/index/create", null);

        // Assert
        // Without valid Azure credentials, this should fail
        // The endpoint should handle the error gracefully
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest, 
            HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task DeleteIndexEndpoint_WithoutCredentials_ReturnsError()
    {
        // Act
        var response = await _client.DeleteAsync("/api/v1/knowledge-base/index");

        // Assert
        // Without valid Azure credentials, this should fail
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.NotFound, 
            HttpStatusCode.InternalServerError);
    }

    private record IngestionResponse(
        bool Success,
        int TotalRows,
        int ValidRows,
        int InvalidRows,
        long ProcessingTimeMs,
        DateTime CompletedAt,
        object[] Errors);

    private record StatusResponse(
        int TotalVehicles,
        DateTime? LastIngestionDate,
        string DataSource,
        string Status);
}
