# Task: Multi-Criteria Query Composition

**Task ID:** 009  
**GitHub Issue:** [#19](https://github.com/mollie-ward/complexSearch/issues/19)  
**Feature:** Natural Language Query Understanding  
**Type:** Backend Implementation  
**Priority:** High  
**Estimated Complexity:** Medium  
**FRD Reference:** FRD-001 (FR-5, FR-6)

---

## Description

Compose complex search queries combining multiple constraints (make, model, price, mileage, features) with logical operators (AND/OR) and handle edge cases like contradictory constraints.

---

## Dependencies

**Depends on:**
- Task 007: Query Intent Classification & Entity Extraction
- Task 008: Attribute Mapping & Constraint Parsing

**Blocks:**
- Task 014: Search Strategy Selection & Orchestration
- Task 015: Result Ranking & Re-ranking Logic

---

## Technical Requirements

### Query Composer Service Interface

```csharp
public interface IQueryComposerService
{
    Task<ComposedQuery> ComposeQueryAsync(MappedQuery mappedQuery);
    Task<bool> ValidateQueryAsync(ComposedQuery query);
    Task<ComposedQuery> ResolveConflictsAsync(ComposedQuery query);
}

public class ComposedQuery
{
    public QueryType Type { get; set; }
    public List<ConstraintGroup> ConstraintGroups { get; set; }
    public LogicalOperator GroupOperator { get; set; }
    public List<string> Warnings { get; set; }
    public bool HasConflicts { get; set; }
}

public class ConstraintGroup
{
    public List<SearchConstraint> Constraints { get; set; }
    public LogicalOperator Operator { get; set; }
    public double Priority { get; set; }
}

public enum QueryType
{
    Simple,         // Single constraint
    Filtered,       // Multiple exact/range constraints
    Complex,        // Mixed exact + semantic
    MultiModal      // Requires multiple search strategies
}

public enum LogicalOperator
{
    And,
    Or
}
```

### Query Composition Logic

**Simple Query (1 constraint):**
```
Query: "Show me BMW cars"
→ Composed: make = "BMW"
```

**Filtered Query (2-5 exact/range constraints):**
```
Query: "BMW under £20k"
→ Composed: make = "BMW" AND price <= 20000
```

**Complex Query (exact + semantic + range):**
```
Query: "Economical BMW under £20k with low mileage"
→ Composed:
  Group 1 (AND):
    - make = "BMW"
    - price <= 20000
    - mileage <= 30000
    - engineSize <= 2.0 (from "economical")
    - fuelType IN ["Electric", "Hybrid"] (from "economical")
```

**Multi-Modal Query (requires different search strategies):**
```
Query: "Reliable BMW 3 series with parking sensors"
→ Composed:
  Exact: make = "BMW", model CONTAINS "3"
  Semantic: "reliable" (embedding-based)
  Feature: features CONTAINS "parking sensors"
```

### Logical Operator Handling

**Default: AND (conjunctive)**
- Most queries assume all criteria must match
- "BMW under £20k" → make = "BMW" AND price <= 20000

**Explicit OR (disjunctive):**
- Detect keywords: "or", "either", "alternatively"
- "BMW or Audi" → make = "BMW" OR make = "Audi"
- "under £20k or low mileage" → price <= 20000 OR mileage <= 30000

**Grouping with parentheses (conceptually):**
```
Query: "BMW or Audi under £20k"
→ (make = "BMW" OR make = "Audi") AND price <= 20000
```

### Constraint Prioritization

**Priority Levels:**

**High Priority (must-have):**
- Explicit exact matches: make, model
- Hard constraints: "exactly", "must have"
- Safety constraints: price range, location

**Medium Priority (should-have):**
- Range filters: price, mileage
- Features
- Dates

**Low Priority (nice-to-have):**
- Qualitative terms: "reliable", "economical"
- Soft preferences: "preferably", "ideally"

**Use Case:**
If query is too restrictive (0 results), relax low-priority constraints first.

### Conflict Detection & Resolution

**Contradictory Constraints:**

**Price Conflicts:**
```
Query: "cheap expensive car"
Conflict: price <= 12000 AND price >= 30000
Resolution: Flag as error, ask user for clarification
```

**Mileage Conflicts:**
```
Query: "low mileage high mileage"
Conflict: mileage <= 30000 AND mileage >= 100000
Resolution: Reject query
```

**Range Inversions:**
```
Constraint: price >= 30000 AND price <= 20000
Resolution: Detect and reject (mathematically impossible)
```

**Overlapping Ranges:**
```
Query: "£15k-£25k under £30k"
→ Use intersection: £15k-£25k (more specific wins)
```

**Resolution Strategies:**
1. **Reject:** Contradictory constraints → return error
2. **Merge:** Overlapping ranges → use intersection
3. **Prefer Explicit:** Explicit values override qualitative
4. **Ask User:** If ambiguous, request clarification

### Azure Search Query Translation

**Translate to OData filter syntax:**

```csharp
// Input: make = "BMW" AND price <= 20000
// Output: "make eq 'BMW' and price le 20000"

public string ToODataFilter(ComposedQuery query)
{
    var filters = new List<string>();
    
    foreach (var group in query.ConstraintGroups)
    {
        var groupFilters = group.Constraints
            .Select(c => ToODataExpression(c))
            .ToList();
        
        var groupFilter = string.Join(
            $" {group.Operator.ToString().ToLower()} ",
            groupFilters
        );
        
        filters.Add($"({groupFilter})");
    }
    
    return string.Join(
        $" {query.GroupOperator.ToString().ToLower()} ",
        filters
    );
}

private string ToODataExpression(SearchConstraint constraint)
{
    return constraint.Operator switch
    {
        ConstraintOperator.Equals => $"{constraint.FieldName} eq '{constraint.Value}'",
        ConstraintOperator.LessThanOrEqual => $"{constraint.FieldName} le {constraint.Value}",
        ConstraintOperator.GreaterThanOrEqual => $"{constraint.FieldName} ge {constraint.Value}",
        ConstraintOperator.Contains => $"search.in({constraint.FieldName}, '{constraint.Value}', ',')",
        ConstraintOperator.Between => 
            $"{constraint.FieldName} ge {constraint.Value[0]} and {constraint.FieldName} le {constraint.Value[1]}",
        _ => throw new NotSupportedException()
    };
}
```

### API Endpoints

**POST /api/v1/query/compose**

Request:
```json
{
  "mappedQuery": {
    "constraints": [
      { "fieldName": "make", "operator": "Equals", "value": "BMW" },
      { "fieldName": "price", "operator": "LessThanOrEqual", "value": 20000 },
      { "fieldName": "mileage", "operator": "LessThanOrEqual", "value": 50000 }
    ]
  }
}
```

Response:
```json
{
  "type": "Filtered",
  "constraintGroups": [
    {
      "constraints": [
        { "fieldName": "make", "operator": "Equals", "value": "BMW" },
        { "fieldName": "price", "operator": "LessThanOrEqual", "value": 20000 },
        { "fieldName": "mileage", "operator": "LessThanOrEqual", "value": 50000 }
      ],
      "operator": "And",
      "priority": 1.0
    }
  ],
  "groupOperator": "And",
  "warnings": [],
  "hasConflicts": false,
  "odataFilter": "make eq 'BMW' and price le 20000 and mileage le 50000"
}
```

---

## Acceptance Criteria

### Functional Criteria

✅ **Simple Queries:**
- [ ] Single constraint queries compose correctly
- [ ] "Show me BMW" → make = "BMW"

✅ **Multi-Constraint Queries:**
- [ ] 2-5 constraints combined with AND
- [ ] "BMW under £20k with leather" works
- [ ] All constraints included

✅ **Logical Operators:**
- [ ] AND is default operator
- [ ] OR detected from keywords
- [ ] Grouping logic works: "(A OR B) AND C"

✅ **Conflict Detection:**
- [ ] Contradictory constraints flagged
- [ ] Range inversions rejected
- [ ] Overlapping ranges merged

✅ **OData Translation:**
- [ ] Composed query converts to valid OData
- [ ] All operators translate correctly
- [ ] Complex queries work with Azure Search

### Technical Criteria

✅ **Performance:**
- [ ] Query composition <200ms
- [ ] Handles 10+ constraints
- [ ] No performance degradation with complexity

✅ **Accuracy:**
- [ ] 100% of valid queries compose correctly
- [ ] 95%+ conflict detection accuracy
- [ ] OData output always valid

---

## Testing Requirements

### Unit Tests

**Test Coverage:** ≥85%

**Test Cases:**
```csharp
[Fact]
public async Task ComposeQuery_SingleConstraint_ReturnsSimpleQuery()

[Fact]
public async Task ComposeQuery_MultipleANDConstraints_ReturnsFilteredQuery()

[Fact]
public async Task ComposeQuery_ORKeyword_UsesOrOperator()

[Fact]
public async Task ValidateQuery_ContradictoryPrice_ReturnsFalse()

[Fact]
public async Task ResolveConflicts_OverlappingRanges_MergesCorrectly()

[Theory]
[InlineData("make eq 'BMW'")]
[InlineData("make eq 'BMW' and price le 20000")]
public async Task ToODataFilter_VariousConstraints_ReturnsValidOData(string expected)

[Fact]
public async Task ComposeQuery_ComplexWithSemanticTerms_CreatesMultiModalQuery()
```

### Integration Tests

**Test Cases:**
- [ ] Compose 20 real queries from FRD
- [ ] Test all query types (simple, filtered, complex, multi-modal)
- [ ] Verify OData filters work with Azure Search
- [ ] Test conflict resolution scenarios

---

## Implementation Notes

### DO:
- ✅ Default to AND operator
- ✅ Detect conflict patterns
- ✅ Generate valid OData syntax
- ✅ Group constraints by priority
- ✅ Log composition decisions
- ✅ Validate output before returning

### DON'T:
- ❌ Skip conflict detection
- ❌ Generate invalid OData
- ❌ Ignore logical operators from query
- ❌ Assume all constraints equal priority

### Edge Cases:
- Empty constraint list → Return error
- Single constraint → Simple query
- All qualitative → Semantic-only query
- Contradictory → Flag and reject

---

## Definition of Done

- [ ] Query composer service implemented
- [ ] All query types supported
- [ ] Logical operators working (AND/OR)
- [ ] Conflict detection functional
- [ ] OData translation working
- [ ] API endpoint functional
- [ ] All unit tests pass (≥85% coverage)
- [ ] Integration tests with Azure Search pass
- [ ] Documentation updated
- [ ] Code reviewed and approved

---

## Related Files

**Created/Modified:**
- `src/VehicleSearch.Infrastructure/AI/QueryComposerService.cs`
- `src/VehicleSearch.Infrastructure/AI/ConflictResolver.cs`
- `src/VehicleSearch.Infrastructure/AI/ODataTranslator.cs`
- `src/VehicleSearch.Core/Interfaces/IQueryComposerService.cs`
- `src/VehicleSearch.Core/Models/ComposedQuery.cs`
- `src/VehicleSearch.Api/Controllers/QueryController.cs`
- `tests/VehicleSearch.Infrastructure.Tests/QueryComposerServiceTests.cs`

**References:**
- FRD-001: Natural Language Query Understanding (FR-5, FR-6)
- Task 007: Parsed queries
- Task 008: Mapped constraints
- Azure AI Search OData syntax documentation
