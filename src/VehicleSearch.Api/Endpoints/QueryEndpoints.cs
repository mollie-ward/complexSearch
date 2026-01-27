using Microsoft.AspNetCore.Mvc;
using VehicleSearch.Core.Interfaces;
using VehicleSearch.Core.Models;

namespace VehicleSearch.Api.Endpoints;

/// <summary>
/// Endpoints for query understanding operations.
/// </summary>
public static class QueryEndpoints
{
    /// <summary>
    /// Maps query understanding endpoints.
    /// </summary>
    /// <param name="app">The web application.</param>
    public static void MapQueryEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/query")
            .WithTags("Query Understanding")
            .WithOpenApi();

        // POST /api/v1/query/parse
        group.MapPost("/parse", async (
            ParseQueryRequest request,
            [FromServices] IQueryUnderstandingService queryService,
            CancellationToken cancellationToken) =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Query))
                {
                    return Results.BadRequest(new { error = "Query cannot be empty" });
                }

                // Build conversation context if conversationId is provided
                ConversationContext? context = null;
                if (!string.IsNullOrWhiteSpace(request.ConversationId))
                {
                    context = new ConversationContext
                    {
                        SessionId = request.ConversationId,
                        History = new List<string>() // TODO: Load from conversation history store
                    };
                }

                var result = await queryService.ParseQueryAsync(request.Query, context, cancellationToken);

                var response = new ParseQueryResponse
                {
                    OriginalQuery = result.OriginalQuery,
                    Intent = result.Intent.ToString().ToLower(),
                    Confidence = result.ConfidenceScore,
                    Entities = result.Entities.Select(e => new EntityResponse
                    {
                        Type = e.Type.ToString(),
                        Value = e.Value,
                        Confidence = e.Confidence,
                        StartPosition = e.StartPosition,
                        EndPosition = e.EndPosition
                    }).ToList(),
                    UnmappedTerms = result.UnmappedTerms
                };

                return Results.Ok(response);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    title: "Failed to parse query",
                    detail: ex.Message,
                    statusCode: 500);
            }
        })
        .WithName("ParseQuery")
        .WithSummary("Parse natural language query")
        .WithDescription("Classifies intent and extracts entities from a user query");

        // POST /api/v1/query/intent
        group.MapPost("/intent", async (
            ParseQueryRequest request,
            [FromServices] IQueryUnderstandingService queryService,
            CancellationToken cancellationToken) =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Query))
                {
                    return Results.BadRequest(new { error = "Query cannot be empty" });
                }

                ConversationContext? context = null;
                if (!string.IsNullOrWhiteSpace(request.ConversationId))
                {
                    context = new ConversationContext
                    {
                        SessionId = request.ConversationId
                    };
                }

                var intent = await queryService.ClassifyIntentAsync(request.Query, context, cancellationToken);

                return Results.Ok(new { intent = intent.ToString().ToLower() });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    title: "Failed to classify intent",
                    detail: ex.Message,
                    statusCode: 500);
            }
        })
        .WithName("ClassifyIntent")
        .WithSummary("Classify query intent")
        .WithDescription("Classifies the intent of a user query");

        // POST /api/v1/query/entities
        group.MapPost("/entities", async (
            ParseQueryRequest request,
            [FromServices] IQueryUnderstandingService queryService,
            CancellationToken cancellationToken) =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Query))
                {
                    return Results.BadRequest(new { error = "Query cannot be empty" });
                }

                var entities = await queryService.ExtractEntitiesAsync(request.Query, cancellationToken);

                var response = entities.Select(e => new EntityResponse
                {
                    Type = e.Type.ToString(),
                    Value = e.Value,
                    Confidence = e.Confidence,
                    StartPosition = e.StartPosition,
                    EndPosition = e.EndPosition
                }).ToList();

                return Results.Ok(response);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    title: "Failed to extract entities",
                    detail: ex.Message,
                    statusCode: 500);
            }
        })
        .WithName("ExtractEntities")
        .WithSummary("Extract entities from query")
        .WithDescription("Extracts vehicle-related entities from a user query");
    }

    /// <summary>
    /// Request model for query parsing.
    /// </summary>
    public record ParseQueryRequest
    {
        /// <summary>
        /// The user query to parse.
        /// </summary>
        public string Query { get; init; } = string.Empty;

        /// <summary>
        /// Optional conversation identifier for context.
        /// </summary>
        public string? ConversationId { get; init; }
    }

    /// <summary>
    /// Response model for parsed query.
    /// </summary>
    public record ParseQueryResponse
    {
        /// <summary>
        /// The original query.
        /// </summary>
        public string OriginalQuery { get; init; } = string.Empty;

        /// <summary>
        /// The classified intent.
        /// </summary>
        public string Intent { get; init; } = string.Empty;

        /// <summary>
        /// Overall confidence score.
        /// </summary>
        public double Confidence { get; init; }

        /// <summary>
        /// Extracted entities.
        /// </summary>
        public List<EntityResponse> Entities { get; init; } = new();

        /// <summary>
        /// Terms that could not be mapped.
        /// </summary>
        public List<string> UnmappedTerms { get; init; } = new();
    }

    /// <summary>
    /// Response model for an entity.
    /// </summary>
    public record EntityResponse
    {
        /// <summary>
        /// Entity type.
        /// </summary>
        public string Type { get; init; } = string.Empty;

        /// <summary>
        /// Entity value.
        /// </summary>
        public string Value { get; init; } = string.Empty;

        /// <summary>
        /// Confidence score.
        /// </summary>
        public double Confidence { get; init; }

        /// <summary>
        /// Start position in query.
        /// </summary>
        public int StartPosition { get; init; }

        /// <summary>
        /// End position in query.
        /// </summary>
        public int EndPosition { get; init; }
    }
}
