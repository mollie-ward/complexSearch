using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using VehicleSearch.Core.Interfaces;
using VehicleSearch.Core.Models;

namespace VehicleSearch.Infrastructure.AI;

/// <summary>
/// Service for resolving references and refining queries in conversation context.
/// </summary>
public class ReferenceResolverService : IReferenceResolverService
{
    private readonly ILogger<ReferenceResolverService> _logger;
    private readonly ComparativeResolver _comparativeResolver;
    private readonly QueryRefiner _queryRefiner;

    // Pronoun patterns
    private static readonly string[] SingularPronouns = { "it", "that", "this" };
    private static readonly string[] PluralPronouns = { "them", "those", "these" };
    private static readonly Regex PositionalPattern = 
        new(@"\b(first|second|third|fourth|fifth|last|previous)\s+(one|vehicle|car)\b", 
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Initializes a new instance of the <see cref="ReferenceResolverService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="comparativeResolver">The comparative resolver.</param>
    /// <param name="queryRefiner">The query refiner.</param>
    public ReferenceResolverService(
        ILogger<ReferenceResolverService> logger,
        ComparativeResolver comparativeResolver,
        QueryRefiner queryRefiner)
    {
        _logger = logger;
        _comparativeResolver = comparativeResolver;
        _queryRefiner = queryRefiner;
    }

    /// <inheritdoc/>
    public async Task<ResolvedQuery> ResolveReferencesAsync(
        string query, 
        ConversationSession session, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Resolving references in query: '{Query}' for session {SessionId}", 
            query, session.SessionId);

        var resolvedQuery = new ResolvedQuery
        {
            OriginalQuery = query,
            ResolvedQueryText = query
        };

        // Extract references from the query
        var references = await ExtractReferentsAsync(query, cancellationToken);
        if (!references.Any())
        {
            _logger.LogDebug("No references found in query");
            return resolvedQuery;
        }

        resolvedQuery.ResolvedReferences = references;

        // Resolve pronouns
        await ResolvePronounsAsync(query, session, resolvedQuery, cancellationToken);

        // Resolve positional references
        ResolvePositionalReferences(query, session, resolvedQuery);

        return resolvedQuery;
    }

    /// <inheritdoc/>
    public async Task<Dictionary<string, SearchConstraint>> ResolveComparativesAsync(
        ParsedQuery parsedQuery, 
        SearchState searchState, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Resolving comparatives in query: '{Query}'", parsedQuery.OriginalQuery);

        var resolvedConstraints = _comparativeResolver.ResolveComparatives(
            parsedQuery.OriginalQuery, 
            searchState.ActiveFilters);

        return await Task.FromResult(resolvedConstraints);
    }

    /// <inheritdoc/>
    public async Task<List<Reference>> ExtractReferentsAsync(
        string query, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Extracting references from query: '{Query}'", query);

        var references = new List<Reference>();
        var queryLower = query.ToLowerInvariant();
        var words = queryLower.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // Check for singular pronouns
        for (int i = 0; i < words.Length; i++)
        {
            if (SingularPronouns.Contains(words[i]))
            {
                references.Add(new Reference
                {
                    ReferenceText = words[i],
                    Type = ReferenceType.Pronoun,
                    Position = i
                });
            }
            else if (PluralPronouns.Contains(words[i]))
            {
                references.Add(new Reference
                {
                    ReferenceText = words[i],
                    Type = ReferenceType.Pronoun,
                    Position = i
                });
            }
        }

        // Check for positional references
        var positionalMatches = PositionalPattern.Matches(query);
        foreach (Match match in positionalMatches)
        {
            references.Add(new Reference
            {
                ReferenceText = match.Value,
                Type = ReferenceType.Anaphoric,
                Position = match.Index
            });
        }

        // Check for comparatives
        foreach (var term in ComparativeResolverExtensions.GetComparativeTerms())
        {
            if (queryLower.Contains(term))
            {
                int position = queryLower.IndexOf(term, StringComparison.Ordinal);
                references.Add(new Reference
                {
                    ReferenceText = term,
                    Type = ReferenceType.Comparative,
                    Position = position
                });
            }
        }

        _logger.LogDebug("Extracted {Count} references from query", references.Count);
        return await Task.FromResult(references);
    }

    /// <inheritdoc/>
    public async Task<ComposedQuery> RefineQueryAsync(
        ParsedQuery newQuery, 
        SearchState searchState, 
        CancellationToken cancellationToken = default)
    {
        return await _queryRefiner.RefineQueryAsync(newQuery, searchState, cancellationToken);
    }

    /// <summary>
    /// Resolves pronoun references to vehicles.
    /// </summary>
    private async Task ResolvePronounsAsync(
        string query, 
        ConversationSession session, 
        ResolvedQuery resolvedQuery, 
        CancellationToken cancellationToken)
    {
        var queryLower = query.ToLowerInvariant();
        var words = queryLower.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // Check for singular pronouns -> last single result
        if (words.Any(w => SingularPronouns.Contains(w)))
        {
            var lastSingleResult = GetLastSingleResult(session);
            if (lastSingleResult != null)
            {
                resolvedQuery.ResolvedValues["vehicle_id"] = lastSingleResult;
                resolvedQuery.ResolvedQueryText = query; // Keep query as-is, value in ResolvedValues
                _logger.LogInformation("Resolved singular pronoun to vehicle: {VehicleId}", lastSingleResult);
            }
            else
            {
                resolvedQuery.HasUnresolvedReferences = true;
                resolvedQuery.UnresolvedMessage = "I don't have a specific vehicle to refer to. Which vehicle would you like to know about?";
                _logger.LogWarning("Could not resolve singular pronoun - no previous single result");
            }
        }

        // Check for plural pronouns -> last result set
        if (words.Any(w => PluralPronouns.Contains(w)))
        {
            var lastResultSet = GetLastResultSet(session);
            if (lastResultSet != null && lastResultSet.Any())
            {
                resolvedQuery.ResolvedValues["vehicle_ids"] = lastResultSet;
                resolvedQuery.ResolvedQueryText = query; // Keep query as-is, value in ResolvedValues
                _logger.LogInformation("Resolved plural pronoun to {Count} vehicles", lastResultSet.Count);
            }
            else
            {
                resolvedQuery.HasUnresolvedReferences = true;
                resolvedQuery.UnresolvedMessage = "I don't have previous search results to refer to. Please perform a search first.";
                _logger.LogWarning("Could not resolve plural pronoun - no previous result set");
            }
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Resolves positional references (e.g., "the first one", "the second one").
    /// </summary>
    private void ResolvePositionalReferences(
        string query, 
        ConversationSession session, 
        ResolvedQuery resolvedQuery)
    {
        var matches = PositionalPattern.Matches(query);
        if (!matches.Any())
            return;

        var lastResults = GetLastResultSet(session);
        if (lastResults == null || !lastResults.Any())
        {
            resolvedQuery.HasUnresolvedReferences = true;
            resolvedQuery.UnresolvedMessage = "I don't have previous search results to refer to by position.";
            _logger.LogWarning("Could not resolve positional reference - no previous results");
            return;
        }

        foreach (Match match in matches)
        {
            var position = match.Groups[1].Value.ToLowerInvariant();
            var index = position switch
            {
                "first" => 0,
                "second" => 1,
                "third" => 2,
                "fourth" => 3,
                "fifth" => 4,
                "last" => lastResults.Count - 1,
                "previous" => lastResults.Count - 1,
                _ => -1
            };

            if (index >= 0 && index < lastResults.Count)
            {
                var vehicleId = lastResults[index];
                resolvedQuery.ResolvedValues["vehicle_id"] = vehicleId;
                resolvedQuery.ResolvedQueryText = query; // Keep query as-is, value in ResolvedValues
                _logger.LogInformation("Resolved positional reference '{Position}' to vehicle: {VehicleId}", 
                    position, vehicleId);
            }
            else
            {
                resolvedQuery.HasUnresolvedReferences = true;
                resolvedQuery.UnresolvedMessage = $"I only have {lastResults.Count} result(s) from the previous search.";
                _logger.LogWarning("Could not resolve positional reference '{Position}' - index out of bounds", position);
            }
        }
    }

    /// <summary>
    /// Gets the last single vehicle result from the conversation history.
    /// </summary>
    private string? GetLastSingleResult(ConversationSession session)
    {
        var searchState = session.CurrentSearchState;
        if (searchState?.LastResultIds != null && searchState.LastResultIds.Count == 1)
        {
            return searchState.LastResultIds[0];
        }

        return null;
    }

    /// <summary>
    /// Gets the last result set from the conversation history.
    /// </summary>
    private List<string>? GetLastResultSet(ConversationSession session)
    {
        var searchState = session.CurrentSearchState;
        return searchState?.LastResultIds;
    }
}

/// <summary>
/// Extension methods for ComparativeResolver to expose comparative terms.
/// </summary>
internal static class ComparativeResolverExtensions
{
    private static readonly string[] ComparativeTerms = new[]
    {
        "cheaper", "less expensive", "more expensive", "pricier",
        "lower mileage", "less mileage", "higher mileage", "more mileage",
        "newer", "older", "bigger", "larger", "smaller"
    };

    public static string[] GetComparativeTerms(this ComparativeResolver resolver)
    {
        return ComparativeTerms;
    }

    public static string[] GetComparativeTerms()
    {
        return ComparativeTerms;
    }
}
