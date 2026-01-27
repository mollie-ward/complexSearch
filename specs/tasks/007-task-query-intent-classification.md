# Task: Query Intent Classification & Entity Extraction

**Task ID:** 007  
**GitHub Issue:** [#15](https://github.com/mollie-ward/complexSearch/issues/15)  
**Feature:** Natural Language Query Understanding  
**Type:** Backend Implementation  
**Priority:** High  
**Estimated Complexity:** High  
**FRD Reference:** FRD-001 (FR-1, FR-2)

---

## Description

Implement natural language understanding service to classify user query intent (search, refine, compare, off-topic) and extract vehicle-related entities (make, model, price, mileage, features) from conversational English queries.

---

## Dependencies

**Depends on:**
- Task 001: Backend API Scaffolding
- Task 005: Azure AI Search Index Setup (for field schema knowledge)

**Blocks:**
- Task 008: Attribute Mapping & Constraint Parsing
- Task 010: Query Embedding & Semantic Matching
- Task 016: Input Validation & Safety Rules

---

## Technical Requirements

### Query Understanding Service Interface

```csharp
public interface IQueryUnderstandingService
{
    Task<ParsedQuery> ParseQueryAsync(string query, ConversationContext? context = null);
    Task<QueryIntent> ClassifyIntentAsync(string query);
    Task<IEnumerable<ExtractedEntity>> ExtractEntitiesAsync(string query);
}

public class ParsedQuery
{
    public string OriginalQuery { get; set; }
    public QueryIntent Intent { get; set; }
    public List<ExtractedEntity> Entities { get; set; }
    public double ConfidenceScore { get; set; }
    public List<string> UnmappedTerms { get; set; }
}

public enum QueryIntent
{
    Search,          // New vehicle search
    Refine,          // Refine previous results
    Compare,         // Compare vehicles
    Information,     // Ask for information
    OffTopic        // Not related to vehicles
}

public class ExtractedEntity
{
    public EntityType Type { get; set; }
    public string Value { get; set; }
    public double Confidence { get; set; }
    public int StartPosition { get; set; }
    public int EndPosition { get; set; }
}

public enum EntityType
{
    Make,
    Model,
    Derivative,
    Price,
    PriceRange,
    Mileage,
    EngineSize,
    FuelType,
    Transmission,
    BodyType,
    Colour,
    Feature,
    Location,
    Year,
    QualitativeTerm  // "reliable", "economical", etc.
}
```

### Intent Classification

**Approach:** Use LLM-based classification or pattern matching

**Classification Rules:**

**Search Intent:**
- Patterns: "find", "show me", "looking for", "want", "need"
- Example: "Show me BMW cars"
- Confidence: High if clear vehicle attributes present

**Refine Intent:**
- Patterns: "cheaper", "more expensive", "bigger", "smaller", "what about", "instead"
- Requires: Previous conversation context
- Example: "Show me cheaper ones"

**Compare Intent:**
- Patterns: "compare", "difference between", "vs", "versus"
- Example: "Compare BMW 3 series to Audi A4"

**Information Intent:**
- Patterns: "how many", "tell me about", "what is"
- Example: "How many electric cars do you have?"

**Off-Topic Intent:**
- No vehicle-related entities
- General questions
- Example: "What's the weather?"

**LLM Prompt for Classification:**
```
Classify the following user query into one of these intents:
- search: User wants to find vehicles
- refine: User wants to modify previous search
- compare: User wants to compare vehicles
- information: User asking for information
- off_topic: Query not related to vehicles

Query: "{query}"
Context: "{previous_query}"

Respond with JSON: {"intent": "search", "confidence": 0.95}
```

### Entity Extraction

**Make Extraction:**
- Known makes from knowledge base
- Fuzzy matching: "beamer" → "BMW"
- Multi-word: "Alfa Romeo", "Land Rover"

**Model Extraction:**
- Known models from knowledge base
- Handle variations: "3 series" → "3", "Q3" → "Q3"
- Context-aware: "320d" needs "BMW" context

**Price Extraction:**
- Patterns: "£20,000", "£20k", "20000", "twenty thousand"
- Range: "£15k-£25k", "between £15,000 and £25,000"
- Qualifiers: "under £20k", "less than £20,000", "up to £20k"

**Mileage Extraction:**
- Patterns: "50,000 miles", "50k", "low mileage"
- Qualifiers: "under 50k miles", "less than 50,000"
- Defaults: "low mileage" → <= 30,000

**Feature Extraction:**
- Known features from knowledge base
- Patterns: "leather", "navigation", "parking sensors"
- Synonyms: "nav" → "navigation", "sat nav" → "navigation"

**Location Extraction:**
- Known locations from knowledge base
- Cities: "Manchester", "Leeds", "London"
- Regions: "North", "South", "Midlands"

**Qualitative Terms:**
- "reliable", "economical", "family car", "sporty"
- Store for later conceptual mapping (Task 011)

### Pattern-Based Extraction (Regex)

```csharp
// Price patterns
private static readonly Regex PricePattern = new(
    @"£?\s*(\d{1,3}(?:,\d{3})*|\d+)k?\s*(?:pounds?)?",
    RegexOptions.IgnoreCase
);

// Mileage patterns
private static readonly Regex MileagePattern = new(
    @"(\d{1,3}(?:,\d{3})*|\d+)k?\s*(?:miles?)?",
    RegexOptions.IgnoreCase
);

// Year patterns
private static readonly Regex YearPattern = new(
    @"\b(19\d{2}|20[0-2]\d)\b"
);
```

### Confidence Scoring

**High Confidence (>0.8):**
- Exact make/model match
- Clear price with currency symbol
- Explicit intent patterns

**Medium Confidence (0.5-0.8):**
- Partial matches
- Implied constraints
- Contextual entities

**Low Confidence (<0.5):**
- Ambiguous terms
- Vague qualitative terms
- No clear entities

### API Endpoints

**POST /api/v1/query/parse**

Request:
```json
{
  "query": "Show me BMW 3 series under £20,000",
  "conversationId": "abc123"
}
```

Response:
```json
{
  "originalQuery": "Show me BMW 3 series under £20,000",
  "intent": "search",
  "confidence": 0.92,
  "entities": [
    {
      "type": "Make",
      "value": "BMW",
      "confidence": 1.0,
      "startPosition": 8,
      "endPosition": 11
    },
    {
      "type": "Model",
      "value": "3 series",
      "confidence": 0.95,
      "startPosition": 12,
      "endPosition": 20
    },
    {
      "type": "Price",
      "value": "20000",
      "confidence": 1.0,
      "startPosition": 27,
      "endPosition": 35
    }
  ],
  "unmappedTerms": []
}
```

---

## Acceptance Criteria

### Functional Criteria

✅ **Intent Classification:**
- [ ] Search queries correctly classified (90%+ accuracy)
- [ ] Refine queries detected with context
- [ ] Off-topic queries flagged
- [ ] Confidence scores accurate

✅ **Entity Extraction:**
- [ ] Vehicle makes extracted (95%+ accuracy)
- [ ] Models extracted correctly
- [ ] Prices parsed correctly (£20k → 20000)
- [ ] Mileage values extracted
- [ ] Features identified
- [ ] Locations extracted

✅ **Multi-Entity Queries:**
- [ ] "BMW under £20k with leather" extracts all 3 entities
- [ ] All entities have correct types
- [ ] Position information accurate

✅ **Edge Cases:**
- [ ] Ambiguous queries handled
- [ ] Typos tolerated ("auddi" → "Audi")
- [ ] Multi-word makes work ("Alfa Romeo")
- [ ] Price ranges parsed correctly

### Technical Criteria

✅ **Performance:**
- [ ] Query parsing <500ms
- [ ] Supports 1000 concurrent requests
- [ ] LLM calls cached when possible

✅ **Accuracy:**
- [ ] 90%+ intent classification accuracy
- [ ] 95%+ make/model extraction accuracy
- [ ] 90%+ price/mileage parsing accuracy

---

## Testing Requirements

### Unit Tests

**Test Coverage:** ≥85%

**Test Cases:**
```csharp
[Theory]
[InlineData("Show me BMW cars", QueryIntent.Search)]
[InlineData("Cheaper ones", QueryIntent.Refine)]
[InlineData("Compare BMW to Audi", QueryIntent.Compare)]
[InlineData("What's the weather?", QueryIntent.OffTopic)]
public async Task ClassifyIntent_VariousQueries_ReturnsCorrectIntent(string query, QueryIntent expected)

[Fact]
public async Task ExtractEntities_BMWUnder20k_ExtractsMakeAndPrice()

[Fact]
public async Task ExtractPrice_VariousFormats_ParsesCorrectly()
// Test: "£20,000", "£20k", "20000", "twenty thousand pounds"

[Fact]
public async Task ExtractMileage_LowMileage_ReturnsDefault()

[Fact]
public async Task ExtractFeatures_MultipleFeatures_ExtractsAll()

[Fact]
public async Task ParseQuery_ComplexQuery_ExtractsAllEntities()
```

### Integration Tests

**Test Cases:**
- [ ] Parse 20 real queries from FRD examples
- [ ] Test with LLM (OpenAI/Azure OpenAI)
- [ ] Verify confidence scores reasonable
- [ ] Test context-aware parsing

---

## Implementation Notes

### DO:
- ✅ Use LLM for intent classification (GPT-4 or similar)
- ✅ Combine LLM with pattern matching for entities
- ✅ Cache LLM responses for identical queries
- ✅ Implement fuzzy matching for makes/models
- ✅ Handle UK currency and format (£, UK spellings)
- ✅ Log all parsed queries for improvement

### DON'T:
- ❌ Rely solely on regex (too brittle)
- ❌ Skip confidence scoring
- ❌ Ignore context from conversation
- ❌ Hardcode all entity values

### LLM Integration:
- Use structured output (JSON mode)
- Provide few-shot examples
- Set temperature low (0.1-0.3) for consistency
- Handle rate limiting and errors

---

## Definition of Done

- [ ] Query understanding service implemented
- [ ] Intent classification working (90%+ accuracy)
- [ ] Entity extraction working (95%+ make/model)
- [ ] API endpoint functional
- [ ] Confidence scoring implemented
- [ ] All unit tests pass (≥85% coverage)
- [ ] Integration tests with LLM pass
- [ ] Documentation updated
- [ ] Code reviewed and approved

---

## Related Files

**Created/Modified:**
- `src/VehicleSearch.Infrastructure/AI/QueryUnderstandingService.cs`
- `src/VehicleSearch.Infrastructure/AI/IntentClassifier.cs`
- `src/VehicleSearch.Infrastructure/AI/EntityExtractor.cs`
- `src/VehicleSearch.Core/Interfaces/IQueryUnderstandingService.cs`
- `src/VehicleSearch.Core/Models/ParsedQuery.cs`
- `src/VehicleSearch.Api/Controllers/QueryController.cs`
- `tests/VehicleSearch.Infrastructure.Tests/QueryUnderstandingServiceTests.cs`

**References:**
- FRD-001: Natural Language Query Understanding (FR-1, FR-2)
- AGENTS.md (AI agent patterns)
- Task 005 (for field schema)
