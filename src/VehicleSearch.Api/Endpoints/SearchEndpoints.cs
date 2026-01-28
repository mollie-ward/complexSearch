using Microsoft.AspNetCore.Mvc;
using VehicleSearch.Core.Interfaces;
using VehicleSearch.Core.Models;

namespace VehicleSearch.Api.Endpoints;

/// <summary>
/// Endpoints for search operations.
/// </summary>
public static class SearchEndpoints
{
    /// <summary>
    /// Maps search endpoints.
    /// </summary>
    /// <param name="app">The web application.</param>
    public static void MapSearchEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/search")
            .WithTags("Search")
            .WithOpenApi();

        // POST /api/v1/search/semantic
        group.MapPost("/semantic", async (
            SemanticSearchApiRequest request,
            [FromServices] ISemanticSearchService semanticSearchService,
            CancellationToken cancellationToken) =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Query))
                {
                    return Results.BadRequest(new { error = "Query cannot be empty" });
                }

                if (request.MaxResults <= 0 || request.MaxResults > 100)
                {
                    return Results.BadRequest(new { error = "MaxResults must be between 1 and 100" });
                }

                // Convert API request to service request
                var searchRequest = new SemanticSearchRequest
                {
                    Query = request.Query,
                    MaxResults = request.MaxResults,
                    Filters = request.Filters?.Select(f => new SearchConstraint
                    {
                        FieldName = f.FieldName,
                        Operator = Enum.Parse<ConstraintOperator>(f.Operator, ignoreCase: true),
                        Value = f.Value,
                        Type = ConstraintType.Exact // Default to exact for filters
                    }).ToList() ?? new List<SearchConstraint>()
                };

                // Execute semantic search
                var response = await semanticSearchService.SearchAsync(searchRequest, cancellationToken);

                // Convert to API response
                var apiResponse = new SemanticSearchApiResponse
                {
                    Matches = response.Matches.Select(m => new VehicleMatchResponse
                    {
                        VehicleId = m.VehicleId,
                        Vehicle = new VehicleResponse
                        {
                            Make = m.Vehicle.Make,
                            Model = m.Vehicle.Model,
                            Derivative = m.Vehicle.Derivative,
                            Price = m.Vehicle.Price,
                            Mileage = m.Vehicle.Mileage,
                            BodyType = m.Vehicle.BodyType,
                            EngineSize = m.Vehicle.EngineSize,
                            FuelType = m.Vehicle.FuelType,
                            TransmissionType = m.Vehicle.TransmissionType,
                            Colour = m.Vehicle.Colour,
                            NumberOfDoors = m.Vehicle.NumberOfDoors,
                            RegistrationDate = m.Vehicle.RegistrationDate,
                            Features = m.Vehicle.Features
                        },
                        SimilarityScore = m.SimilarityScore,
                        NormalizedScore = m.NormalizedScore
                    }).ToList(),
                    AverageScore = response.AverageScore,
                    SearchDuration = $"{response.SearchDuration.TotalMilliseconds:F2}ms"
                };

                return Results.Ok(apiResponse);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    title: "Failed to perform semantic search",
                    detail: ex.Message,
                    statusCode: 500);
            }
        })
        .WithName("SemanticSearch")
        .WithSummary("Perform semantic search using vector embeddings")
        .WithDescription("Searches for vehicles using natural language queries and vector similarity matching");

        // POST /api/v1/search/similarity
        group.MapPost("/similarity", async (
            SimilarityRequest request,
            [FromServices] IConceptualMapperService conceptualMapper,
            [FromServices] IVehicleRetrievalService vehicleRetrieval,
            CancellationToken cancellationToken) =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.VehicleId))
                {
                    return Results.BadRequest(new { error = "VehicleId is required" });
                }

                if (string.IsNullOrWhiteSpace(request.Concept))
                {
                    return Results.BadRequest(new { error = "Concept is required" });
                }

                // Get the vehicle
                var vehicle = await vehicleRetrieval.GetVehicleByIdAsync(request.VehicleId, cancellationToken);
                if (vehicle == null)
                {
                    return Results.NotFound(new { error = $"Vehicle '{request.VehicleId}' not found" });
                }

                // Map concept to attributes
                var mapping = await conceptualMapper.MapConceptToAttributesAsync(request.Concept);
                if (mapping == null)
                {
                    return Results.BadRequest(new { error = $"Unknown concept '{request.Concept}'" });
                }

                // Compute similarity
                var score = await conceptualMapper.ComputeSimilarityAsync(vehicle, mapping);

                return Results.Ok(new
                {
                    overallScore = score.OverallScore,
                    componentScores = score.ComponentScores,
                    matchingAttributes = score.MatchingAttributes,
                    mismatchingAttributes = score.MismatchingAttributes
                });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    title: "Failed to compute similarity",
                    detail: ex.Message,
                    statusCode: 500);
            }
        })
        .WithName("ComputeSimilarity")
        .WithSummary("Compute similarity score for a vehicle against a concept")
        .WithDescription("Computes multi-factor similarity between a vehicle and a qualitative concept");

        // POST /api/v1/search/explain
        group.MapPost("/explain", async (
            ExplainRequest request,
            [FromServices] IConceptualMapperService conceptualMapper,
            [FromServices] IVehicleRetrievalService vehicleRetrieval,
            CancellationToken cancellationToken) =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.VehicleId))
                {
                    return Results.BadRequest(new { error = "VehicleId is required" });
                }

                if (request.Query == null)
                {
                    return Results.BadRequest(new { error = "Query is required" });
                }

                // Get the vehicle
                var vehicle = await vehicleRetrieval.GetVehicleByIdAsync(request.VehicleId, cancellationToken);
                if (vehicle == null)
                {
                    return Results.NotFound(new { error = $"Vehicle '{request.VehicleId}' not found" });
                }

                // Generate explanation
                var explanation = await conceptualMapper.ExplainRelevanceAsync(vehicle, request.Query);

                return Results.Ok(explanation);
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    title: "Failed to generate explanation",
                    detail: ex.Message,
                    statusCode: 500);
            }
        })
        .WithName("ExplainRelevance")
        .WithSummary("Generate explanation for why a vehicle matched a query")
        .WithDescription("Provides an explainable relevance score with detailed breakdown");
    }

    /// <summary>
    /// Request model for semantic search API.
    /// </summary>
    public record SemanticSearchApiRequest
    {
        /// <summary>
        /// Gets or sets the natural language query.
        /// </summary>
        public string Query { get; init; } = string.Empty;

        /// <summary>
        /// Gets or sets the maximum number of results (1-100).
        /// </summary>
        public int MaxResults { get; init; } = 10;

        /// <summary>
        /// Gets or sets optional filters to combine with semantic search.
        /// </summary>
        public List<FilterRequest>? Filters { get; init; }
    }

    /// <summary>
    /// Filter request model.
    /// </summary>
    public record FilterRequest
    {
        /// <summary>
        /// Gets or sets the field name.
        /// </summary>
        public string FieldName { get; init; } = string.Empty;

        /// <summary>
        /// Gets or sets the operator (Equals, LessThanOrEqual, etc.).
        /// </summary>
        public string Operator { get; init; } = string.Empty;

        /// <summary>
        /// Gets or sets the filter value.
        /// </summary>
        public object Value { get; init; } = null!;
    }

    /// <summary>
    /// Response model for semantic search API.
    /// </summary>
    public record SemanticSearchApiResponse
    {
        /// <summary>
        /// Gets or sets the list of vehicle matches.
        /// </summary>
        public List<VehicleMatchResponse> Matches { get; init; } = new();

        /// <summary>
        /// Gets or sets the average similarity score.
        /// </summary>
        public double AverageScore { get; init; }

        /// <summary>
        /// Gets or sets the search duration as a formatted string.
        /// </summary>
        public string SearchDuration { get; init; } = string.Empty;
    }

    /// <summary>
    /// Vehicle match response model.
    /// </summary>
    public record VehicleMatchResponse
    {
        /// <summary>
        /// Gets or sets the vehicle ID.
        /// </summary>
        public string VehicleId { get; init; } = string.Empty;

        /// <summary>
        /// Gets or sets the vehicle details.
        /// </summary>
        public VehicleResponse Vehicle { get; init; } = null!;

        /// <summary>
        /// Gets or sets the similarity score (0-1).
        /// </summary>
        public double SimilarityScore { get; init; }

        /// <summary>
        /// Gets or sets the normalized score (0-100).
        /// </summary>
        public int NormalizedScore { get; init; }
    }

    /// <summary>
    /// Vehicle response model.
    /// </summary>
    public record VehicleResponse
    {
        /// <summary>
        /// Gets or sets the make.
        /// </summary>
        public string Make { get; init; } = string.Empty;

        /// <summary>
        /// Gets or sets the model.
        /// </summary>
        public string Model { get; init; } = string.Empty;

        /// <summary>
        /// Gets or sets the derivative.
        /// </summary>
        public string Derivative { get; init; } = string.Empty;

        /// <summary>
        /// Gets or sets the price.
        /// </summary>
        public decimal Price { get; init; }

        /// <summary>
        /// Gets or sets the mileage.
        /// </summary>
        public int Mileage { get; init; }

        /// <summary>
        /// Gets or sets the body type.
        /// </summary>
        public string BodyType { get; init; } = string.Empty;

        /// <summary>
        /// Gets or sets the engine size.
        /// </summary>
        public decimal EngineSize { get; init; }

        /// <summary>
        /// Gets or sets the fuel type.
        /// </summary>
        public string FuelType { get; init; } = string.Empty;

        /// <summary>
        /// Gets or sets the transmission type.
        /// </summary>
        public string TransmissionType { get; init; } = string.Empty;

        /// <summary>
        /// Gets or sets the colour.
        /// </summary>
        public string Colour { get; init; } = string.Empty;

        /// <summary>
        /// Gets or sets the number of doors.
        /// </summary>
        public int? NumberOfDoors { get; init; }

        /// <summary>
        /// Gets or sets the registration date.
        /// </summary>
        public DateTime? RegistrationDate { get; init; }

        /// <summary>
        /// Gets or sets the features.
        /// </summary>
        public List<string> Features { get; init; } = new();
    }

    /// <summary>
    /// Request model for similarity computation.
    /// </summary>
    public record SimilarityRequest
    {
        /// <summary>
        /// Gets or sets the vehicle ID.
        /// </summary>
        public string VehicleId { get; init; } = string.Empty;

        /// <summary>
        /// Gets or sets the concept name (e.g., "reliable", "economical").
        /// </summary>
        public string Concept { get; init; } = string.Empty;
    }

    /// <summary>
    /// Request model for explanation generation.
    /// </summary>
    public record ExplainRequest
    {
        /// <summary>
        /// Gets or sets the vehicle ID.
        /// </summary>
        public string VehicleId { get; init; } = string.Empty;

        /// <summary>
        /// Gets or sets the parsed query to explain.
        /// </summary>
        public ParsedQuery Query { get; init; } = null!;
    }
}
