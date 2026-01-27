using Microsoft.Extensions.Logging;
using VehicleSearch.Core.Interfaces;
using VehicleSearch.Core.Models;

namespace VehicleSearch.Infrastructure.AI;

/// <summary>
/// Service for understanding and parsing natural language queries.
/// Orchestrates intent classification and entity extraction.
/// </summary>
public class QueryUnderstandingService : IQueryUnderstandingService
{
    private readonly IIntentClassifier _intentClassifier;
    private readonly IEntityExtractor _entityExtractor;
    private readonly ILogger<QueryUnderstandingService> _logger;

    public QueryUnderstandingService(
        IIntentClassifier intentClassifier,
        IEntityExtractor entityExtractor,
        ILogger<QueryUnderstandingService> logger)
    {
        _intentClassifier = intentClassifier ?? throw new ArgumentNullException(nameof(intentClassifier));
        _entityExtractor = entityExtractor ?? throw new ArgumentNullException(nameof(entityExtractor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<ParsedQuery> ParseQueryAsync(
        string query,
        ConversationContext? context = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new ArgumentException("Query cannot be null or empty.", nameof(query));
        }

        _logger.LogInformation("Parsing query: {Query}", query);

        try
        {
            // Classify intent and extract entities in parallel
            var intentTask = _intentClassifier.ClassifyAsync(query, context, cancellationToken);
            var entitiesTask = _entityExtractor.ExtractAsync(query, cancellationToken);

            await Task.WhenAll(intentTask, entitiesTask);

            var (intent, confidence) = await intentTask;
            var entities = (await entitiesTask).ToList();

            // Identify unmapped terms (words that weren't extracted as entities)
            var unmappedTerms = IdentifyUnmappedTerms(query, entities);

            var result = new ParsedQuery
            {
                OriginalQuery = query,
                Intent = intent,
                Entities = entities,
                ConfidenceScore = confidence,
                UnmappedTerms = unmappedTerms
            };

            _logger.LogInformation(
                "Parsed query successfully. Intent: {Intent}, Confidence: {Confidence}, Entities: {EntityCount}",
                intent, confidence, entities.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing query: {Query}", query);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<QueryIntent> ClassifyIntentAsync(
        string query,
        ConversationContext? context = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new ArgumentException("Query cannot be null or empty.", nameof(query));
        }

        var (intent, _) = await _intentClassifier.ClassifyAsync(query, context, cancellationToken);
        return intent;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ExtractedEntity>> ExtractEntitiesAsync(
        string query,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new ArgumentException("Query cannot be null or empty.", nameof(query));
        }

        return await _entityExtractor.ExtractAsync(query, cancellationToken);
    }

    /// <summary>
    /// Identifies words in the query that weren't extracted as entities.
    /// </summary>
    private List<string> IdentifyUnmappedTerms(string query, List<ExtractedEntity> entities)
    {
        // Common words to ignore
        var stopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "a", "an", "the", "and", "or", "but", "is", "are", "was", "were", "be", "been",
            "being", "have", "has", "had", "do", "does", "did", "will", "would", "should",
            "could", "may", "might", "can", "i", "you", "he", "she", "it", "we", "they",
            "show", "me", "find", "get", "want", "need", "looking", "for", "with", "in",
            "on", "at", "to", "from", "of", "by", "about", "under", "over", "between",
            "what", "which", "who", "when", "where", "why", "how"
        };

        // Get all words from the query
        var words = query
            .Split(new[] { ' ', ',', '.', '!', '?', '-', '(', ')', '[', ']' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 2 && !stopWords.Contains(w))
            .ToList();

        // Remove words that are part of extracted entities
        var unmapped = new List<string>();
        foreach (var word in words)
        {
            var isExtracted = entities.Any(e => 
                e.Value.Contains(word, StringComparison.OrdinalIgnoreCase) ||
                query.Substring(e.StartPosition, e.EndPosition - e.StartPosition)
                    .Contains(word, StringComparison.OrdinalIgnoreCase));

            if (!isExtracted)
            {
                unmapped.Add(word);
            }
        }

        return unmapped.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }
}
