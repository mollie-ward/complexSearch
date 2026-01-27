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
