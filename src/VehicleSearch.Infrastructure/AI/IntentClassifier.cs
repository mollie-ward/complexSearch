using System.Text.Json;
using System.Text.RegularExpressions;
using Azure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;
using VehicleSearch.Core.Interfaces;
using VehicleSearch.Core.Models;

namespace VehicleSearch.Infrastructure.AI;

/// <summary>
/// Service for classifying the intent of user queries using LLM with pattern-based fallback.
/// </summary>
public class IntentClassifier : IIntentClassifier
{
    private readonly ILogger<IntentClassifier> _logger;
    private readonly ChatClient? _chatClient;
    private readonly Dictionary<string, (QueryIntent Intent, double Confidence)> _cache;

    // Intent patterns for fallback
    private static readonly Dictionary<QueryIntent, List<Regex>> IntentPatterns = new()
    {
        {
            QueryIntent.Search,
            new List<Regex>
            {
                new(@"\b(find|show|looking\s+for|want|need|search)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                new(@"\b(get\s+me|I\s+want|I\s+need)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)
            }
        },
        {
            QueryIntent.Refine,
            new List<Regex>
            {
                new(@"\b(cheaper|more\s+expensive|bigger|smaller|better)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                new(@"\b(what\s+about|instead|rather|different)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                new(@"\b(narrow\s+down|filter|refine)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)
            }
        },
        {
            QueryIntent.Compare,
            new List<Regex>
            {
                new(@"\b(compare|comparison|difference|versus|vs\.?)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                new(@"\b(which\s+is\s+better|better\s+than)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)
            }
        },
        {
            QueryIntent.Information,
            new List<Regex>
            {
                new(@"\b(how\s+many|tell\s+me|what\s+is|explain)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                new(@"\b(do\s+you\s+have|can\s+you\s+tell)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)
            }
        }
    };

    public IntentClassifier(IOptions<AzureOpenAIConfig> config, ILogger<IntentClassifier> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (config?.Value == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        var openAIConfig = config.Value;

        if (string.IsNullOrWhiteSpace(openAIConfig.Endpoint))
        {
            _logger.LogWarning("Azure OpenAI endpoint not configured. LLM-based classification will not be available.");
            _chatClient = null;
        }
        else if (string.IsNullOrWhiteSpace(openAIConfig.ApiKey))
        {
            _logger.LogWarning("Azure OpenAI API key not configured. LLM-based classification will not be available.");
            _chatClient = null;
        }
        else
        {
            var openAIClient = new OpenAIClient(
                new AzureKeyCredential(openAIConfig.ApiKey),
                new OpenAIClientOptions
                {
                    Endpoint = new Uri(openAIConfig.Endpoint)
                });
            
            // Use chat deployment name from config or default
            var deploymentName = openAIConfig.ChatDeploymentName ?? "gpt-4";
            _chatClient = openAIClient.GetChatClient(deploymentName);
        }

        _cache = new Dictionary<string, (QueryIntent, double)>();
    }

    /// <inheritdoc/>
    public async Task<(QueryIntent Intent, double Confidence)> ClassifyAsync(
        string query,
        ConversationContext? context = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new ArgumentException("Query cannot be null or empty.", nameof(query));
        }

        // Check cache first
        var cacheKey = GetCacheKey(query, context);
        if (_cache.TryGetValue(cacheKey, out var cached))
        {
            _logger.LogDebug("Returning cached intent classification for query: {Query}", query);
            return cached;
        }

        // Try LLM classification first
        if (_chatClient != null)
        {
            try
            {
                var result = await ClassifyWithLLMAsync(query, context, cancellationToken);
                _cache[cacheKey] = result;
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "LLM classification failed, falling back to pattern matching");
            }
        }

        // Fallback to pattern matching
        var patternResult = ClassifyWithPatterns(query, context);
        _cache[cacheKey] = patternResult;
        return patternResult;
    }

    private async Task<(QueryIntent Intent, double Confidence)> ClassifyWithLLMAsync(
        string query,
        ConversationContext? context,
        CancellationToken cancellationToken)
    {
        var previousQuery = context?.History.LastOrDefault() ?? "";
        var prompt = BuildPrompt(query, previousQuery);

        var chatOptions = new ChatCompletionOptions
        {
            Temperature = 0.2f, // Low temperature for consistency
            MaxOutputTokenCount = 100,
            ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat()
        };

        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(@"You are a vehicle search assistant. Classify user queries into one of these intents:
- search: User wants to find vehicles
- refine: User wants to modify previous search
- compare: User wants to compare vehicles
- information: User asking for information
- off_topic: Query not related to vehicles

Respond with JSON: {""intent"": ""search"", ""confidence"": 0.95}"),
            new UserChatMessage(prompt)
        };

        var response = await _chatClient!.CompleteChatAsync(messages, chatOptions, cancellationToken);
        var content = response.Value.Content[0].Text;

        return ParseLLMResponse(content);
    }

    private (QueryIntent Intent, double Confidence) ClassifyWithPatterns(string query, ConversationContext? context)
    {
        _logger.LogDebug("Using pattern-based classification for query: {Query}", query);

        // Check for vehicle-related keywords to determine if off-topic
        var vehicleKeywords = new[] { "car", "vehicle", "bmw", "audi", "mercedes", "ford", "toyota", 
            "price", "mileage", "diesel", "petrol", "automatic", "manual" };
        var hasVehicleKeywords = vehicleKeywords.Any(k => query.Contains(k, StringComparison.OrdinalIgnoreCase));

        if (!hasVehicleKeywords)
        {
            return (QueryIntent.OffTopic, 0.8);
        }

        // Score each intent based on pattern matches
        var scores = new Dictionary<QueryIntent, int>();
        foreach (var (intent, patterns) in IntentPatterns)
        {
            scores[intent] = patterns.Count(p => p.IsMatch(query));
        }

        // Get the intent with the highest score
        var maxScore = scores.Values.Max();
        if (maxScore == 0)
        {
            // Default to Search if we have vehicle keywords but no clear pattern
            return (QueryIntent.Search, 0.6);
        }

        var topIntent = scores.First(kvp => kvp.Value == maxScore).Key;
        
        // Calculate confidence based on score
        var confidence = Math.Min(0.7 + (maxScore * 0.1), 0.95);

        return (topIntent, confidence);
    }

    private string BuildPrompt(string query, string previousQuery)
    {
        if (string.IsNullOrWhiteSpace(previousQuery))
        {
            return $"Query: \"{query}\"";
        }

        return $"Query: \"{query}\"\nContext: \"{previousQuery}\"";
    }

    private (QueryIntent Intent, double Confidence) ParseLLMResponse(string content)
    {
        try
        {
            var jsonDoc = JsonDocument.Parse(content);
            var root = jsonDoc.RootElement;

            var intentStr = root.GetProperty("intent").GetString()?.ToLower() ?? "search";
            var confidence = root.TryGetProperty("confidence", out var confElement) 
                ? confElement.GetDouble() 
                : 0.8;

            var intent = intentStr switch
            {
                "search" => QueryIntent.Search,
                "refine" => QueryIntent.Refine,
                "compare" => QueryIntent.Compare,
                "information" => QueryIntent.Information,
                "off_topic" => QueryIntent.OffTopic,
                _ => QueryIntent.Search
            };

            return (intent, confidence);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse LLM response, defaulting to Search intent");
            return (QueryIntent.Search, 0.5);
        }
    }

    private string GetCacheKey(string query, ConversationContext? context)
    {
        var key = query.ToLowerInvariant();
        if (context?.History.Any() == true)
        {
            key += $"|{context.History.Last().ToLowerInvariant()}";
        }
        return key;
    }
}
