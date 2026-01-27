using Microsoft.AspNetCore.Mvc;
using VehicleSearch.Core.Interfaces;

namespace VehicleSearch.Api.Endpoints;

/// <summary>
/// Knowledge base management endpoints for vehicle data ingestion.
/// </summary>
public static class KnowledgeBaseEndpoints
{
    /// <summary>
    /// Maps knowledge base endpoints.
    /// </summary>
    /// <param name="app">The web application.</param>
    public static void MapKnowledgeBaseEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/knowledge-base")
            .WithTags("Knowledge Base")
            .WithOpenApi();

        // POST /api/v1/knowledge-base/ingest
        group.MapPost("/ingest", async (
            [FromBody] IngestRequest request,
            [FromServices] IDataIngestionService ingestionService,
            CancellationToken cancellationToken) =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.FilePath))
                {
                    return Results.BadRequest(new { error = "FilePath is required" });
                }

                var result = await ingestionService.IngestFromCsvAsync(request.FilePath, cancellationToken);

                return Results.Ok(new
                {
                    success = result.Success,
                    totalRows = result.TotalRows,
                    validRows = result.ValidRows,
                    invalidRows = result.InvalidRows,
                    processingTimeMs = result.ProcessingTimeMs,
                    completedAt = result.CompletedAt,
                    errors = result.Errors.Select(e => new
                    {
                        rowNumber = e.RowNumber,
                        fieldName = e.FieldName,
                        message = e.Message,
                        value = e.Value
                    })
                });
            }
            catch (FileNotFoundException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
            catch (Exception)
            {
                return Results.Problem(
                    title: "Ingestion failed",
                    detail: "An error occurred during data ingestion. Please check the logs for details.",
                    statusCode: 500);
            }
        })
        .WithName("IngestVehicleData")
        .WithSummary("Ingest vehicle data from a CSV file")
        .WithDescription("Parses, validates, and normalizes vehicle data from a CSV file");

        // GET /api/v1/knowledge-base/status
        group.MapGet("/status", (ILogger<Program> logger) =>
        {
            // TODO: This will be enhanced when we add search index integration
            var response = new
            {
                totalVehicles = 0,
                lastIngestionDate = (DateTime?)null,
                dataSource = "CSV",
                status = "Ready"
            };

            return Results.Ok(response);
        })
        .WithName("GetKnowledgeBaseStatus")
        .WithSummary("Get knowledge base status")
        .WithDescription("Returns the current status of the knowledge base including total vehicles and last ingestion date");

        // POST /api/v1/knowledge-base/index/create
        group.MapPost("/index/create", async (
            [FromServices] ISearchIndexService indexService,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var startTime = DateTime.UtcNow;
                await indexService.CreateIndexAsync(cancellationToken);

                var status = await indexService.GetIndexStatusAsync(cancellationToken);

                return Results.Ok(new
                {
                    indexName = status.IndexName,
                    fieldsCount = 18, // Total number of fields in the schema
                    vectorFieldsCount = 1,
                    created = true,
                    timestamp = startTime
                });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (Exception)
            {
                return Results.Problem(
                    title: "Index creation failed",
                    detail: "An error occurred while creating the search index. Please check the logs for details.",
                    statusCode: 500);
            }
        })
        .WithName("CreateSearchIndex")
        .WithSummary("Create the Azure AI Search index")
        .WithDescription("Creates the search index with the full schema including vector fields for hybrid search");

        // DELETE /api/v1/knowledge-base/index
        group.MapDelete("/index", async (
            [FromServices] ISearchIndexService indexService,
            CancellationToken cancellationToken) =>
        {
            try
            {
                await indexService.DeleteIndexAsync(cancellationToken);

                return Results.Ok(new
                {
                    deleted = true,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (InvalidOperationException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
            catch (Exception)
            {
                return Results.Problem(
                    title: "Index deletion failed",
                    detail: "An error occurred while deleting the search index. Please check the logs for details.",
                    statusCode: 500);
            }
        })
        .WithName("DeleteSearchIndex")
        .WithSummary("Delete the Azure AI Search index")
        .WithDescription("Deletes the search index and all its documents");

        // GET /api/v1/knowledge-base/index/status
        group.MapGet("/index/status", async (
            [FromServices] ISearchIndexService indexService,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var status = await indexService.GetIndexStatusAsync(cancellationToken);

                return Results.Ok(new
                {
                    exists = status.Exists,
                    indexName = status.IndexName,
                    documentCount = status.DocumentCount,
                    storageSize = status.StorageSize
                });
            }
            catch (Exception)
            {
                return Results.Problem(
                    title: "Failed to get index status",
                    detail: "An error occurred while retrieving the index status. Please check the logs for details.",
                    statusCode: 500);
            }
        })
        .WithName("GetSearchIndexStatus")
        .WithSummary("Get search index status")
        .WithDescription("Returns the current status of the search index including document count and storage size");
    }

    /// <summary>
    /// Request model for ingesting data.
    /// </summary>
    public record IngestRequest
    {
        /// <summary>
        /// Gets the data source type (e.g., "csv").
        /// </summary>
        public string Source { get; init; } = "csv";

        /// <summary>
        /// Gets the file path to ingest from.
        /// </summary>
        public string FilePath { get; init; } = string.Empty;
    }
}
