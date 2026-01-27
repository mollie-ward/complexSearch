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
// TODO: Add service registrations in future tasks

var app = builder.Build();

// Configure the HTTP request pipeline

// 1. Exception handling middleware (first to catch all exceptions)
app.UseMiddleware<ExceptionHandlingMiddleware>();

// 2. CORS middleware
app.UseCors("AllowFrontend");

// 3. Request logging (Serilog request logging)
app.UseSerilogRequestLogging();

// 4. OpenAPI/Swagger (in development)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Vehicle Search API v1");
    });
}

// 5. HTTPS redirection
app.UseHttpsRedirection();

// 6. Rate limiting placeholder
// TODO: Implement rate limiting in future tasks

// 7. Authentication placeholder
// TODO: Implement authentication in v2

// 8. Routing
app.UseRouting();

// Map default endpoints (health checks, etc.)
app.MapDefaultEndpoints();

// Map endpoints
app.MapHealthEndpoints();

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

