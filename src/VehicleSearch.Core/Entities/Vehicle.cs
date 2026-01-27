namespace VehicleSearch.Core.Entities;

/// <summary>
/// Represents a vehicle in the search system.
/// </summary>
public class Vehicle
{
    /// <summary>
    /// Gets or sets the unique identifier for the vehicle.
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
    /// Gets or sets the vehicle year.
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// Gets or sets the vehicle price.
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Gets or sets the vehicle mileage.
    /// </summary>
    public int Mileage { get; set; }

    /// <summary>
    /// Gets or sets the vehicle description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets additional metadata as key-value pairs.
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();
}
