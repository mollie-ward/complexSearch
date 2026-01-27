namespace VehicleSearch.Core.Models;

/// <summary>
/// Represents a vehicle document in the Azure AI Search index.
/// </summary>
public class VehicleSearchDocument
{
    /// <summary>
    /// Gets or sets the unique identifier (Registration Number).
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the vehicle make.
    /// </summary>
    public string Make { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the vehicle model.
    /// </summary>
    public string Model { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the vehicle derivative.
    /// </summary>
    public string Derivative { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the vehicle price.
    /// </summary>
    public double Price { get; set; }

    /// <summary>
    /// Gets or sets the vehicle mileage.
    /// </summary>
    public int Mileage { get; set; }

    /// <summary>
    /// Gets or sets the body type.
    /// </summary>
    public string BodyType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the engine size in liters.
    /// </summary>
    public double? EngineSize { get; set; }

    /// <summary>
    /// Gets or sets the fuel type.
    /// </summary>
    public string FuelType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the transmission type.
    /// </summary>
    public string TransmissionType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the colour.
    /// </summary>
    public string Colour { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of doors.
    /// </summary>
    public int? NumberOfDoors { get; set; }

    /// <summary>
    /// Gets or sets the registration date.
    /// </summary>
    public DateTimeOffset RegistrationDate { get; set; }

    /// <summary>
    /// Gets or sets the sale location.
    /// </summary>
    public string SaleLocation { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the channel.
    /// </summary>
    public string Channel { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of features.
    /// </summary>
    public string[] Features { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the vehicle description for full-text search.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the embedding vector for semantic search.
    /// </summary>
    public float[] DescriptionVector { get; set; } = Array.Empty<float>();

    /// <summary>
    /// Gets or sets the date when the vehicle was processed.
    /// </summary>
    public DateTimeOffset ProcessedDate { get; set; }
}
