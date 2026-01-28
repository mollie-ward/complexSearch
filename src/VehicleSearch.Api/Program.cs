using Serilog;
using VehicleSearch.Api.Endpoints;
using VehicleSearch.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults (OpenTelemetry, health checks, service discovery)
builder.AddServiceDefaults();

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Vehicle Search API",
        Version = "v1",
        Description = "API for searching and exploring vehicle inventory using AI-powered semantic search"
    });
});

// Configure CORS
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? new[] { "http://localhost:3000" };
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(corsOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Register application services
builder.Services.Configure<VehicleSearch.Core.Models.AzureSearchConfig>(
    builder.Configuration.GetSection("AzureAISearch"));
builder.Services.Configure<VehicleSearch.Core.Models.AzureOpenAIConfig>(
    builder.Configuration.GetSection("AzureOpenAI"));

builder.Services.AddSingleton<VehicleSearch.Infrastructure.Data.CsvDataLoader>();
builder.Services.AddSingleton<VehicleSearch.Infrastructure.Data.DataNormalizer>();
builder.Services.AddSingleton<VehicleSearch.Infrastructure.Data.DataValidator>();
builder.Services.AddScoped<VehicleSearch.Core.Interfaces.IDataIngestionService, VehicleSearch.Infrastructure.Data.DataIngestionService>();

// Register Azure Search services
builder.Services.AddSingleton<VehicleSearch.Infrastructure.Search.AzureSearchClient>();
builder.Services.AddScoped<VehicleSearch.Core.Interfaces.ISearchIndexService, VehicleSearch.Infrastructure.Search.SearchIndexService>();

// Register Azure OpenAI and Embedding services
builder.Services.AddSingleton<VehicleSearch.Infrastructure.AI.EmbeddingService>();
builder.Services.AddMemoryCache(options =>
{
    options.SizeLimit = 1000; // Maximum 1000 cached embeddings
});
builder.Services.AddScoped<VehicleSearch.Core.Interfaces.IEmbeddingService>(sp =>
{
    var innerService = sp.GetRequiredService<VehicleSearch.Infrastructure.AI.EmbeddingService>();
    var cache = sp.GetRequiredService<Microsoft.Extensions.Caching.Memory.IMemoryCache>();
    var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<VehicleSearch.Infrastructure.AI.CachedEmbeddingService>>();
    return new VehicleSearch.Infrastructure.AI.CachedEmbeddingService(innerService, cache, logger);
});
builder.Services.AddScoped<VehicleSearch.Core.Interfaces.IVehicleIndexingService, VehicleSearch.Infrastructure.Search.VehicleIndexingService>();
builder.Services.AddScoped<VehicleSearch.Core.Interfaces.IVehicleRetrievalService, VehicleSearch.Infrastructure.Search.VehicleRetrievalService>();

// Register Semantic Search services
builder.Services.AddScoped<VehicleSearch.Core.Interfaces.ISemanticSearchService, VehicleSearch.Infrastructure.Search.SemanticSearchService>();

// Register Search Orchestration services
builder.Services.AddScoped<VehicleSearch.Infrastructure.Search.ExactSearchExecutor>();
builder.Services.AddScoped<VehicleSearch.Infrastructure.Search.SemanticSearchExecutor>();
builder.Services.AddScoped<VehicleSearch.Infrastructure.Search.HybridSearchExecutor>();
builder.Services.AddScoped<VehicleSearch.Core.Interfaces.ISearchOrchestratorService, VehicleSearch.Infrastructure.Search.SearchOrchestratorService>();

// Register Result Ranking services
builder.Services.AddScoped<VehicleSearch.Core.Interfaces.IResultRankingService, VehicleSearch.Infrastructure.Search.ResultRankingService>();
builder.Services.AddScoped<VehicleSearch.Infrastructure.Search.RRFMerger>();
builder.Services.AddScoped<VehicleSearch.Infrastructure.Search.DiversityEnhancer>();

// Register Query Understanding services
builder.Services.AddScoped<VehicleSearch.Core.Interfaces.IIntentClassifier, VehicleSearch.Infrastructure.AI.IntentClassifier>();
builder.Services.AddScoped<VehicleSearch.Core.Interfaces.IEntityExtractor, VehicleSearch.Infrastructure.AI.EntityExtractor>();
builder.Services.AddScoped<VehicleSearch.Core.Interfaces.IQueryUnderstandingService, VehicleSearch.Infrastructure.AI.QueryUnderstandingService>();

// Register Attribute Mapping services
builder.Services.Configure<VehicleSearch.Infrastructure.AI.QualitativeTermsConfig>(
    builder.Configuration.GetSection("QualitativeTerms"));
builder.Services.AddSingleton<VehicleSearch.Infrastructure.AI.OperatorInferenceService>();
builder.Services.AddScoped<VehicleSearch.Infrastructure.AI.ConstraintParser>();
builder.Services.AddScoped<VehicleSearch.Core.Interfaces.IAttributeMapperService, VehicleSearch.Infrastructure.AI.AttributeMapperService>();

// Register Query Composition services
builder.Services.AddScoped<VehicleSearch.Infrastructure.AI.ConflictResolver>();
builder.Services.AddScoped<VehicleSearch.Infrastructure.AI.ODataTranslator>();
builder.Services.AddScoped<VehicleSearch.Core.Interfaces.IQueryComposerService, VehicleSearch.Infrastructure.AI.QueryComposerService>();

// Register Conceptual Mapping services
builder.Services.AddSingleton<VehicleSearch.Infrastructure.AI.SimilarityScorer>();
builder.Services.AddScoped<VehicleSearch.Core.Interfaces.IConceptualMapperService, VehicleSearch.Infrastructure.AI.ConceptualMapperService>();

// Register Conversation Session services
builder.Services.AddSingleton<VehicleSearch.Infrastructure.Session.InMemoryConversationSessionService>();
builder.Services.AddSingleton<VehicleSearch.Core.Interfaces.IConversationSessionService>(sp =>
    sp.GetRequiredService<VehicleSearch.Infrastructure.Session.InMemoryConversationSessionService>());
builder.Services.AddHostedService<VehicleSearch.Infrastructure.Session.SessionCleanupService>();

// Register Reference Resolution services
builder.Services.AddScoped<VehicleSearch.Infrastructure.AI.ComparativeResolver>();
builder.Services.AddScoped<VehicleSearch.Infrastructure.AI.QueryRefiner>();
builder.Services.AddScoped<VehicleSearch.Core.Interfaces.IReferenceResolverService, VehicleSearch.Infrastructure.AI.ReferenceResolverService>();

// Register Safety Guardrail services
builder.Services.AddScoped<VehicleSearch.Core.Interfaces.ISafetyGuardrailService, VehicleSearch.Infrastructure.Safety.SafetyGuardrailService>();

var app = builder.Build();

// Configure the HTTP request pipeline

// 1. Exception handling middleware (first to catch all exceptions)
app.UseMiddleware<ExceptionHandlingMiddleware>();

// 2. Safety guardrail middleware (validate inputs early)
app.UseMiddleware<SafetyGuardrailMiddleware>();

// 3. CORS middleware
app.UseCors("AllowFrontend");

// 4. Request logging (Serilog request logging)
app.UseSerilogRequestLogging();

// 5. OpenAPI/Swagger (in development)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Vehicle Search API v1");
    });
}

// 6. HTTPS redirection
app.UseHttpsRedirection();

// 7. Authentication placeholder
// TODO: Implement authentication in v2

// 8. Routing
app.UseRouting();

// Map default endpoints (health checks, etc.)
app.MapDefaultEndpoints();

// Map endpoints
app.MapHealthEndpoints();
app.MapKnowledgeBaseEndpoints();
app.MapVehiclesEndpoints();
app.MapQueryEndpoints();
app.MapSearchEndpoints();
app.MapConversationEndpoints();

// Run the application
try
{
    Log.Information("Starting Vehicle Search API");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Make Program class accessible for integration tests
public partial class Program { }

