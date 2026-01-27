namespace VehicleSearch.Core.Entities;

/// <summary>
/// Represents a vehicle in the search system.
/// </summary>
public class Vehicle
{
    /// <summary>
    /// Gets or sets the unique identifier for the vehicle (Registration Number).
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
    /// Gets or sets the body type.
    /// </summary>
    public string BodyType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the vehicle price.
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Gets or sets the vehicle mileage.
    /// </summary>
    public int Mileage { get; set; }

    /// <summary>
    /// Gets or sets the engine size in liters.
    /// </summary>
    public decimal EngineSize { get; set; }

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
    public DateTime? RegistrationDate { get; set; }

    /// <summary>
    /// Gets or sets the sale location.
    /// </summary>
    public string SaleLocation { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the channel.
    /// </summary>
    public string Channel { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the sale type.
    /// </summary>
    public string SaleType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of features.
    /// </summary>
    public List<string> Features { get; set; } = new();

    /// <summary>
    /// Gets or sets the grade.
    /// </summary>
    public string Grade { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether service history is present.
    /// </summary>
    public bool ServiceHistoryPresent { get; set; }

    /// <summary>
    /// Gets or sets the number of services.
    /// </summary>
    public int? NumberOfServices { get; set; }

    /// <summary>
    /// Gets or sets the last service date.
    /// </summary>
    public DateTime? LastServiceDate { get; set; }

    /// <summary>
    /// Gets or sets the MOT expiry date.
    /// </summary>
    public DateTime? MotExpiryDate { get; set; }

    /// <summary>
    /// Gets or sets the VAT type.
    /// </summary>
    public string VatType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets additional information.
    /// </summary>
    public string AdditionalInfo { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of declarations.
    /// </summary>
    public List<string> Declarations { get; set; } = new();

    /// <summary>
    /// Gets or sets the CAP retail price.
    /// </summary>
    public decimal? CapRetailPrice { get; set; }

    /// <summary>
    /// Gets or sets the CAP clean price.
    /// </summary>
    public decimal? CapCleanPrice { get; set; }

    /// <summary>
    /// Gets or sets the vehicle description (computed field for semantic search).
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date when the vehicle was processed.
    /// </summary>
    public DateTime ProcessedDate { get; set; }
}
