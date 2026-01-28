using Microsoft.AspNetCore.Mvc;
using VehicleSearch.Core.Entities;
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

        // POST /api/v1/search
        group.MapPost("", async (
            OrchestratedSearchRequest request,
            [FromServices] IQueryComposerService queryComposer,
            [FromServices] ISearchOrchestratorService searchOrchestrator,
            CancellationToken cancellationToken) =>
        {
            try
            {
                if (request.ComposedQuery == null)
                {
                    return Results.BadRequest(new { error = "ComposedQuery is required" });
                }

                if (request.MaxResults <= 0 || request.MaxResults > 100)
                {
                    return Results.BadRequest(new { error = "MaxResults must be between 1 and 100" });
                }

                // Determine strategy
                var strategy = await searchOrchestrator.DetermineStrategyAsync(
                    request.ComposedQuery,
                    cancellationToken);

                // Execute search
                var results = await searchOrchestrator.ExecuteSearchAsync(
                    request.ComposedQuery,
                    strategy,
                    request.MaxResults,
                    cancellationToken);

                // Convert to API response
                var response = new OrchestratedSearchResponse
                {
                    Results = results.Results.Select(r => new VehicleSearchResult
                    {
                        Vehicle = new VehicleResponse
                        {
                            Make = r.Vehicle.Make,
                            Model = r.Vehicle.Model,
                            Derivative = r.Vehicle.Derivative,
                            Price = r.Vehicle.Price,
                            Mileage = r.Vehicle.Mileage,
                            BodyType = r.Vehicle.BodyType,
                            EngineSize = r.Vehicle.EngineSize,
                            FuelType = r.Vehicle.FuelType,
                            TransmissionType = r.Vehicle.TransmissionType,
                            Colour = r.Vehicle.Colour,
                            NumberOfDoors = r.Vehicle.NumberOfDoors,
                            RegistrationDate = r.Vehicle.RegistrationDate,
                            Features = r.Vehicle.Features
                        },
                        RelevanceScore = r.Score,
                        ScoreBreakdown = r.ScoreBreakdown != null ? new ScoreBreakdownResponse
                        {
                            ExactMatchScore = r.ScoreBreakdown.ExactMatchScore,
                            SemanticScore = r.ScoreBreakdown.SemanticScore,
                            KeywordScore = r.ScoreBreakdown.KeywordScore,
                            FinalScore = r.ScoreBreakdown.FinalScore
                        } : null
                    }).ToList(),
                    TotalCount = results.TotalCount,
                    Strategy = new SearchStrategyResponse
                    {
                        Type = results.Strategy.Type.ToString(),
                        Approaches = results.Strategy.Approaches.Select(a => a.ToString()).ToList(),
                        Weights = results.Strategy.Weights.ToDictionary(
                            kvp => kvp.Key.ToString(),
                            kvp => kvp.Value)
                    },
                    SearchDuration = $"{results.SearchDuration.TotalMilliseconds:F2}ms"
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
                    title: "Failed to perform search",
                    detail: ex.Message,
                    statusCode: 500);
            }
        })
        .WithName("OrchestrationSearch")
        .WithSummary("Execute an orchestrated search with automatic strategy selection")
        .WithDescription("Executes a search using the optimal combination of exact match, semantic search, and filtering based on query characteristics");

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
                    mismatchingAttributes = score.MismatchingAttributes,
                    descriptionBoost = score.DescriptionBoost
                });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (Exception)
            {
                return Results.Problem(
                    title: "Failed to compute similarity",
                    detail: "An unexpected error occurred while computing similarity score",
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
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (Exception)
            {
                return Results.Problem(
                    title: "Failed to generate explanation",
                    detail: "An unexpected error occurred while generating explanation",
                    statusCode: 500);
            }
        })
        .WithName("ExplainRelevance")
        .WithSummary("Generate explanation for why a vehicle matched a query")
        .WithDescription("Provides an explainable relevance score with detailed breakdown");

        // POST /api/v1/search/rerank
        group.MapPost("/rerank", async (
            RerankRequest request,
            [FromServices] IResultRankingService rankingService,
            CancellationToken cancellationToken) =>
        {
            try
            {
                if (request.Results == null || !request.Results.Any())
                {
                    return Results.BadRequest(new { error = "Results list cannot be empty" });
                }

                if (request.Query == null)
                {
                    return Results.BadRequest(new { error = "Query is required for re-ranking" });
                }

                // Validate all results have required vehicle data
                if (request.Results.Any(r => r.Vehicle == null))
                {
                    return Results.BadRequest(new { error = "All results must have valid vehicle data" });
                }

                // Convert API request to service models
                var vehicleResults = request.Results.Select(r => new VehicleResult
                {
                    Vehicle = new Vehicle
                    {
                        Id = r.VehicleId,
                        Make = r.Vehicle.Make,
                        Model = r.Vehicle.Model,
                        Derivative = r.Vehicle.Derivative,
                        Price = r.Vehicle.Price,
                        Mileage = r.Vehicle.Mileage,
                        BodyType = r.Vehicle.BodyType,
                        EngineSize = r.Vehicle.EngineSize,
                        FuelType = r.Vehicle.FuelType,
                        TransmissionType = r.Vehicle.TransmissionType,
                        Colour = r.Vehicle.Colour,
                        NumberOfDoors = r.Vehicle.NumberOfDoors,
                        RegistrationDate = r.Vehicle.RegistrationDate,
                        Features = r.Vehicle.Features,
                        ServiceHistoryPresent = r.Vehicle.ServiceHistoryPresent ?? false,
                        NumberOfServices = r.Vehicle.NumberOfServices,
                        LastServiceDate = r.Vehicle.LastServiceDate,
                        MotExpiryDate = r.Vehicle.MotExpiryDate,
                        Declarations = r.Vehicle.Declarations ?? new List<string>()
                    },
                    Score = r.RelevanceScore,
                    ScoreBreakdown = r.ScoreBreakdown != null ? new SearchScoreBreakdown
                    {
                        ExactMatchScore = r.ScoreBreakdown.ExactMatchScore,
                        SemanticScore = r.ScoreBreakdown.SemanticScore,
                        KeywordScore = r.ScoreBreakdown.KeywordScore,
                        FinalScore = r.ScoreBreakdown.FinalScore
                    } : null
                }).ToList();

                // Re-rank using the specified strategy or default
                List<VehicleResult> rerankedResults;
                if (request.Strategy != null)
                {
                    var strategy = new RerankingStrategy
                    {
                        Approach = Enum.Parse<RerankingApproach>(request.Strategy.Approach, ignoreCase: true),
                        FactorWeights = request.Strategy.FactorWeights?.ToDictionary(
                            kvp => Enum.Parse<RankingFactor>(kvp.Key, ignoreCase: true),
                            kvp => kvp.Value) ?? new Dictionary<RankingFactor, double>(),
                        ApplyDiversity = request.Strategy.ApplyDiversity ?? true,
                        MaxPerMake = request.Strategy.MaxPerMake ?? 3,
                        MaxPerModel = request.Strategy.MaxPerModel ?? 2
                    };

                    rerankedResults = await rankingService.RerankResultsAsync(
                        vehicleResults,
                        strategy,
                        request.Query,
                        cancellationToken);
                }
                else
                {
                    rerankedResults = await rankingService.RankResultsAsync(
                        vehicleResults,
                        request.Query,
                        cancellationToken);
                }

                // Convert back to API response
                var response = new RerankResponse
                {
                    Results = rerankedResults.Select(r => new VehicleSearchResult
                    {
                        Vehicle = new VehicleResponse
                        {
                            Make = r.Vehicle.Make,
                            Model = r.Vehicle.Model,
                            Derivative = r.Vehicle.Derivative,
                            Price = r.Vehicle.Price,
                            Mileage = r.Vehicle.Mileage,
                            BodyType = r.Vehicle.BodyType,
                            EngineSize = r.Vehicle.EngineSize,
                            FuelType = r.Vehicle.FuelType,
                            TransmissionType = r.Vehicle.TransmissionType,
                            Colour = r.Vehicle.Colour,
                            NumberOfDoors = r.Vehicle.NumberOfDoors,
                            RegistrationDate = r.Vehicle.RegistrationDate,
                            Features = r.Vehicle.Features
                        },
                        RelevanceScore = r.Score,
                        ScoreBreakdown = r.ScoreBreakdown != null ? new ScoreBreakdownResponse
                        {
                            ExactMatchScore = r.ScoreBreakdown.ExactMatchScore,
                            SemanticScore = r.ScoreBreakdown.SemanticScore,
                            KeywordScore = r.ScoreBreakdown.KeywordScore,
                            FinalScore = r.ScoreBreakdown.FinalScore
                        } : null
                    }).ToList()
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
                    title: "Failed to re-rank results",
                    detail: ex.Message,
                    statusCode: 500);
            }
        })
        .WithName("RerankResults")
        .WithSummary("Re-rank search results using advanced ranking algorithms")
        .WithDescription("Applies weighted scoring, business rules, and diversity enhancement to improve result relevance");
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

    /// <summary>
    /// Request model for orchestrated search.
    /// </summary>
    public record OrchestratedSearchRequest
    {
        /// <summary>
        /// Gets or sets the composed query with constraints.
        /// </summary>
        public ComposedQuery ComposedQuery { get; init; } = null!;

        /// <summary>
        /// Gets or sets the maximum number of results (1-100).
        /// </summary>
        public int MaxResults { get; init; } = 10;
    }

    /// <summary>
    /// Response model for orchestrated search.
    /// </summary>
    public record OrchestratedSearchResponse
    {
        /// <summary>
        /// Gets or sets the list of vehicle search results.
        /// </summary>
        public List<VehicleSearchResult> Results { get; init; } = new();

        /// <summary>
        /// Gets or sets the total count of results.
        /// </summary>
        public int TotalCount { get; init; }

        /// <summary>
        /// Gets or sets the search strategy used.
        /// </summary>
        public SearchStrategyResponse Strategy { get; init; } = null!;

        /// <summary>
        /// Gets or sets the search duration.
        /// </summary>
        public string SearchDuration { get; init; } = string.Empty;
    }

    /// <summary>
    /// Vehicle search result model.
    /// </summary>
    public record VehicleSearchResult
    {
        /// <summary>
        /// Gets or sets the vehicle details.
        /// </summary>
        public VehicleResponse Vehicle { get; init; } = null!;

        /// <summary>
        /// Gets or sets the relevance score.
        /// </summary>
        public double RelevanceScore { get; init; }

        /// <summary>
        /// Gets or sets the score breakdown.
        /// </summary>
        public ScoreBreakdownResponse? ScoreBreakdown { get; init; }
    }

    /// <summary>
    /// Search strategy response model.
    /// </summary>
    public record SearchStrategyResponse
    {
        /// <summary>
        /// Gets or sets the strategy type.
        /// </summary>
        public string Type { get; init; } = string.Empty;

        /// <summary>
        /// Gets or sets the list of approaches.
        /// </summary>
        public List<string> Approaches { get; init; } = new();

        /// <summary>
        /// Gets or sets the weights for each approach.
        /// </summary>
        public Dictionary<string, double> Weights { get; init; } = new();
    }

    /// <summary>
    /// Score breakdown response model.
    /// </summary>
    public record ScoreBreakdownResponse
    {
        /// <summary>
        /// Gets or sets the exact match score.
        /// </summary>
        public double ExactMatchScore { get; init; }

        /// <summary>
        /// Gets or sets the semantic score.
        /// </summary>
        public double SemanticScore { get; init; }

        /// <summary>
        /// Gets or sets the keyword score.
        /// </summary>
        public double KeywordScore { get; init; }

        /// <summary>
        /// Gets or sets the final score.
        /// </summary>
        public double FinalScore { get; init; }
    }

    /// <summary>
    /// Request model for re-ranking results.
    /// </summary>
    public record RerankRequest
    {
        /// <summary>
        /// Gets or sets the results to re-rank.
        /// </summary>
        public List<RerankVehicleResult> Results { get; init; } = new();

        /// <summary>
        /// Gets or sets the composed query for context.
        /// </summary>
        public ComposedQuery Query { get; init; } = null!;

        /// <summary>
        /// Gets or sets the optional re-ranking strategy.
        /// </summary>
        public RerankStrategyRequest? Strategy { get; init; }
    }

    /// <summary>
    /// Response model for re-ranking results.
    /// </summary>
    public record RerankResponse
    {
        /// <summary>
        /// Gets or sets the re-ranked results.
        /// </summary>
        public List<VehicleSearchResult> Results { get; init; } = new();
    }

    /// <summary>
    /// Vehicle result for re-ranking request.
    /// </summary>
    public record RerankVehicleResult
    {
        /// <summary>
        /// Gets or sets the vehicle ID.
        /// </summary>
        public string VehicleId { get; init; } = string.Empty;

        /// <summary>
        /// Gets or sets the vehicle details.
        /// </summary>
        public RerankVehicleData Vehicle { get; init; } = null!;

        /// <summary>
        /// Gets or sets the current relevance score.
        /// </summary>
        public double RelevanceScore { get; init; }

        /// <summary>
        /// Gets or sets the score breakdown.
        /// </summary>
        public ScoreBreakdownResponse? ScoreBreakdown { get; init; }
    }

    /// <summary>
    /// Vehicle data for re-ranking request.
    /// </summary>
    public record RerankVehicleData
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

        /// <summary>
        /// Gets or sets whether service history is present.
        /// </summary>
        public bool? ServiceHistoryPresent { get; init; }

        /// <summary>
        /// Gets or sets the number of services.
        /// </summary>
        public int? NumberOfServices { get; init; }

        /// <summary>
        /// Gets or sets the last service date.
        /// </summary>
        public DateTime? LastServiceDate { get; init; }

        /// <summary>
        /// Gets or sets the MOT expiry date.
        /// </summary>
        public DateTime? MotExpiryDate { get; init; }

        /// <summary>
        /// Gets or sets the declarations.
        /// </summary>
        public List<string>? Declarations { get; init; }
    }

    /// <summary>
    /// Re-ranking strategy request.
    /// </summary>
    public record RerankStrategyRequest
    {
        /// <summary>
        /// Gets or sets the approach (e.g., "WeightedScore", "BusinessRules", "Hybrid").
        /// </summary>
        public string Approach { get; init; } = string.Empty;

        /// <summary>
        /// Gets or sets the factor weights (e.g., {"SemanticRelevance": 0.4, "ExactMatchCount": 0.3}).
        /// </summary>
        public Dictionary<string, double>? FactorWeights { get; init; }

        /// <summary>
        /// Gets or sets whether to apply diversity enhancement.
        /// </summary>
        public bool? ApplyDiversity { get; init; }

        /// <summary>
        /// Gets or sets the maximum vehicles per make.
        /// </summary>
        public int? MaxPerMake { get; init; }

        /// <summary>
        /// Gets or sets the maximum vehicles per model.
        /// </summary>
        public int? MaxPerModel { get; init; }
    }
}
