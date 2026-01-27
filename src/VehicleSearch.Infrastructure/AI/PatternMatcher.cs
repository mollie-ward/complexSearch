using System.Text.RegularExpressions;
using VehicleSearch.Core.Models;

namespace VehicleSearch.Infrastructure.AI;

/// <summary>
/// Utility class for pattern-based entity extraction using regex.
/// </summary>
public class PatternMatcher
{
    // Price patterns - matches various price formats
    private static readonly Regex PricePattern = new(
        @"£?\s*(\d{1,3}(?:,\d{3})*|\d+)k?\s*(?:pounds?)?",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex PriceRangePattern = new(
        @"(?:between\s+)?£?\s*(\d{1,3}(?:,\d{3})*|\d+)k?\s*(?:-|to|and)\s*£?\s*(\d{1,3}(?:,\d{3})*|\d+)k?",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex PriceQualifierPattern = new(
        @"(?:under|less\s+than|up\s+to|below|max)\s+£?\s*(\d{1,3}(?:,\d{3})*|\d+)k?",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Mileage patterns
    private static readonly Regex MileagePattern = new(
        @"(\d{1,3}(?:,\d{3})*|\d+)k?\s*(?:miles?)?",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex MileageQualifierPattern = new(
        @"(?:under|less\s+than|up\s+to|below|max)\s+(\d{1,3}(?:,\d{3})*|\d+)k?\s*(?:miles?)?",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex LowMileagePattern = new(
        @"\blow\s+mileage\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Year patterns
    private static readonly Regex YearPattern = new(
        @"\b(19\d{2}|20[0-2]\d)\b",
        RegexOptions.Compiled);

    /// <summary>
    /// Extracts price entities from the query.
    /// </summary>
    public static List<ExtractedEntity> ExtractPrices(string query)
    {
        var entities = new List<ExtractedEntity>();

        // Check for price ranges first
        var rangeMatches = PriceRangePattern.Matches(query);
        foreach (Match match in rangeMatches)
        {
            var minPrice = ParsePrice(match.Groups[1].Value);
            var maxPrice = ParsePrice(match.Groups[2].Value);
            
            entities.Add(new ExtractedEntity
            {
                Type = EntityType.PriceRange,
                Value = $"{minPrice}-{maxPrice}",
                Confidence = 1.0,
                StartPosition = match.Index,
                EndPosition = match.Index + match.Length
            });
        }

        // Then check for price qualifiers (under, less than, etc.)
        var qualifierMatches = PriceQualifierPattern.Matches(query);
        foreach (Match match in qualifierMatches)
        {
            if (!IsOverlapping(match, rangeMatches))
            {
                var price = ParsePrice(match.Groups[1].Value);
                entities.Add(new ExtractedEntity
                {
                    Type = EntityType.Price,
                    Value = price.ToString(),
                    Confidence = 0.95,
                    StartPosition = match.Index,
                    EndPosition = match.Index + match.Length
                });
            }
        }

        // Finally check for simple price patterns
        var priceMatches = PricePattern.Matches(query);
        foreach (Match match in priceMatches)
        {
            if (!IsOverlapping(match, rangeMatches) && !IsOverlapping(match, qualifierMatches))
            {
                var price = ParsePrice(match.Groups[1].Value);
                // Only consider values that look like prices (>= 1000)
                if (price >= 1000)
                {
                    entities.Add(new ExtractedEntity
                    {
                        Type = EntityType.Price,
                        Value = price.ToString(),
                        Confidence = 0.9,
                        StartPosition = match.Index,
                        EndPosition = match.Index + match.Length
                    });
                }
            }
        }

        return entities;
    }

    /// <summary>
    /// Extracts mileage entities from the query.
    /// </summary>
    public static List<ExtractedEntity> ExtractMileage(string query)
    {
        var entities = new List<ExtractedEntity>();

        // Check for "low mileage" qualifier
        var lowMileageMatch = LowMileagePattern.Match(query);
        if (lowMileageMatch.Success)
        {
            entities.Add(new ExtractedEntity
            {
                Type = EntityType.Mileage,
                Value = "30000", // Default for "low mileage"
                Confidence = 0.7,
                StartPosition = lowMileageMatch.Index,
                EndPosition = lowMileageMatch.Index + lowMileageMatch.Length
            });
            return entities; // Return early for low mileage
        }

        // Check for mileage with qualifiers
        var qualifierMatches = MileageQualifierPattern.Matches(query);
        foreach (Match match in qualifierMatches)
        {
            var mileage = ParseMileage(match.Groups[1].Value);
            entities.Add(new ExtractedEntity
            {
                Type = EntityType.Mileage,
                Value = mileage.ToString(),
                Confidence = 0.95,
                StartPosition = match.Index,
                EndPosition = match.Index + match.Length
            });
        }

        // Check for simple mileage patterns (only if not overlapping with prices)
        if (entities.Count == 0)
        {
            var mileageMatches = MileagePattern.Matches(query);
            var priceMatches = PricePattern.Matches(query);

            foreach (Match match in mileageMatches)
            {
                // Skip if it looks like a price (has £ or "pound")
                if (query.Substring(Math.Max(0, match.Index - 2), Math.Min(10, query.Length - Math.Max(0, match.Index - 2)))
                    .Contains("£") || match.Value.ToLower().Contains("pound"))
                {
                    continue;
                }

                // Skip if overlapping with a price match
                if (IsOverlapping(match, priceMatches))
                {
                    continue;
                }

                var mileage = ParseMileage(match.Groups[1].Value);
                // Only consider reasonable mileage values (1,000 to 500,000)
                if (mileage >= 1000 && mileage <= 500000 && query.ToLower().Contains("mile"))
                {
                    entities.Add(new ExtractedEntity
                    {
                        Type = EntityType.Mileage,
                        Value = mileage.ToString(),
                        Confidence = 0.85,
                        StartPosition = match.Index,
                        EndPosition = match.Index + match.Length
                    });
                }
            }
        }

        return entities;
    }

    /// <summary>
    /// Extracts year entities from the query.
    /// </summary>
    public static List<ExtractedEntity> ExtractYears(string query)
    {
        var entities = new List<ExtractedEntity>();
        var matches = YearPattern.Matches(query);

        foreach (Match match in matches)
        {
            entities.Add(new ExtractedEntity
            {
                Type = EntityType.Year,
                Value = match.Value,
                Confidence = 0.95,
                StartPosition = match.Index,
                EndPosition = match.Index + match.Length
            });
        }

        return entities;
    }

    /// <summary>
    /// Parses a price string to a numeric value.
    /// </summary>
    private static int ParsePrice(string priceStr)
    {
        // Remove commas
        priceStr = priceStr.Replace(",", "");

        // Handle 'k' suffix (thousands)
        if (priceStr.EndsWith("k", StringComparison.OrdinalIgnoreCase))
        {
            priceStr = priceStr[..^1];
            if (int.TryParse(priceStr, out int thousands))
            {
                return thousands * 1000;
            }
        }

        if (int.TryParse(priceStr, out int price))
        {
            return price;
        }

        return 0;
    }

    /// <summary>
    /// Parses a mileage string to a numeric value.
    /// </summary>
    private static int ParseMileage(string mileageStr)
    {
        // Remove commas
        mileageStr = mileageStr.Replace(",", "");

        // Handle 'k' suffix (thousands)
        if (mileageStr.EndsWith("k", StringComparison.OrdinalIgnoreCase))
        {
            mileageStr = mileageStr[..^1];
            if (int.TryParse(mileageStr, out int thousands))
            {
                return thousands * 1000;
            }
        }

        if (int.TryParse(mileageStr, out int mileage))
        {
            return mileage;
        }

        return 0;
    }

    /// <summary>
    /// Checks if a match overlaps with any matches in a collection.
    /// </summary>
    private static bool IsOverlapping(Match match, MatchCollection otherMatches)
    {
        foreach (Match other in otherMatches)
        {
            if (match.Index >= other.Index && match.Index < other.Index + other.Length)
            {
                return true;
            }
            if (other.Index >= match.Index && other.Index < match.Index + match.Length)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Calculates Levenshtein distance for fuzzy matching.
    /// </summary>
    public static int LevenshteinDistance(string source, string target)
    {
        if (string.IsNullOrEmpty(source))
        {
            return string.IsNullOrEmpty(target) ? 0 : target.Length;
        }

        if (string.IsNullOrEmpty(target))
        {
            return source.Length;
        }

        var distance = new int[source.Length + 1, target.Length + 1];

        for (int i = 0; i <= source.Length; i++)
        {
            distance[i, 0] = i;
        }

        for (int j = 0; j <= target.Length; j++)
        {
            distance[0, j] = j;
        }

        for (int i = 1; i <= source.Length; i++)
        {
            for (int j = 1; j <= target.Length; j++)
            {
                int cost = (target[j - 1] == source[i - 1]) ? 0 : 1;
                distance[i, j] = Math.Min(
                    Math.Min(distance[i - 1, j] + 1, distance[i, j - 1] + 1),
                    distance[i - 1, j - 1] + cost);
            }
        }

        return distance[source.Length, target.Length];
    }
}
