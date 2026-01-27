# Feature Requirements Document (FRD)

## Feature: Natural Language Query Understanding

**Feature ID:** FRD-001  
**PRD Reference:** REQ-1  
**Version:** 1.0  
**Status:** Draft  
**Created:** January 27, 2026

---

## 1. Feature Overview

### Purpose
Process and interpret user queries expressed in conversational English, extracting search intent and vehicle attributes without requiring users to know database field names or use specific syntax.

### Business Value
- Reduces barrier to entry for non-technical users
- Eliminates need for user training on search syntax
- Increases search success rate through intent understanding
- Enables natural, intuitive interaction with inventory data

### User Impact
Users can ask questions like "find me a reliable family car under £20,000" instead of constructing complex filters like "body_type=SUV AND price<=20000 AND doors>=5".

---

## 2. Functional Requirements

### FR-1: Query Intent Classification
**What:** The system must identify the user's primary search intent.

**Capabilities:**
- Classify query type (vehicle search, comparison, information request, refinement)
- Distinguish between new searches vs. follow-up queries
- Identify question patterns ("What...", "Show me...", "Find...", "How many...")
- Recognize negative intent (out-of-scope requests)

**Acceptance Criteria:**
- Search queries correctly identified (90%+ accuracy)
- Comparison queries ("compare X vs Y") recognized
- Follow-up queries ("cheaper ones", "what about manual") detected
- Out-of-scope queries flagged for safety handling

### FR-2: Entity Extraction
**What:** The system must extract vehicle-related entities from natural language.

**Capabilities:**
- Extract vehicle makes ("BMW", "Audi", "Alfa Romeo")
- Extract models and derivatives ("Q3", "A1 Sportback", "Stelvio")
- Extract specifications (engine size, fuel type, transmission, body type)
- Extract numeric constraints (price ranges, mileage limits, year ranges)
- Extract features ("leather seats", "parking sensors", "navigation")
- Extract locations ("Manchester", "Leeds", "North")
- Extract qualitative terms ("reliable", "economical", "family car", "sporty")

**Acceptance Criteria:**
- Vehicle makes extracted with 95%+ accuracy
- Numeric values (prices, mileage) extracted correctly
- Multi-word entities preserved ("parking sensors", "manual transmission")
- Synonyms recognized (e.g., "nav" → "navigation", "auto" → "automatic")
- Case-insensitive matching works

### FR-3: Attribute Mapping
**What:** The system must map extracted entities to knowledge base fields.

**Capabilities:**
- Map makes to "Make" field
- Map price references to "Buy Now Price" field
- Map mileage references to "Mileage" field
- Map features to "Equipment" field
- Map locations to "Sale Location" field
- Map qualitative terms to relevant attributes ("economical" → "Fuel" + "Engine Size")
- Handle ambiguous terms ("cheap" → price constraint, "small" → body type or engine size)

**Acceptance Criteria:**
- Exact field names not required from user
- Conversational terms mapped correctly ("miles driven" → Mileage)
- Multiple interpretations handled (system clarifies if needed)
- Unmappable terms don't break the query

### FR-4: Constraint Interpretation
**What:** The system must interpret numeric and logical constraints from natural language.

**Capabilities:**
- Parse price ranges ("under £20k", "between £15-25k", "around £30,000")
- Parse mileage constraints ("low mileage", "less than 50k miles", "under 30,000")
- Parse date/age constraints ("recent", "2024 or newer", "registered after 2020")
- Parse comparison operators ("less than", "more than", "at least", "up to")
- Handle imprecise terms ("affordable", "high mileage", "new-ish")

**Acceptance Criteria:**
- "Under £20k" → price <= 20000
- "Between £15-25k" → price >= 15000 AND price <= 25000
- "Low mileage" → mileage <= 30000 (reasonable threshold)
- "Recent" → registration_date >= 2023 (last 3 years)
- Ambiguous terms use sensible defaults and communicate assumptions

### FR-5: Multi-Criteria Parsing
**What:** The system must handle queries with multiple simultaneous constraints.

**Capabilities:**
- Extract multiple attributes from single query
- Preserve relationships between constraints (AND logic)
- Handle optional preferences vs. requirements
- Parse ordered lists of criteria
- Maintain constraint priority

**Acceptance Criteria:**
- "BMW under £25k with less than 50k miles in Manchester" extracts:
  - Make: BMW
  - Price: <= 25000
  - Mileage: < 50000
  - Location: Manchester
- All constraints applied simultaneously
- No constraint lost or ignored

### FR-6: Context-Aware Parsing
**What:** The system must interpret queries in the context of conversation history.

**Capabilities:**
- Resolve pronouns and references ("show me cheaper ones" → same make/model, lower price)
- Interpret comparative terms ("bigger", "newer", "more expensive")
- Maintain constraint persistence across refinements
- Detect constraint modifications vs. additions

**Acceptance Criteria:**
- "Show me cheaper ones" correctly references previous results
- "What about electric?" adds fuel constraint while keeping other criteria
- "Bigger" interpreted relative to previous body type
- Context reset on new topic detection

---

## 3. Query Understanding Scope

### Supported Query Patterns:
- **Direct requests:** "Find BMW 3 series"
- **Price-focused:** "Cars under £15,000"
- **Feature-focused:** "SUVs with leather seats"
- **Multi-criteria:** "Electric cars under £30k with low mileage"
- **Lifestyle/intent:** "Good family car", "reliable commuter", "sporty convertible"
- **Location-based:** "Vehicles in Leeds", "auctions near Manchester"
- **Refinements:** "Cheaper ones", "What about automatic?", "Show me diesel"

### Supported Attributes:
- **Vehicle Identity:** Make, Model, Derivative, Body Type
- **Specifications:** Engine Size, Fuel, Transmission, Doors, Colour
- **Pricing:** Buy Now Price, Price Ranges
- **Condition:** Mileage, Service History, Grade
- **Features:** Equipment items (navigation, sensors, leather, etc.)
- **Location:** Sale Location, Region
- **Availability:** Channel (auction vs. buy now), VAT Type

### Qualitative Terms Interpretation:
- **"Economical"** → Low engine size (<= 2.0L) OR Electric/Hybrid
- **"Family car"** → 5 doors, SUV/Hatchback/MPV, 5+ seats
- **"Reliable"** → Full service history, lower mileage, mainstream makes
- **"Sporty"** → Coupe/Convertible OR performance derivatives (M Sport, RS, etc.)
- **"Practical"** → 4-5 doors, hatchback/estate/SUV, good boot space
- **"Affordable"** → Below median price (context-dependent)

---

## 4. Inputs & Outputs

### Inputs:
- **User Query:** Natural language text string (English)
- **Conversation Context:** Previous queries and results (optional)
- **User Preferences:** Saved filters or defaults (optional)

### Outputs:
- **Parsed Query Object:**
  - Intent: search | compare | refine | information
  - Entities: List of extracted terms
  - Constraints: Field-value pairs with operators
  - Qualitative Terms: Interpreted meanings
  - Confidence Score: 0-1 indicating parsing certainty
- **Ambiguity Flags:** Terms needing clarification
- **Unmapped Terms:** Words not recognized

---

## 5. Dependencies

### Depends On:
- Knowledge base schema (from FRD-005)
- Vehicle domain vocabulary and synonyms

### Depended On By:
- Feature 2: Semantic Search Engine (uses parsed query)
- Feature 3: Conversational Context Management (uses intent classification)
- Feature 4: Hybrid Search Orchestration (uses constraints)
- Feature 6: Safety Guardrails (uses intent for validation)

---

## 6. Acceptance Criteria

### Scenario 1: Simple Make/Model Query
**Given:** User query "Show me BMW 3 series"  
**When:** Query is parsed  
**Then:**
- Intent: search
- Make: "BMW"
- Model: "3" or "3 series" (fuzzy matched)
- No other constraints
- Confidence: >0.9

### Scenario 2: Price Range Query
**Given:** User query "Cars under £20,000"  
**When:** Query is parsed  
**Then:**
- Intent: search
- Price constraint: <= 20000
- No make/model specified
- Confidence: >0.8

### Scenario 3: Multi-Criteria Query
**Given:** User query "Electric BMW with under 30,000 miles in the North"  
**When:** Query is parsed  
**Then:**
- Intent: search
- Make: "BMW"
- Fuel: "Electric"
- Mileage: < 30000
- Location: Northern regions (Leeds, Newcastle, Manchester, etc.)
- All constraints preserved
- Confidence: >0.85

### Scenario 4: Lifestyle Intent Query
**Given:** User query "I need a reliable family car"  
**When:** Query is parsed  
**Then:**
- Intent: search
- Qualitative terms: ["reliable", "family car"]
- Interpreted constraints:
  - Doors: >= 5 OR Body: SUV/MPV/Estate
  - Service History: Present
- Ambiguity flagged for user confirmation

### Scenario 5: Refinement Query
**Given:** Previous query was "Show me Audi SUVs" (returned Q3, Q5, Q7)  
**And:** User query is "Under £20,000"  
**When:** Query is parsed  
**Then:**
- Intent: refine
- Previous constraints preserved (Make: Audi, Body: SUV)
- New constraint added (Price: <= 20000)
- Context reference detected

### Scenario 6: Ambiguous Query
**Given:** User query "Small cars"  
**When:** Query is parsed  
**Then:**
- Intent: search
- Ambiguity detected: "small" could mean engine size or body type
- Multiple interpretations provided:
  - Body: Hatchback/Coupe (small physical size)
  - Engine Size: <= 1.5L
- System requests clarification or returns both interpretations

---

## 7. Non-Functional Requirements

### Performance:
- Parse queries in <500ms
- Support 1000 concurrent parsing requests
- Cache common query patterns for faster processing

### Accuracy:
- 90%+ intent classification accuracy
- 95%+ entity extraction accuracy for makes/models
- 85%+ accuracy for complex multi-criteria queries
- 80%+ accuracy for qualitative term interpretation

### Language Support:
- English language only (UK/US variants)
- Handle common misspellings ("auddi" → "Audi")
- Support abbreviations ("auto" → "automatic", "nav" → "navigation")

### Robustness:
- Gracefully handle typos and grammar errors
- Work with incomplete sentences
- Handle very short queries (2-3 words)
- Handle very long queries (50+ words)

---

## 8. Out of Scope

- Multi-language support (non-English queries)
- Voice input processing or speech-to-text
- Image-based queries or visual search
- Learning individual user vocabulary over time
- Sentiment analysis or emotional tone detection
- Query spelling correction (basic typo handling only)

---

## 9. Edge Cases & Error Handling

### Edge Case 1: Contradictory Constraints
**Query:** "Show me diesel electric cars"  
**Handling:** Detect contradiction, request clarification

### Edge Case 2: Unrealistic Constraints
**Query:** "BMW under £500"  
**Handling:** Process query, return 0 results, suggest reasonable price range

### Edge Case 3: Very Vague Query
**Query:** "Find me a car"  
**Handling:** Accept query, return popular/featured vehicles, suggest refinement

### Edge Case 4: Over-Specified Query
**Query:** "Red 2022 BMW 320i M Sport with 15,234 miles, leather, nav, sensors in Leeds for £18,250"  
**Handling:** Extract all constraints, likely return 0 results, suggest relaxing criteria

### Edge Case 5: Off-Topic Query
**Query:** "What's the weather today?"  
**Handling:** Flag as out-of-scope, route to safety guardrails (FRD-006)

---

## 10. Open Questions

1. **Clarification Strategy:** When ambiguous, should system ask for clarification or show multiple interpretations?
2. **Default Values:** What defaults for "affordable", "low mileage", "recent"?
3. **Synonym Expansion:** How comprehensive should synonym list be?
4. **Regional Variations:** Should "estate" (UK) and "wagon" (US) both be recognized?
5. **Confidence Threshold:** At what confidence level should system warn users about low certainty?

---

## 11. Success Metrics

- **Parse Success Rate:** 95%+ of queries successfully parsed
- **Intent Accuracy:** 90%+ correct intent classification
- **Entity Extraction:** 95%+ for makes/models, 85%+ for features
- **Multi-Criteria:** 85%+ of multi-attribute queries fully captured
- **User Satisfaction:** <5% of users report misunderstood queries

---

## 12. Examples

| User Query | Parsed Intent | Extracted Constraints |
|------------|---------------|----------------------|
| "BMW under £25k" | search | Make=BMW, Price<=25000 |
| "Electric cars with low mileage" | search | Fuel=Electric, Mileage<=30000 |
| "Show me cheaper ones" | refine | Price<[previous_min_price] |
| "Audi Q3 or Q5" | search | Make=Audi, Model IN [Q3, Q5] |
| "Family SUV around £20,000" | search | Body=SUV, Doors>=5, Price≈20000 |
| "Reliable diesel estate" | search | Fuel=Diesel, Body=Estate, ServiceHistory=Present |
