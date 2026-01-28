using Microsoft.AspNetCore.Mvc;
using VehicleSearch.Core.Enums;
using VehicleSearch.Core.Exceptions;
using VehicleSearch.Core.Interfaces;
using VehicleSearch.Core.Models;

namespace VehicleSearch.Api.Endpoints;

/// <summary>
/// Endpoints for conversation session management.
/// </summary>
public static class ConversationEndpoints
{
    /// <summary>
    /// Maps conversation session endpoints.
    /// </summary>
    /// <param name="app">The web application.</param>
    public static void MapConversationEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/conversation")
            .WithTags("Conversation")
            .WithOpenApi();

        // POST /api/v1/conversation
        group.MapPost("", async (
            [FromServices] IConversationSessionService sessionService,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var session = await sessionService.CreateSessionAsync(cancellationToken);

                return Results.Ok(new CreateSessionResponse
                {
                    SessionId = session.SessionId,
                    CreatedAt = session.CreatedAt
                });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    title: "Failed to create session",
                    detail: ex.Message,
                    statusCode: 500);
            }
        })
        .WithName("CreateConversationSession")
        .WithSummary("Create new conversation session")
        .WithDescription("Creates a new conversation session with a unique ID");

        // GET /api/v1/conversation/{sessionId}
        group.MapGet("{sessionId}", async (
            string sessionId,
            [FromServices] IConversationSessionService sessionService,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var session = await sessionService.GetSessionAsync(sessionId, cancellationToken);

                return Results.Ok(new GetSessionResponse
                {
                    SessionId = session.SessionId,
                    CreatedAt = session.CreatedAt,
                    LastAccessedAt = session.LastAccessedAt,
                    MessageCount = session.MessageCount,
                    CurrentSearchState = session.CurrentSearchState
                });
            }
            catch (SessionNotFoundException ex)
            {
                return Results.NotFound(new { error = ex.Message, sessionId = ex.SessionId });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    title: "Failed to retrieve session",
                    detail: ex.Message,
                    statusCode: 500);
            }
        })
        .WithName("GetConversationSession")
        .WithSummary("Get session details")
        .WithDescription("Retrieves details of an existing conversation session");

        // GET /api/v1/conversation/{sessionId}/history
        group.MapGet("{sessionId}/history", async (
            string sessionId,
            [FromQuery] int? maxMessages,
            [FromServices] IConversationSessionService sessionService,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var history = await sessionService.GetHistoryAsync(
                    sessionId,
                    maxMessages ?? 10,
                    cancellationToken);

                return Results.Ok(history);
            }
            catch (SessionNotFoundException ex)
            {
                return Results.NotFound(new { error = ex.Message, sessionId = ex.SessionId });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    title: "Failed to retrieve conversation history",
                    detail: ex.Message,
                    statusCode: 500);
            }
        })
        .WithName("GetConversationHistory")
        .WithSummary("Get conversation history")
        .WithDescription("Retrieves the conversation history for a session");

        // DELETE /api/v1/conversation/{sessionId}
        group.MapDelete("{sessionId}", async (
            string sessionId,
            [FromServices] IConversationSessionService sessionService,
            CancellationToken cancellationToken) =>
        {
            try
            {
                await sessionService.ClearSessionAsync(sessionId, cancellationToken);

                return Results.Ok(new ClearSessionResponse
                {
                    Success = true,
                    Message = "Session cleared successfully"
                });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    title: "Failed to clear session",
                    detail: ex.Message,
                    statusCode: 500);
            }
        })
        .WithName("ClearConversationSession")
        .WithSummary("Clear/delete session")
        .WithDescription("Deletes a conversation session and all its data");
    }

    /// <summary>
    /// Response model for session creation.
    /// </summary>
    public record CreateSessionResponse
    {
        /// <summary>
        /// Gets or sets the session identifier.
        /// </summary>
        public string SessionId { get; init; } = string.Empty;

        /// <summary>
        /// Gets or sets the creation timestamp.
        /// </summary>
        public DateTime CreatedAt { get; init; }
    }

    /// <summary>
    /// Response model for getting session details.
    /// </summary>
    public record GetSessionResponse
    {
        /// <summary>
        /// Gets or sets the session identifier.
        /// </summary>
        public string SessionId { get; init; } = string.Empty;

        /// <summary>
        /// Gets or sets the creation timestamp.
        /// </summary>
        public DateTime CreatedAt { get; init; }

        /// <summary>
        /// Gets or sets the last accessed timestamp.
        /// </summary>
        public DateTime LastAccessedAt { get; init; }

        /// <summary>
        /// Gets or sets the total number of messages.
        /// </summary>
        public int MessageCount { get; init; }

        /// <summary>
        /// Gets or sets the current search state.
        /// </summary>
        public SearchState? CurrentSearchState { get; init; }
    }

    /// <summary>
    /// Response model for clearing session.
    /// </summary>
    public record ClearSessionResponse
    {
        /// <summary>
        /// Gets or sets whether the operation was successful.
        /// </summary>
        public bool Success { get; init; }

        /// <summary>
        /// Gets or sets the response message.
        /// </summary>
        public string Message { get; init; } = string.Empty;
    }
}
