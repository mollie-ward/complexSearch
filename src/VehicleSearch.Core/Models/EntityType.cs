namespace VehicleSearch.Core.Models;

/// <summary>
/// Represents the type of entity extracted from a query.
/// </summary>
public enum EntityType
{
    /// <summary>
    /// Vehicle make (e.g., BMW, Audi).
    /// </summary>
    Make,

    /// <summary>
    /// Vehicle model (e.g., 3 Series, A4).
    /// </summary>
    Model,

    /// <summary>
    /// Vehicle derivative (e.g., 320d M Sport).
    /// </summary>
    Derivative,

    /// <summary>
    /// Single price value.
    /// </summary>
    Price,

    /// <summary>
    /// Price range (min-max).
    /// </summary>
    PriceRange,

    /// <summary>
    /// Mileage value.
    /// </summary>
    Mileage,

    /// <summary>
    /// Engine size (e.g., 2.0L).
    /// </summary>
    EngineSize,

    /// <summary>
    /// Fuel type (e.g., Petrol, Diesel, Electric).
    /// </summary>
    FuelType,

    /// <summary>
    /// Transmission type (e.g., Manual, Automatic).
    /// </summary>
    Transmission,

    /// <summary>
    /// Body type (e.g., Sedan, SUV, Hatchback).
    /// </summary>
    BodyType,

    /// <summary>
    /// Vehicle colour.
    /// </summary>
    Colour,

    /// <summary>
    /// Vehicle feature (e.g., leather, navigation).
    /// </summary>
    Feature,

    /// <summary>
    /// Location (e.g., Manchester, Leeds).
    /// </summary>
    Location,

    /// <summary>
    /// Year of manufacture.
    /// </summary>
    Year,

    /// <summary>
    /// Qualitative term (e.g., "reliable", "economical").
    /// </summary>
    QualitativeTerm
}
