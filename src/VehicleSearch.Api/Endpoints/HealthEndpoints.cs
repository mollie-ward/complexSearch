namespace VehicleSearch.Api.Endpoints;

/// <summary>
/// Health check endpoint for API status monitoring.
/// </summary>
public static class HealthEndpoints
{
    /// <summary>
    /// Maps health check endpoints.
    /// </summary>
    /// <param name="app">The web application.</param>
    public static void MapHealthEndpoints(this WebApplication app)
    {
        app.MapGet("/api/health", () =>
        {
            var response = new
            {
                status = "Healthy",
                timestamp = DateTime.UtcNow.ToString("O"),
                version = "1.0.0",
                dependencies = new
                {
                    database = "Not configured",
                    aiService = "Not configured",
                    searchService = "Not configured"
                }
            };

            return Results.Ok(response);
        })
        .WithName("GetHealth")
        .WithTags("Health")
        .WithOpenApi();
    }
}
