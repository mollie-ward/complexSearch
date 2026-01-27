var builder = DistributedApplication.CreateBuilder(args);

// Add connection strings for Azure services
var azureSearch = builder.AddConnectionString("AzureAISearch");
var azureOpenAI = builder.AddConnectionString("AzureOpenAI");

// Add backend API using path-based reference
var api = builder.AddProject("vehiclesearch-api", "../VehicleSearch.Api/VehicleSearch.Api.csproj")
    .WithExternalHttpEndpoints()
    .WithReference(azureSearch)
    .WithReference(azureOpenAI);

// Add frontend - using npm app
var frontend = builder.AddNpmApp("vehiclesearch-frontend", "../../frontend")
    .WithHttpEndpoint(port: 3000, env: "PORT")
    .WithExternalHttpEndpoints()
    .WithEnvironment("NEXT_PUBLIC_API_URL", api.GetEndpoint("http"));

builder.Build().Run();
