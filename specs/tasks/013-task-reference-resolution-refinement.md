# Task: Reference Resolution & Query Refinement

**Task ID:** 013  
**Feature:** Conversational Context Management  
**Type:** Backend Implementation  
**Priority:** Medium  
**Estimated Complexity:** High  
**FRD Reference:** FRD-003 (FR-4, FR-5, FR-6)

---

## Description

Implement reference resolution to understand pronouns ("it", "them"), comparative terms ("cheaper", "bigger"), and refinement queries that build upon previous searches using conversation context.

---

## Dependencies

**Depends on:**
- Task 012: Session Context Storage & Management (needs history)
- Task 007: Query Intent Classification (for refine intent)

**Blocks:**
- Task 014: Search Strategy Selection (enhanced query input)

---

## Technical Requirements

### Reference Resolver Service Interface

```csharp
public interface IReferenceResolverService
{
    Task<ResolvedQuery> ResolveReferencesAsync(string query, ConversationSession session);
    Task<ResolvedQuery> ResolveComparativesAsync(ParsedQuery parsedQuery, SearchState searchState);
    Task<List<string>> ExtractReferentsAsync(string query);
}

public class ResolvedQuery
{
    public string OriginalQuery { get; set; }
    public string ResolvedQuery { get; set; }
    public List<Reference> ResolvedReferences { get; set; }
    public Dictionary<string, object> ResolvedValues { get; set; }
    public bool HasUnresolvedReferences { get; set; }
}

public class Reference
{
    public string ReferenceText { get; set; }
    public ReferenceType Type { get; set; }
    public string ResolvedValue { get; set; }
    public int Position { get; set; }
}

public enum ReferenceType
{
    Pronoun,          // "it", "them", "those"
    Demonstrative,    // "this", "that", "these"
    Comparative,      // "cheaper", "more expensive", "bigger"
    Anaphoric,        // "the BMW", "the previous one"
    Implicit          // "cheaper" (implies previous price)
}
```

### Pronoun Resolution

**Resolve pronouns to vehicles or attributes:**

**"it" / "that" → Last single result or last mentioned vehicle**
```
Previous: "Show me the BMW 320d"
Current: "Tell me more about it"
→ Resolved: "Tell me more about [BMW 320d - V001]"
```

**"them" / "those" → Last result set**
```
Previous: "Show me BMW cars under £20k" (returned V001, V002, V003)
Current: "Show me cheaper ones from them"
→ Resolved: "Show me cheaper ones from [V001, V002, V003]"
```

**"the first/second/last one"**
```
Previous: Results [V001, V002, V003]
Current: "Show me the second one"
→ Resolved: "Show me [V002]"
```

**Implementation:**

```csharp
public async Task<ResolvedQuery> ResolveReferencesAsync(
    string query,
    ConversationSession session)
{
    var lowerQuery = query.ToLower();
    var references = new List<Reference>();
    var resolvedQuery = query;
    
    // Resolve "it" / "that"
    if (ContainsPronoun(lowerQuery, new[] { "it", "that" }))
    {
        var lastVehicle = GetLastSingleVehicle(session);
        if (lastVehicle != null)
        {
            var reference = new Reference
            {
                ReferenceText = "it",
                Type = ReferenceType.Pronoun,
                ResolvedValue = lastVehicle.Id
            };
            references.Add(reference);
            resolvedQuery = resolvedQuery.Replace("it", $"vehicle {lastVehicle.Id}");
        }
    }
    
    // Resolve "them" / "those"
    if (ContainsPronoun(lowerQuery, new[] { "them", "those" }))
    {
        var lastResults = session.CurrentSearchState?.LastResultIds;
        if (lastResults?.Any() == true)
        {
            var reference = new Reference
            {
                ReferenceText = "them",
                Type = ReferenceType.Pronoun,
                ResolvedValue = string.Join(",", lastResults)
            };
            references.Add(reference);
            // Don't replace in query, use as filter constraint
        }
    }
    
    // Resolve positional references
    var positionalPattern = @"(first|second|third|last|previous)\s+one";
    var match = Regex.Match(lowerQuery, positionalPattern);
    if (match.Success)
    {
        var position = match.Groups[1].Value;
        var vehicleId = GetVehicleByPosition(session, position);
        if (vehicleId != null)
        {
            references.Add(new Reference
            {
                ReferenceText = match.Value,
                Type = ReferenceType.Anaphoric,
                ResolvedValue = vehicleId
            });
            resolvedQuery = resolvedQuery.Replace(match.Value, $"vehicle {vehicleId}");
        }
    }
    
    return new ResolvedQuery
    {
        OriginalQuery = query,
        ResolvedQuery = resolvedQuery,
        ResolvedReferences = references,
        HasUnresolvedReferences = references.Any(r => r.ResolvedValue == null)
    };
}

private string GetLastSingleVehicle(ConversationSession session)
{
    // Get last search with exactly 1 result OR last explicitly mentioned vehicle
    var lastMessage = session.Messages
        .Where(m => m.Role == MessageRole.Assistant && m.Results != null)
        .OrderByDescending(m => m.Timestamp)
        .FirstOrDefault();
    
    if (lastMessage?.Results?.ResultIds?.Count == 1)
        return lastMessage.Results.ResultIds[0];
    
    return null;
}

private string GetVehicleByPosition(ConversationSession session, string position)
{
    var lastResults = session.CurrentSearchState?.LastResultIds;
    if (lastResults == null || !lastResults.Any()) return null;
    
    return position switch
    {
        "first" => lastResults.First(),
        "second" => lastResults.ElementAtOrDefault(1),
        "third" => lastResults.ElementAtOrDefault(2),
        "last" => lastResults.Last(),
        "previous" => lastResults.Last(),
        _ => null
    };
}
```

### Comparative Resolution

**Resolve comparative terms against previous constraints:**

**"cheaper" → price < previous price**
```
Previous: "BMW under £20k" (max price: 20000)
Current: "Show me cheaper ones"
→ Resolved: price < 20000 (or price <= 18000, 10% cheaper)
```

**"more expensive" → price > previous price**
```
Previous: "Cars under £15k"
Current: "What about more expensive ones?"
→ Resolved: price > 15000
```

**"lower mileage" → mileage < previous mileage**
```
Previous: "Cars with under 50k miles"
Current: "Show me lower mileage"
→ Resolved: mileage < 50000
```

**"bigger" / "larger" → depends on context**
```
Previous: "Small cars" (interpreted as bodyType or engineSize)
Current: "Show me bigger ones"
→ Resolved: bodyType IN ['SUV', 'MPV', 'Estate'] OR engineSize > previous
```

**Implementation:**

```csharp
public async Task<ResolvedQuery> ResolveComparativesAsync(
    ParsedQuery parsedQuery,
    SearchState searchState)
{
    var comparatives = parsedQuery.Entities
        .Where(e => e.Type == EntityType.QualitativeTerm && IsComparative(e.Value))
        .ToList();
    
    var resolvedValues = new Dictionary<string, object>();
    
    foreach (var comparative in comparatives)
    {
        var term = comparative.Value.ToLower();
        var resolved = term switch
        {
            "cheaper" or "less expensive" => ResolveComparativePrice(searchState, -0.1),
            "more expensive" or "pricier" => ResolveComparativePrice(searchState, 0.1),
            "lower mileage" or "less mileage" => ResolveComparativeMileage(searchState, -0.1),
            "higher mileage" or "more mileage" => ResolveComparativeMileage(searchState, 0.1),
            "newer" => ResolveComparativeDate(searchState, isNewer: true),
            "older" => ResolveComparativeDate(searchState, isNewer: false),
            "bigger" or "larger" => ResolveComparativeSize(searchState, increase: true),
            "smaller" => ResolveComparativeSize(searchState, increase: false),
            _ => null
        };
        
        if (resolved != null)
        {
            resolvedValues[comparative.Value] = resolved;
        }
    }
    
    return new ResolvedQuery
    {
        OriginalQuery = parsedQuery.OriginalQuery,
        ResolvedQuery = parsedQuery.OriginalQuery,
        ResolvedValues = resolvedValues
    };
}

private SearchConstraint ResolveComparativePrice(SearchState state, double percentChange)
{
    var lastPriceConstraint = state.ActiveFilters?.Values
        .FirstOrDefault(f => f.FieldName == "price");
    
    if (lastPriceConstraint == null) return null;
    
    var basePrice = Convert.ToDouble(lastPriceConstraint.Value);
    var newPrice = basePrice * (1 + percentChange);
    
    return new SearchConstraint
    {
        FieldName = "price",
        Operator = percentChange < 0 ? ConstraintOperator.LessThan : ConstraintOperator.GreaterThan,
        Value = (int)newPrice
    };
}

private SearchConstraint ResolveComparativeMileage(SearchState state, double percentChange)
{
    var lastMileageConstraint = state.ActiveFilters?.Values
        .FirstOrDefault(f => f.FieldName == "mileage");
    
    if (lastMileageConstraint == null) return null;
    
    var baseMileage = Convert.ToDouble(lastMileageConstraint.Value);
    var newMileage = baseMileage * (1 + percentChange);
    
    return new SearchConstraint
    {
        FieldName = "mileage",
        Operator = percentChange < 0 ? ConstraintOperator.LessThan : ConstraintOperator.GreaterThan,
        Value = (int)newMileage
    };
}
```

### Query Refinement

**Build upon previous query:**

```
Previous: "Show me BMW cars"
Current: "Under £20k with leather"
→ Combined: make=BMW AND price<=20000 AND features CONTAINS leather
```

**Implementation:**

```csharp
public async Task<ComposedQuery> RefineQueryAsync(
    ParsedQuery newQuery,
    SearchState searchState)
{
    // Start with previous constraints
    var constraints = new List<SearchConstraint>();
    
    if (searchState.ActiveFilters != null)
    {
        constraints.AddRange(searchState.ActiveFilters.Values);
    }
    
    // Add new constraints
    var newMappedQuery = await _attributeMapper.MapToSearchQueryAsync(newQuery);
    constraints.AddRange(newMappedQuery.Constraints);
    
    // Remove duplicates (prefer new values)
    var deduplicated = constraints
        .GroupBy(c => c.FieldName)
        .Select(g => g.Last())  // Last wins (new constraint)
        .ToList();
    
    return new ComposedQuery
    {
        Type = QueryType.Filtered,
        ConstraintGroups = new List<ConstraintGroup>
        {
            new()
            {
                Constraints = deduplicated,
                Operator = LogicalOperator.And
            }
        }
    };
}
```

### API Endpoints

**POST /api/v1/query/resolve**

Request:
```json
{
  "query": "Show me cheaper ones",
  "sessionId": "abc123"
}
```

Response:
```json
{
  "originalQuery": "Show me cheaper ones",
  "resolvedQuery": "Show me vehicles with price < 18000",
  "resolvedReferences": [
    {
      "referenceText": "cheaper",
      "type": "Comparative",
      "resolvedValue": "price < 18000"
    }
  ],
  "resolvedValues": {
    "cheaper": {
      "fieldName": "price",
      "operator": "LessThan",
      "value": 18000
    }
  },
  "hasUnresolvedReferences": false
}
```

---

## Acceptance Criteria

### Functional Criteria

✅ **Pronoun Resolution:**
- [ ] "it" resolves to last single vehicle
- [ ] "them" resolves to last result set
- [ ] "the first one" resolves to first result
- [ ] All pronouns handled

✅ **Comparative Resolution:**
- [ ] "cheaper" resolves to lower price
- [ ] "lower mileage" resolves correctly
- [ ] "bigger" infers context (size/engine)
- [ ] All comparatives work

✅ **Query Refinement:**
- [ ] New constraints added to previous
- [ ] Duplicate fields updated (not duplicated)
- [ ] Works across multiple refinements

✅ **Edge Cases:**
- [ ] No previous context → return unresolved
- [ ] Ambiguous references → ask for clarification
- [ ] Multiple references in one query work

### Technical Criteria

✅ **Performance:**
- [ ] Reference resolution <100ms
- [ ] Works with 10+ message history

✅ **Accuracy:**
- [ ] 90%+ pronoun resolution accuracy
- [ ] 85%+ comparative resolution accuracy

---

## Testing Requirements

### Unit Tests

**Test Coverage:** ≥85%

**Test Cases:**
```csharp
[Fact]
public async Task ResolveReferences_ItPronoun_ResolvesToLastVehicle()

[Fact]
public async Task ResolveReferences_ThemPronoun_ResolvesToLastResults()

[Fact]
public async Task ResolveReferences_FirstOne_ResolvesToFirstResult()

[Fact]
public async Task ResolveComparative_Cheaper_LowersPriceConstraint()

[Fact]
public async Task RefineQuery_NewConstraints_AddsToExisting()

[Fact]
public async Task RefineQuery_DuplicateField_ReplacesOldValue()
```

### Integration Tests

- [ ] Test 15 reference scenarios from FRD
- [ ] Test multi-turn conversations
- [ ] Test refinement chains (3+ turns)

---

## Definition of Done

- [ ] Reference resolver service implemented
- [ ] Pronoun resolution working
- [ ] Comparative resolution working
- [ ] Query refinement functional
- [ ] API endpoint functional
- [ ] All unit tests pass (≥85% coverage)
- [ ] Integration tests pass
- [ ] Documentation updated
- [ ] Code reviewed and approved

---

## Related Files

**Created/Modified:**
- `src/VehicleSearch.Infrastructure/AI/ReferenceResolverService.cs`
- `src/VehicleSearch.Infrastructure/AI/ComparativeResolver.cs`
- `src/VehicleSearch.Core/Interfaces/IReferenceResolverService.cs`
- `src/VehicleSearch.Core/Models/ResolvedQuery.cs`
- `src/VehicleSearch.Api/Controllers/QueryController.cs`
- `tests/VehicleSearch.Infrastructure.Tests/ReferenceResolverServiceTests.cs`

**References:**
- FRD-003: Conversational Context Management (FR-4, FR-5, FR-6)
- Task 012: Session context
- Task 007: Intent classification
