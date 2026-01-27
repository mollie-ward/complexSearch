using Microsoft.AspNetCore.Mvc;
using VehicleSearch.Core.Interfaces;

namespace VehicleSearch.Api.Endpoints;

/// <summary>
/// Endpoints for vehicle retrieval operations.
/// </summary>
public static class VehiclesEndpoints
{
    /// <summary>
    /// Maps vehicle endpoints.
    /// </summary>
    /// <param name="app">The web application.</param>
    public static void MapVehiclesEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/vehicles")
            .WithTags("Vehicles")
            .WithOpenApi();

        // GET /api/v1/vehicles/{id}
        group.MapGet("/{id}", async (
            string id,
            [FromServices] IVehicleRetrievalService retrievalService,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var vehicle = await retrievalService.GetVehicleByIdAsync(id, cancellationToken);

                if (vehicle == null)
                {
                    return Results.NotFound(new { error = $"Vehicle with ID '{id}' not found" });
                }

                return Results.Ok(new
                {
                    id = vehicle.Id,
                    make = vehicle.Make,
                    model = vehicle.Model,
                    derivative = vehicle.Derivative,
                    price = vehicle.Price,
                    mileage = vehicle.Mileage,
                    bodyType = vehicle.BodyType,
                    engineSize = vehicle.EngineSize,
                    fuelType = vehicle.FuelType,
                    transmissionType = vehicle.TransmissionType,
                    colour = vehicle.Colour,
                    numberOfDoors = vehicle.NumberOfDoors,
                    registrationDate = vehicle.RegistrationDate,
                    saleLocation = vehicle.SaleLocation,
                    channel = vehicle.Channel,
                    features = vehicle.Features,
                    description = vehicle.Description
                });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    title: "Failed to retrieve vehicle",
                    detail: ex.Message,
                    statusCode: 500);
            }
        })
        .WithName("GetVehicleById")
        .WithSummary("Get vehicle by ID")
        .WithDescription("Retrieves a vehicle by its registration number from the search index");

        // GET /api/v1/vehicles
        group.MapGet("/", async (
            [FromServices] IVehicleRetrievalService retrievalService) =>
        {
            try
            {
                var totalCount = await retrievalService.GetTotalCountAsync();

                return Results.Ok(new
                {
                    totalVehicles = totalCount,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    title: "Failed to get vehicle count",
                    detail: ex.Message,
                    statusCode: 500);
            }
        })
        .WithName("GetVehicleCount")
        .WithSummary("Get total vehicle count")
        .WithDescription("Returns the total number of vehicles in the search index");
    }
}
