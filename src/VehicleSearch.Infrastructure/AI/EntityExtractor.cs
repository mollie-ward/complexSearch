using Microsoft.Extensions.Logging;
using VehicleSearch.Core.Interfaces;
using VehicleSearch.Core.Models;

namespace VehicleSearch.Infrastructure.AI;

/// <summary>
/// Service for extracting entities from user queries.
/// </summary>
public class EntityExtractor : IEntityExtractor
{
    private readonly ILogger<EntityExtractor> _logger;
    private readonly IKnowledgeBaseService? _knowledgeBaseService;

    // Known vehicle makes for extraction
    private static readonly HashSet<string> KnownMakes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Audi", "BMW", "Mercedes", "Mercedes-Benz", "Ford", "Toyota", "Volkswagen", "VW",
        "Nissan", "Honda", "Mazda", "Hyundai", "Kia", "Peugeot", "Renault", "Citroen",
        "Vauxhall", "Volvo", "Jaguar", "Land Rover", "Range Rover", "Porsche", "Ferrari",
        "Lamborghini", "Bentley", "Rolls-Royce", "Aston Martin", "McLaren", "Lotus",
        "Mini", "Fiat", "Alfa Romeo", "Seat", "Skoda", "Lexus", "Infiniti", "Acura",
        "Subaru", "Mitsubishi", "Suzuki", "Dacia", "MG", "Jeep", "Chrysler", "Dodge",
        "Tesla", "Rivian", "Lucid", "Polestar"
    };

    // Make synonyms/fuzzy matches
    private static readonly Dictionary<string, string> MakeSynonyms = new(StringComparer.OrdinalIgnoreCase)
    {
        { "beamer", "BMW" },
        { "beemer", "BMW" },
        { "merc", "Mercedes-Benz" },
        { "mercs", "Mercedes-Benz" },
        { "vw", "Volkswagen" },
        { "auddi", "Audi" }, // common typo
        { "aston", "Aston Martin" }
    };

    // Fuel types
    private static readonly HashSet<string> FuelTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Petrol", "Diesel", "Electric", "Hybrid", "Plug-in Hybrid", "PHEV", "EV"
    };

    // Transmission types
    private static readonly HashSet<string> TransmissionTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Manual", "Automatic", "Semi-Automatic", "CVT", "DSG"
    };

    // Body types
    private static readonly HashSet<string> BodyTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Sedan", "Saloon", "Hatchback", "SUV", "Coupe", "Convertible", "Estate",
        "MPV", "Minivan", "Pickup", "Truck", "Van", "Crossover", "Cabriolet"
    };

    // Common features
    private static readonly HashSet<string> CommonFeatures = new(StringComparer.OrdinalIgnoreCase)
    {
        "Leather", "Leather Seats", "Navigation", "Nav", "Sat Nav", "GPS",
        "Parking Sensors", "Parking Camera", "Reverse Camera", "Heated Seats",
        "Sunroof", "Panoramic Roof", "Alloy Wheels", "Cruise Control",
        "Bluetooth", "Apple CarPlay", "Android Auto", "Climate Control",
        "Keyless Entry", "Start Stop", "Xenon Lights", "LED Lights"
    };

    // Feature synonyms
    private static readonly Dictionary<string, string> FeatureSynonyms = new(StringComparer.OrdinalIgnoreCase)
    {
        { "nav", "Navigation" },
        { "sat nav", "Navigation" },
        { "gps", "Navigation" },
        { "heated seats", "Heated Seats" },
        { "parking sensors", "Parking Sensors" }
    };

    // UK locations
    private static readonly HashSet<string> Locations = new(StringComparer.OrdinalIgnoreCase)
    {
        "London", "Manchester", "Birmingham", "Leeds", "Liverpool", "Sheffield",
        "Bristol", "Newcastle", "Nottingham", "Leicester", "Edinburgh", "Glasgow",
        "Cardiff", "Belfast", "North", "South", "Midlands", "East", "West"
    };

    // Qualitative terms
    private static readonly HashSet<string> QualitativeTerms = new(StringComparer.OrdinalIgnoreCase)
    {
        "Reliable", "Economical", "Family Car", "Sporty", "Luxury", "Practical",
        "Efficient", "Safe", "Comfortable", "Spacious", "Compact", "Fast", "Powerful"
    };

    public EntityExtractor(ILogger<EntityExtractor> logger, IKnowledgeBaseService? knowledgeBaseService = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _knowledgeBaseService = knowledgeBaseService;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ExtractedEntity>> ExtractAsync(string query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new ArgumentException("Query cannot be null or empty.", nameof(query));
        }

        var entities = new List<ExtractedEntity>();

        // Extract using pattern matching
        entities.AddRange(PatternMatcher.ExtractPrices(query));
        entities.AddRange(PatternMatcher.ExtractMileage(query));
        entities.AddRange(PatternMatcher.ExtractYears(query));

        // Extract using knowledge-based matching
        entities.AddRange(ExtractMakes(query));
        entities.AddRange(ExtractFuelTypes(query));
        entities.AddRange(ExtractTransmissions(query));
        entities.AddRange(ExtractBodyTypes(query));
        entities.AddRange(ExtractFeatures(query));
        entities.AddRange(ExtractLocations(query));
        entities.AddRange(ExtractQualitativeTerms(query));

        // Remove duplicates (keep highest confidence)
        entities = RemoveDuplicates(entities);

        _logger.LogDebug("Extracted {Count} entities from query: {Query}", entities.Count, query);

        await Task.CompletedTask; // For async interface consistency
        return entities;
    }

    private List<ExtractedEntity> ExtractMakes(string query)
    {
        var entities = new List<ExtractedEntity>();

        // Check for exact matches first
        foreach (var make in KnownMakes)
        {
            var index = query.IndexOf(make, StringComparison.OrdinalIgnoreCase);
            if (index >= 0)
            {
                entities.Add(new ExtractedEntity
                {
                    Type = EntityType.Make,
                    Value = make,
                    Confidence = 1.0,
                    StartPosition = index,
                    EndPosition = index + make.Length
                });
            }
        }

        // Check for synonyms
        foreach (var (synonym, actualMake) in MakeSynonyms)
        {
            var index = query.IndexOf(synonym, StringComparison.OrdinalIgnoreCase);
            if (index >= 0 && !entities.Any(e => e.Type == EntityType.Make && e.Value.Equals(actualMake, StringComparison.OrdinalIgnoreCase)))
            {
                entities.Add(new ExtractedEntity
                {
                    Type = EntityType.Make,
                    Value = actualMake,
                    Confidence = 0.9,
                    StartPosition = index,
                    EndPosition = index + synonym.Length
                });
            }
        }

        // Fuzzy matching for typos
        var words = query.Split(new[] { ' ', ',', '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var word in words)
        {
            if (word.Length < 3) continue;

            foreach (var make in KnownMakes)
            {
                if (entities.Any(e => e.Type == EntityType.Make && e.Value.Equals(make, StringComparison.OrdinalIgnoreCase)))
                    continue;

                var distance = PatternMatcher.LevenshteinDistance(word.ToLower(), make.ToLower());
                if (distance <= 2 && distance < make.Length / 2)
                {
                    var index = query.IndexOf(word, StringComparison.OrdinalIgnoreCase);
                    if (index >= 0)
                    {
                        entities.Add(new ExtractedEntity
                        {
                            Type = EntityType.Make,
                            Value = make,
                            Confidence = 0.8 - (distance * 0.1),
                            StartPosition = index,
                            EndPosition = index + word.Length
                        });
                    }
                }
            }
        }

        return entities;
    }

    private List<ExtractedEntity> ExtractFuelTypes(string query)
    {
        var entities = new List<ExtractedEntity>();

        foreach (var fuelType in FuelTypes)
        {
            var index = query.IndexOf(fuelType, StringComparison.OrdinalIgnoreCase);
            if (index >= 0)
            {
                entities.Add(new ExtractedEntity
                {
                    Type = EntityType.FuelType,
                    Value = fuelType,
                    Confidence = 0.95,
                    StartPosition = index,
                    EndPosition = index + fuelType.Length
                });
            }
        }

        return entities;
    }

    private List<ExtractedEntity> ExtractTransmissions(string query)
    {
        var entities = new List<ExtractedEntity>();

        foreach (var transmission in TransmissionTypes)
        {
            var index = query.IndexOf(transmission, StringComparison.OrdinalIgnoreCase);
            if (index >= 0)
            {
                entities.Add(new ExtractedEntity
                {
                    Type = EntityType.Transmission,
                    Value = transmission,
                    Confidence = 0.95,
                    StartPosition = index,
                    EndPosition = index + transmission.Length
                });
            }
        }

        return entities;
    }

    private List<ExtractedEntity> ExtractBodyTypes(string query)
    {
        var entities = new List<ExtractedEntity>();

        foreach (var bodyType in BodyTypes)
        {
            var index = query.IndexOf(bodyType, StringComparison.OrdinalIgnoreCase);
            if (index >= 0)
            {
                entities.Add(new ExtractedEntity
                {
                    Type = EntityType.BodyType,
                    Value = bodyType,
                    Confidence = 0.9,
                    StartPosition = index,
                    EndPosition = index + bodyType.Length
                });
            }
        }

        return entities;
    }

    private List<ExtractedEntity> ExtractFeatures(string query)
    {
        var entities = new List<ExtractedEntity>();

        foreach (var feature in CommonFeatures)
        {
            var index = query.IndexOf(feature, StringComparison.OrdinalIgnoreCase);
            if (index >= 0)
            {
                entities.Add(new ExtractedEntity
                {
                    Type = EntityType.Feature,
                    Value = feature,
                    Confidence = 0.85,
                    StartPosition = index,
                    EndPosition = index + feature.Length
                });
            }
        }

        // Check synonyms
        foreach (var (synonym, actualFeature) in FeatureSynonyms)
        {
            var index = query.IndexOf(synonym, StringComparison.OrdinalIgnoreCase);
            if (index >= 0 && !entities.Any(e => e.Type == EntityType.Feature && e.Value.Equals(actualFeature, StringComparison.OrdinalIgnoreCase)))
            {
                entities.Add(new ExtractedEntity
                {
                    Type = EntityType.Feature,
                    Value = actualFeature,
                    Confidence = 0.8,
                    StartPosition = index,
                    EndPosition = index + synonym.Length
                });
            }
        }

        return entities;
    }

    private List<ExtractedEntity> ExtractLocations(string query)
    {
        var entities = new List<ExtractedEntity>();

        foreach (var location in Locations)
        {
            var index = query.IndexOf(location, StringComparison.OrdinalIgnoreCase);
            if (index >= 0)
            {
                entities.Add(new ExtractedEntity
                {
                    Type = EntityType.Location,
                    Value = location,
                    Confidence = 0.9,
                    StartPosition = index,
                    EndPosition = index + location.Length
                });
            }
        }

        return entities;
    }

    private List<ExtractedEntity> ExtractQualitativeTerms(string query)
    {
        var entities = new List<ExtractedEntity>();

        foreach (var term in QualitativeTerms)
        {
            var index = query.IndexOf(term, StringComparison.OrdinalIgnoreCase);
            if (index >= 0)
            {
                entities.Add(new ExtractedEntity
                {
                    Type = EntityType.QualitativeTerm,
                    Value = term,
                    Confidence = 0.75,
                    StartPosition = index,
                    EndPosition = index + term.Length
                });
            }
        }

        return entities;
    }

    private List<ExtractedEntity> RemoveDuplicates(List<ExtractedEntity> entities)
    {
        // Group by type and value, keep highest confidence
        return entities
            .GroupBy(e => new { e.Type, Value = e.Value.ToLowerInvariant() })
            .Select(g => g.OrderByDescending(e => e.Confidence).First())
            .ToList();
    }
}
