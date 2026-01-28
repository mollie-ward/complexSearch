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

        // POST /api/v1/query/map
        group.MapPost("/map", async (
            MapQueryRequest request,
            [FromServices] IAttributeMapperService mapperService,
            CancellationToken cancellationToken) =>
        {
            try
            {
                if (request.ParsedQuery == null)
                {
                    return Results.BadRequest(new { error = "ParsedQuery cannot be null" });
                }

                var mappedQuery = await mapperService.MapToSearchQueryAsync(request.ParsedQuery, cancellationToken);

                var response = new MapQueryResponse
                {
                    Constraints = mappedQuery.Constraints.Select(c => new ConstraintResponse
                    {
                        FieldName = c.FieldName,
                        Operator = c.Operator.ToString(),
                        Value = c.Value,
                        Type = c.Type.ToString()
                    }).ToList(),
                    UnmappableTerms = mappedQuery.UnmappableTerms,
                    Metadata = mappedQuery.Metadata
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
                    title: "Failed to map query",
                    detail: ex.Message,
                    statusCode: 500);
            }
        })
        .WithName("MapQuery")
        .WithSummary("Map parsed query to search constraints")
        .WithDescription("Maps extracted entities from a parsed query into structured search constraints");

        // POST /api/v1/query/compose
        group.MapPost("/compose", async (
            ComposeQueryRequest request,
            [FromServices] IQueryComposerService composerService,
            CancellationToken cancellationToken) =>
        {
            try
            {
                if (request.MappedQuery == null)
                {
                    return Results.BadRequest(new { error = "MappedQuery cannot be null" });
                }

                var composedQuery = await composerService.ComposeQueryAsync(request.MappedQuery, cancellationToken);

                var response = new ComposeQueryResponse
                {
                    Type = composedQuery.Type.ToString(),
                    ConstraintGroups = composedQuery.ConstraintGroups.Select(g => new ConstraintGroupResponse
                    {
                        Constraints = g.Constraints.Select(c => new ConstraintResponse
                        {
                            FieldName = c.FieldName,
                            Operator = c.Operator.ToString(),
                            Value = c.Value,
                            Type = c.Type.ToString()
                        }).ToList(),
                        Operator = g.Operator.ToString(),
                        Priority = g.Priority
                    }).ToList(),
                    GroupOperator = composedQuery.GroupOperator.ToString(),
                    Warnings = composedQuery.Warnings,
                    HasConflicts = composedQuery.HasConflicts,
                    ODataFilter = composedQuery.ODataFilter
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
                    title: "Failed to compose query",
                    detail: ex.Message,
                    statusCode: 500);
            }
        })
        .WithName("ComposeQuery")
        .WithSummary("Compose complex search query")
        .WithDescription("Composes a complex search query from mapped constraints with logical operators and conflict resolution");

        // POST /api/v1/query/resolve
        group.MapPost("/resolve", async (
            ResolveReferencesRequest request,
            [FromServices] IReferenceResolverService referenceResolver,
            [FromServices] IConversationSessionService sessionService,
            CancellationToken cancellationToken) =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Query))
                {
                    return Results.BadRequest(new { error = "Query cannot be empty" });
                }

                if (string.IsNullOrWhiteSpace(request.SessionId))
                {
                    return Results.BadRequest(new { error = "SessionId is required" });
                }

                // Get the conversation session
                var session = await sessionService.GetSessionAsync(request.SessionId, cancellationToken);

                // Resolve references
                var resolvedQuery = await referenceResolver.ResolveReferencesAsync(
                    request.Query, 
                    session, 
                    cancellationToken);

                var response = new ResolveReferencesResponse
                {
                    OriginalQuery = resolvedQuery.OriginalQuery,
                    ResolvedQuery = resolvedQuery.ResolvedQueryText,
                    ResolvedReferences = resolvedQuery.ResolvedReferences.Select(r => new ReferenceResponse
                    {
                        ReferenceText = r.ReferenceText,
                        Type = r.Type.ToString(),
                        ResolvedValue = r.ResolvedValue?.ToString() ?? string.Empty,
                        Position = r.Position
                    }).ToList(),
                    ResolvedValues = resolvedQuery.ResolvedValues,
                    HasUnresolvedReferences = resolvedQuery.HasUnresolvedReferences,
                    UnresolvedMessage = resolvedQuery.UnresolvedMessage
                };

                return Results.Ok(response);
            }
            catch (VehicleSearch.Core.Exceptions.SessionNotFoundException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    title: "Failed to resolve references",
                    detail: ex.Message,
                    statusCode: 500);
            }
        })
        .WithName("ResolveReferences")
        .WithSummary("Resolve references in query")
        .WithDescription("Resolves pronouns and references in a query using conversation context");

        // POST /api/v1/query/refine
        group.MapPost("/refine", async (
            RefineQueryRequest request,
            [FromServices] IReferenceResolverService referenceResolver,
            [FromServices] IConversationSessionService sessionService,
            [FromServices] IQueryUnderstandingService queryService,
            CancellationToken cancellationToken) =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Query))
                {
                    return Results.BadRequest(new { error = "Query cannot be empty" });
                }

                if (string.IsNullOrWhiteSpace(request.SessionId))
                {
                    return Results.BadRequest(new { error = "SessionId is required" });
                }

                // Get the conversation session
                var session = await sessionService.GetSessionAsync(request.SessionId, cancellationToken);

                if (session.CurrentSearchState == null)
                {
                    return Results.BadRequest(new { error = "No previous search state found in session" });
                }

                // Parse the new query
                var parsedQuery = await queryService.ParseQueryAsync(request.Query, null, cancellationToken);

                // Refine the query with previous context
                var composedQuery = await referenceResolver.RefineQueryAsync(
                    parsedQuery, 
                    session.CurrentSearchState, 
                    cancellationToken);

                // Determine which constraints were added, updated, or removed
                var previousFields = session.CurrentSearchState.ActiveFilters.Keys.ToHashSet();
                var currentFields = new HashSet<string>();
                foreach (var group in composedQuery.ConstraintGroups)
                {
                    foreach (var constraint in group.Constraints)
                    {
                        currentFields.Add(constraint.FieldName);
                    }
                }

                var addedConstraints = currentFields.Except(previousFields).ToList();
                var updatedConstraints = currentFields.Intersect(previousFields).ToList();
                var removedConstraints = previousFields.Except(currentFields).ToList();

                var response = new RefineQueryResponse
                {
                    ComposedQuery = new ComposeQueryResponse
                    {
                        Type = composedQuery.Type.ToString(),
                        ConstraintGroups = composedQuery.ConstraintGroups.Select(g => new ConstraintGroupResponse
                        {
                            Constraints = g.Constraints.Select(c => new ConstraintResponse
                            {
                                FieldName = c.FieldName,
                                Operator = c.Operator.ToString(),
                                Value = c.Value,
                                Type = c.Type.ToString()
                            }).ToList(),
                            Operator = g.Operator.ToString(),
                            Priority = g.Priority
                        }).ToList(),
                        GroupOperator = composedQuery.GroupOperator.ToString(),
                        Warnings = composedQuery.Warnings,
                        HasConflicts = composedQuery.HasConflicts,
                        ODataFilter = composedQuery.ODataFilter
                    },
                    AddedConstraints = addedConstraints,
                    UpdatedConstraints = updatedConstraints,
                    RemovedConstraints = removedConstraints
                };

                return Results.Ok(response);
            }
            catch (VehicleSearch.Core.Exceptions.SessionNotFoundException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    title: "Failed to refine query",
                    detail: ex.Message,
                    statusCode: 500);
            }
        })
        .WithName("RefineQuery")
        .WithSummary("Refine query with previous context")
        .WithDescription("Refines a query by combining new constraints with previous search state");
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

    /// <summary>
    /// Request model for mapping query.
    /// </summary>
    public record MapQueryRequest
    {
        /// <summary>
        /// The parsed query with entities to map.
        /// </summary>
        public ParsedQuery ParsedQuery { get; init; } = null!;
    }

    /// <summary>
    /// Response model for mapped query.
    /// </summary>
    public record MapQueryResponse
    {
        /// <summary>
        /// The list of search constraints.
        /// </summary>
        public List<ConstraintResponse> Constraints { get; init; } = new();

        /// <summary>
        /// Terms that could not be mapped.
        /// </summary>
        public List<string> UnmappableTerms { get; init; } = new();

        /// <summary>
        /// Metadata about the mapping.
        /// </summary>
        public Dictionary<string, object> Metadata { get; init; } = new();
    }

    /// <summary>
    /// Response model for a constraint.
    /// </summary>
    public record ConstraintResponse
    {
        /// <summary>
        /// Field name in the search index.
        /// </summary>
        public string FieldName { get; init; } = string.Empty;

        /// <summary>
        /// Constraint operator.
        /// </summary>
        public string Operator { get; init; } = string.Empty;

        /// <summary>
        /// Constraint value.
        /// </summary>
        public object Value { get; init; } = null!;

        /// <summary>
        /// Constraint type.
        /// </summary>
        public string Type { get; init; } = string.Empty;
    }

    /// <summary>
    /// Request model for composing query.
    /// </summary>
    public record ComposeQueryRequest
    {
        /// <summary>
        /// The mapped query with constraints to compose.
        /// </summary>
        public MappedQuery MappedQuery { get; init; } = null!;
    }

    /// <summary>
    /// Response model for composed query.
    /// </summary>
    public record ComposeQueryResponse
    {
        /// <summary>
        /// The type of the composed query.
        /// </summary>
        public string Type { get; init; } = string.Empty;

        /// <summary>
        /// The list of constraint groups.
        /// </summary>
        public List<ConstraintGroupResponse> ConstraintGroups { get; init; } = new();

        /// <summary>
        /// The logical operator combining constraint groups.
        /// </summary>
        public string GroupOperator { get; init; } = string.Empty;

        /// <summary>
        /// Warnings generated during composition.
        /// </summary>
        public List<string> Warnings { get; init; } = new();

        /// <summary>
        /// Whether the query has conflicting constraints.
        /// </summary>
        public bool HasConflicts { get; init; }

        /// <summary>
        /// The OData filter string for Azure Search.
        /// </summary>
        public string? ODataFilter { get; init; }
    }

    /// <summary>
    /// Response model for a constraint group.
    /// </summary>
    public record ConstraintGroupResponse
    {
        /// <summary>
        /// The list of constraints in this group.
        /// </summary>
        public List<ConstraintResponse> Constraints { get; init; } = new();

        /// <summary>
        /// The logical operator for this group.
        /// </summary>
        public string Operator { get; init; } = string.Empty;

        /// <summary>
        /// The priority of this constraint group.
        /// </summary>
        public double Priority { get; init; }
    }

    /// <summary>
    /// Request model for resolving references.
    /// </summary>
    public record ResolveReferencesRequest
    {
        /// <summary>
        /// The user query containing references.
        /// </summary>
        public string Query { get; init; } = string.Empty;

        /// <summary>
        /// The session identifier for conversation context.
        /// </summary>
        public string SessionId { get; init; } = string.Empty;
    }

    /// <summary>
    /// Response model for resolved references.
    /// </summary>
    public record ResolveReferencesResponse
    {
        /// <summary>
        /// The original query.
        /// </summary>
        public string OriginalQuery { get; init; } = string.Empty;

        /// <summary>
        /// The query with references replaced.
        /// </summary>
        public string ResolvedQuery { get; init; } = string.Empty;

        /// <summary>
        /// List of resolved references.
        /// </summary>
        public List<ReferenceResponse> ResolvedReferences { get; init; } = new();

        /// <summary>
        /// Dictionary of resolved constraint values.
        /// </summary>
        public Dictionary<string, object> ResolvedValues { get; init; } = new();

        /// <summary>
        /// Flag indicating if there are unresolved references.
        /// </summary>
        public bool HasUnresolvedReferences { get; init; }

        /// <summary>
        /// Message for unresolved references.
        /// </summary>
        public string? UnresolvedMessage { get; init; }
    }

    /// <summary>
    /// Response model for a reference.
    /// </summary>
    public record ReferenceResponse
    {
        /// <summary>
        /// The reference text.
        /// </summary>
        public string ReferenceText { get; init; } = string.Empty;

        /// <summary>
        /// The reference type.
        /// </summary>
        public string Type { get; init; } = string.Empty;

        /// <summary>
        /// The resolved value.
        /// </summary>
        public string ResolvedValue { get; init; } = string.Empty;

        /// <summary>
        /// The position in the query.
        /// </summary>
        public int Position { get; init; }
    }

    /// <summary>
    /// Request model for refining a query.
    /// </summary>
    public record RefineQueryRequest
    {
        /// <summary>
        /// The new query to refine with.
        /// </summary>
        public string Query { get; init; } = string.Empty;

        /// <summary>
        /// The session identifier for conversation context.
        /// </summary>
        public string SessionId { get; init; } = string.Empty;
    }

    /// <summary>
    /// Response model for refined query.
    /// </summary>
    public record RefineQueryResponse
    {
        /// <summary>
        /// The composed query with merged constraints.
        /// </summary>
        public ComposeQueryResponse ComposedQuery { get; init; } = null!;

        /// <summary>
        /// List of newly added constraint field names.
        /// </summary>
        public List<string> AddedConstraints { get; init; } = new();

        /// <summary>
        /// List of updated constraint field names.
        /// </summary>
        public List<string> UpdatedConstraints { get; init; } = new();

        /// <summary>
        /// List of removed constraint field names.
        /// </summary>
        public List<string> RemovedConstraints { get; init; } = new();
    }
}
