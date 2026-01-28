using VehicleSearch.Core.Models;

namespace VehicleSearch.Infrastructure.AI;

/// <summary>
/// Static configuration for concept-to-attribute mappings.
/// Defines how qualitative terms map to measurable vehicle attributes.
/// </summary>
public static class ConceptMappings
{
    /// <summary>
    /// Gets all predefined concept mappings.
    /// </summary>
    public static readonly Dictionary<string, ConceptualMapping> Mappings = new(StringComparer.OrdinalIgnoreCase)
    {
        ["reliable"] = new ConceptualMapping
        {
            Concept = "reliable",
            AttributeWeights = new List<AttributeWeight>
            {
                new AttributeWeight
                {
                    Attribute = "mileage",
                    Weight = 0.3,
                    TargetValue = 60000,
                    ComparisonType = "less"
                },
                new AttributeWeight
                {
                    Attribute = "serviceHistoryPresent",
                    Weight = 0.3,
                    TargetValue = true,
                    ComparisonType = "equals"
                },
                new AttributeWeight
                {
                    Attribute = "numberOfPreviousOwners",
                    Weight = 0.2,
                    TargetValue = 2,
                    ComparisonType = "lessOrEqual"
                },
                new AttributeWeight
                {
                    Attribute = "motExpiryDate",
                    Weight = 0.2,
                    TargetValue = 90, // days from now
                    ComparisonType = "greaterOrEqual"
                }
            },
            PositiveIndicators = new List<string>
            {
                "full service history",
                "one owner",
                "warranty",
                "full service",
                "low mileage"
            },
            NegativeIndicators = new List<string>
            {
                "accident damage",
                "high mileage",
                "no service history"
            }
        },

        ["economical"] = new ConceptualMapping
        {
            Concept = "economical",
            AttributeWeights = new List<AttributeWeight>
            {
                new AttributeWeight
                {
                    Attribute = "fuelType",
                    Weight = 0.4,
                    TargetValue = new[] { "Electric", "Hybrid", "Petrol" },
                    ComparisonType = "in"
                },
                new AttributeWeight
                {
                    Attribute = "engineSize",
                    Weight = 0.3,
                    TargetValue = 2.0,
                    ComparisonType = "less"
                },
                new AttributeWeight
                {
                    Attribute = "price",
                    Weight = 0.3,
                    TargetValue = 20000.0,
                    ComparisonType = "less"
                }
            },
            PositiveIndicators = new List<string>
            {
                "fuel efficient",
                "hybrid",
                "low tax",
                "economical",
                "efficient"
            },
            NegativeIndicators = new List<string>
            {
                "v8",
                "v6",
                "sports",
                "performance"
            }
        },

        ["family car"] = new ConceptualMapping
        {
            Concept = "family car",
            AttributeWeights = new List<AttributeWeight>
            {
                new AttributeWeight
                {
                    Attribute = "numberOfDoors",
                    Weight = 0.3,
                    TargetValue = 5,
                    ComparisonType = "greaterOrEqual"
                },
                new AttributeWeight
                {
                    Attribute = "numberOfSeats",
                    Weight = 0.3,
                    TargetValue = 5,
                    ComparisonType = "greaterOrEqual"
                },
                new AttributeWeight
                {
                    Attribute = "bodyType",
                    Weight = 0.4,
                    TargetValue = new[] { "SUV", "MPV", "Estate", "Hatchback" },
                    ComparisonType = "in"
                }
            },
            PositiveIndicators = new List<string>
            {
                "spacious",
                "boot space",
                "practical",
                "family",
                "seating"
            },
            NegativeIndicators = new List<string>
            {
                "2-door",
                "coupe",
                "sports car",
                "two door"
            }
        },

        ["sporty"] = new ConceptualMapping
        {
            Concept = "sporty",
            AttributeWeights = new List<AttributeWeight>
            {
                new AttributeWeight
                {
                    Attribute = "engineSize",
                    Weight = 0.4,
                    TargetValue = 2.0,
                    ComparisonType = "greater"
                },
                new AttributeWeight
                {
                    Attribute = "bodyType",
                    Weight = 0.3,
                    TargetValue = new[] { "Coupe", "Convertible", "Hatchback" },
                    ComparisonType = "in"
                },
                new AttributeWeight
                {
                    Attribute = "transmissionType",
                    Weight = 0.3,
                    TargetValue = "Manual",
                    ComparisonType = "equals"
                }
            },
            PositiveIndicators = new List<string>
            {
                "turbo",
                "performance",
                "sport",
                "alloy wheels",
                "fast"
            },
            NegativeIndicators = new List<string>
            {
                "economical",
                "mpv",
                "family"
            }
        },

        ["luxury"] = new ConceptualMapping
        {
            Concept = "luxury",
            AttributeWeights = new List<AttributeWeight>
            {
                new AttributeWeight
                {
                    Attribute = "price",
                    Weight = 0.3,
                    TargetValue = 30000.0,
                    ComparisonType = "greater"
                },
                new AttributeWeight
                {
                    Attribute = "make",
                    Weight = 0.4,
                    TargetValue = new[] { "BMW", "Mercedes-Benz", "Audi", "Jaguar", "Lexus", "Mercedes" },
                    ComparisonType = "in"
                },
                new AttributeWeight
                {
                    Attribute = "features",
                    Weight = 0.3,
                    TargetValue = new[] { "leather", "navigation", "heated seats", "sunroof" },
                    ComparisonType = "containsAny"
                }
            },
            PositiveIndicators = new List<string>
            {
                "leather",
                "navigation",
                "heated seats",
                "sunroof",
                "premium"
            },
            NegativeIndicators = new List<string>
            {
                "basic",
                "budget"
            }
        },

        ["practical"] = new ConceptualMapping
        {
            Concept = "practical",
            AttributeWeights = new List<AttributeWeight>
            {
                new AttributeWeight
                {
                    Attribute = "bodyType",
                    Weight = 0.4,
                    TargetValue = new[] { "Estate", "MPV", "SUV", "Hatchback" },
                    ComparisonType = "in"
                },
                new AttributeWeight
                {
                    Attribute = "numberOfDoors",
                    Weight = 0.3,
                    TargetValue = 4,
                    ComparisonType = "greaterOrEqual"
                },
                new AttributeWeight
                {
                    Attribute = "fuelType",
                    Weight = 0.3,
                    TargetValue = new[] { "Diesel", "Hybrid", "Petrol", "Electric" },
                    ComparisonType = "in"
                }
            },
            PositiveIndicators = new List<string>
            {
                "boot space",
                "storage",
                "versatile",
                "practical",
                "spacious"
            },
            NegativeIndicators = new List<string>
            {
                "coupe",
                "sports car",
                "2-door"
            }
        }
    };
}
