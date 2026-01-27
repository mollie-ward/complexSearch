var builder = DistributedApplication.CreateBuilder(args);

// Add connection strings for Azure services
var azureSearch = builder.AddConnectionString("AzureAISearch");
var azureOpenAI = builder.AddConnectionString("AzureOpenAI");

// Add backend API
var api = builder.AddProject<Projects.VehicleSearch_Api>("api")
    .WithExternalHttpEndpoints()
    .WithReference(azureSearch)
    .WithReference(azureOpenAI);

// Add frontend - using npm app
var frontend = builder.AddNpmApp("frontend", "../frontend")
    .WithHttpEndpoint(port: 3000, env: "PORT")
    .WithExternalHttpEndpoints()
    .WithEnvironment("NEXT_PUBLIC_API_URL", api.GetEndpoint("http"));

builder.Build().Run();
