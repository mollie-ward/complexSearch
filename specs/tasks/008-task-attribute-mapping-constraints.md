# Task: Attribute Mapping & Constraint Parsing

**Task ID:** 008  
**Feature:** Natural Language Query Understanding  
**Type:** Backend Implementation  
**Priority:** High  
**Estimated Complexity:** Medium  
**FRD Reference:** FRD-001 (FR-3, FR-4)

---

## Description

Map extracted entities from natural language to database schema fields and parse constraints (price ranges, mileage limits, comparison operators) into structured search filters.

---

## Dependencies

**Depends on:**
- Task 007: Query Intent Classification & Entity Extraction
- Task 005: Azure AI Search Index Setup (for schema)

**Blocks:**
- Task 009: Multi-Criteria Query Composition
- Task 014: Search Strategy Selection & Orchestration

---

## Technical Requirements

### Attribute Mapper Service Interface

```csharp
public interface IAttributeMapperService
{
    Task<MappedQuery> MapToSearchQueryAsync(ParsedQuery parsedQuery);
    Task<SearchConstraint> ParseConstraintAsync(ExtractedEntity entity);
    Task<IEnumerable<SearchField>> MapEntityToFieldsAsync(ExtractedEntity entity);
}

public class MappedQuery
{
    public List<SearchConstraint> Constraints { get; set; }
    public List<string> UnmappableTerms { get; set; }
    public Dictionary<string, object> Metadata { get; set; }
}

public class SearchConstraint
{
    public string FieldName { get; set; }
    public ConstraintOperator Operator { get; set; }
    public object Value { get; set; }
    public ConstraintType Type { get; set; }
}

public enum ConstraintOperator
{
    Equals,
    NotEquals,
    GreaterThan,
    GreaterThanOrEqual,
    LessThan,
    LessThanOrEqual,
    Between,
    Contains,
    In
}

public enum ConstraintType
{
    Exact,          // Exact match (make, model)
    Range,          // Range filter (price, mileage)
    Semantic,       // Semantic search (qualitative terms)
    Composite       // Multiple fields (body type + doors)
}
```

### Entity-to-Field Mapping

**Make → Make (exact match)**
```csharp
Entity: { Type: Make, Value: "BMW" }
→ Constraint: { FieldName: "make", Operator: Equals, Value: "BMW" }
```

**Model → Model (fuzzy match)**
```csharp
Entity: { Type: Model, Value: "3 series" }
→ Constraint: { FieldName: "model", Operator: Contains, Value: "3" }
```

**Price → Price (with operator)**
```csharp
// "under £20k"
Entity: { Type: Price, Value: "20000" }
→ Constraint: { FieldName: "price", Operator: LessThanOrEqual, Value: 20000 }

// "£15k-£25k"
Entity: { Type: PriceRange, Value: "15000-25000" }
→ Constraint: { FieldName: "price", Operator: Between, Value: [15000, 25000] }

// "around £20k"
Entity: { Type: Price, Value: "20000" }
→ Constraint: { FieldName: "price", Operator: Between, Value: [18000, 22000] }
```

**Mileage → Mileage**
```csharp
// "under 50k miles"
Entity: { Type: Mileage, Value: "50000" }
→ Constraint: { FieldName: "mileage", Operator: LessThan, Value: 50000 }

// "low mileage"
Entity: { Type: QualitativeTerm, Value: "low mileage" }
→ Constraint: { FieldName: "mileage", Operator: LessThanOrEqual, Value: 30000 }
```

**Features → Features (array contains)**
```csharp
Entity: { Type: Feature, Value: "leather" }
→ Constraint: { FieldName: "features", Operator: Contains, Value: "leather" }
```

**Location → SaleLocation**
```csharp
Entity: { Type: Location, Value: "Manchester" }
→ Constraint: { FieldName: "saleLocation", Operator: Equals, Value: "Manchester" }
```

### Constraint Parsing

**Price Constraint Parsing:**
- "under £20k" → price <= 20000
- "less than £20,000" → price < 20000
- "£15k-£25k" → price BETWEEN 15000 AND 25000
- "around £20k" → price BETWEEN 18000 AND 22000 (±10%)
- "cheap" → price <= 15000 (default threshold)
- "expensive" → price >= 30000 (default threshold)

**Mileage Constraint Parsing:**
- "low mileage" → mileage <= 30000
- "high mileage" → mileage >= 100000
- "under 50k miles" → mileage < 50000
- "less than 40,000 miles" → mileage < 40000

**Date/Year Parsing:**
- "recent" → registrationDate >= (today - 3 years)
- "new" → registrationDate >= (today - 1 year)
- "2024 or newer" → registrationDate >= 2024-01-01
- "registered after 2020" → registrationDate > 2020-12-31

**Qualitative Term Defaults:**

```csharp
private static readonly Dictionary<string, SearchConstraint[]> QualitativeDefaults = new()
{
    ["affordable"] = new[] {
        new SearchConstraint { FieldName = "price", Operator = LessThanOrEqual, Value = 15000 }
    },
    ["cheap"] = new[] {
        new SearchConstraint { FieldName = "price", Operator = LessThanOrEqual, Value = 12000 }
    },
    ["economical"] = new[] {
        new SearchConstraint { FieldName = "engineSize", Operator = LessThanOrEqual, Value = 2.0 },
        new SearchConstraint { FieldName = "fuelType", Operator = In, Value = new[] { "Electric", "Hybrid" } }
    },
    ["reliable"] = new[] {
        new SearchConstraint { FieldName = "serviceHistoryPresent", Operator = Equals, Value = true },
        new SearchConstraint { FieldName = "mileage", Operator = LessThanOrEqual, Value = 60000 }
    },
    ["low mileage"] = new[] {
        new SearchConstraint { FieldName = "mileage", Operator = LessThanOrEqual, Value = 30000 }
    },
    ["family car"] = new[] {
        new SearchConstraint { FieldName = "numberOfDoors", Operator = GreaterThanOrEqual, Value = 5 },
        new SearchConstraint { FieldName = "bodyType", Operator = In, Value = new[] { "SUV", "MPV", "Estate", "Hatchback" } }
    }
};
```

### Operator Inference

**Infer operator from context:**
- "under", "less than", "below", "up to" → LessThanOrEqual
- "over", "more than", "above", "at least" → GreaterThanOrEqual
- "between", "from X to Y", "X-Y" → Between
- "exactly", "is", no qualifier → Equals
- "around", "about", "approximately" → Between (±10%)

### Ambiguity Handling

**Ambiguous terms flagged for clarification:**
- "small" → Could be body type OR engine size
- "big" → Could be body type OR engine size OR price
- "new" → Could be recent registration OR low mileage

**Resolution strategy:**
1. Use context from other entities
2. Apply most common interpretation
3. Flag for user clarification if critical

### API Endpoints

**POST /api/v1/query/map**

Request:
```json
{
  "parsedQuery": {
    "intent": "search",
    "entities": [
      { "type": "Make", "value": "BMW" },
      { "type": "Price", "value": "20000" }
    ]
  },
  "context": "under"
}
```

Response:
```json
{
  "constraints": [
    {
      "fieldName": "make",
      "operator": "Equals",
      "value": "BMW",
      "type": "Exact"
    },
    {
      "fieldName": "price",
      "operator": "LessThanOrEqual",
      "value": 20000,
      "type": "Range"
    }
  ],
  "unmappableTerms": [],
  "metadata": {
    "totalConstraints": 2,
    "exactMatches": 1,
    "rangeFilters": 1
  }
}
```

---

## Acceptance Criteria

### Functional Criteria

✅ **Entity Mapping:**
- [ ] Makes map to "make" field
- [ ] Prices map to "price" field with correct operator
- [ ] Mileage maps to "mileage" field
- [ ] Features map to "features" array field
- [ ] All entity types have mapping logic

✅ **Constraint Parsing:**
- [ ] "under £20k" → price <= 20000
- [ ] "£15k-£25k" → price BETWEEN 15000 AND 25000
- [ ] "low mileage" → mileage <= 30000
- [ ] "around £20k" → price BETWEEN 18000 AND 22000

✅ **Operator Inference:**
- [ ] "under" infers LessThanOrEqual
- [ ] "between" infers Between
- [ ] No qualifier defaults to Equals
- [ ] "around" adds ±10% range

✅ **Qualitative Terms:**
- [ ] "economical" maps to engine size + fuel type
- [ ] "reliable" maps to service history + mileage
- [ ] "family car" maps to doors + body type
- [ ] All qualitative defaults work

### Technical Criteria

✅ **Performance:**
- [ ] Mapping completes in <100ms
- [ ] Handles 10+ entities per query
- [ ] No memory leaks

✅ **Accuracy:**
- [ ] 95%+ correct field mapping
- [ ] 90%+ correct operator inference
- [ ] Qualitative defaults reasonable

---

## Testing Requirements

### Unit Tests

**Test Coverage:** ≥85%

**Test Cases:**
```csharp
[Theory]
[InlineData("BMW", "make", ConstraintOperator.Equals)]
[InlineData("20000", "price", ConstraintOperator.LessThanOrEqual)]
public async Task MapEntity_VariousTypes_MapsCorrectly(string value, string field, ConstraintOperator op)

[Fact]
public async Task ParsePrice_Under20k_CreatesLessThanConstraint()

[Fact]
public async Task ParsePrice_Range15to25k_CreatesBetweenConstraint()

[Fact]
public async Task ParseMileage_LowMileage_Uses30kDefault()

[Fact]
public async Task MapQualitativeTerm_Economical_CreatesMultipleConstraints()

[Fact]
public async Task InferOperator_UnderKeyword_ReturnsLessThanOrEqual()
```

### Integration Tests

**Test Cases:**
- [ ] Map complex query with 5+ entities
- [ ] Test all qualitative defaults
- [ ] Verify constraints translate to Azure Search filters
- [ ] Test edge cases (ambiguous terms)

---

## Implementation Notes

### DO:
- ✅ Use dictionary for entity-to-field mapping
- ✅ Implement operator inference logic
- ✅ Provide reasonable defaults for qualitative terms
- ✅ Flag ambiguous terms for clarification
- ✅ Log all mapping decisions
- ✅ Make defaults configurable

### DON'T:
- ❌ Hardcode all qualitative interpretations
- ❌ Ignore operator context keywords
- ❌ Skip ambiguity detection
- ❌ Make assumptions without logging

---

## Definition of Done

- [ ] Attribute mapper service implemented
- [ ] All entity types map to fields
- [ ] Constraint parsing working
- [ ] Operator inference functional
- [ ] Qualitative defaults configured
- [ ] API endpoint functional
- [ ] All unit tests pass (≥85% coverage)
- [ ] Integration tests pass
- [ ] Documentation updated
- [ ] Code reviewed and approved

---

## Related Files

**Created/Modified:**
- `src/VehicleSearch.Infrastructure/AI/AttributeMapperService.cs`
- `src/VehicleSearch.Infrastructure/AI/ConstraintParser.cs`
- `src/VehicleSearch.Core/Interfaces/IAttributeMapperService.cs`
- `src/VehicleSearch.Core/Models/SearchConstraint.cs`
- `src/VehicleSearch.Api/Controllers/QueryController.cs`
- `tests/VehicleSearch.Infrastructure.Tests/AttributeMapperServiceTests.cs`

**References:**
- FRD-001: Natural Language Query Understanding (FR-3, FR-4)
- Task 007: Parsed query entities
- Task 005: Search schema fields
