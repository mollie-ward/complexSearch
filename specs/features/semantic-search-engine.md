# Feature Requirements Document (FRD)

## Feature: Semantic Search Engine

**Feature ID:** FRD-002  
**PRD Reference:** REQ-2  
**Version:** 1.0  
**Status:** Draft  
**Created:** January 27, 2026

---

## 1. Feature Overview

### Purpose
Find relevant vehicles based on meaning and intent rather than exact keyword matching, enabling users to discover vehicles that match their conceptual needs even when specific terminology doesn't appear in the data.

### Business Value
- Increases search recall (finds more relevant results)
- Reduces zero-result searches
- Enables discovery based on concepts and synonyms
- Improves user satisfaction with result relevance

### User Impact
Users searching for "economical cars" will find fuel-efficient vehicles even if the word "economical" never appears in the data. Searches for "family vehicle" return SUVs, MPVs, and estates without requiring exact phrase matches.

---

## 2. Functional Requirements

### FR-1: Semantic Query Embedding
**What:** The system must convert user queries into semantic vector representations.

**Capabilities:**
- Transform natural language queries into numerical vectors
- Capture semantic meaning beyond keywords
- Preserve intent across paraphrasing ("cheap car" ≈ "affordable vehicle")
- Handle multi-part queries (combine concepts coherently)
- Generate embeddings in <100ms per query

**Acceptance Criteria:**
- Semantically similar queries produce similar embeddings
- "Family car" and "family vehicle" have >0.9 similarity
- "Economical" and "fuel efficient" cluster closely
- Query embeddings compatible with vehicle embeddings

### FR-2: Vehicle Content Vectorization
**What:** The system must create semantic vector representations of vehicle records.

**Capabilities:**
- Embed vehicle descriptions combining multiple attributes
- Include make, model, features, body type in semantic representation
- Capture vehicle "profile" holistically
- Weight important attributes appropriately
- Support incremental embedding of new vehicles

**Acceptance Criteria:**
- Similar vehicles (same make/model/class) have high embedding similarity
- Electric vehicles cluster together semantically
- Luxury features create distinguishable semantic patterns
- All vehicles in knowledge base embedded and indexed

### FR-3: Semantic Similarity Matching
**What:** The system must find vehicles semantically similar to user queries.

**Capabilities:**
- Calculate similarity between query and vehicle embeddings
- Rank results by semantic relevance
- Return top-k most similar vehicles
- Handle concept matching ("sporty" → M Sport, RS models, coupes)
- Support synonym expansion ("auto" → "automatic", "nav" → "navigation")

**Acceptance Criteria:**
- "Economical cars" returns small engines, hybrids, electrics
- "Family SUV" returns Q3, Q5, Q7, not sports cars
- "Luxury sedan" returns higher-trim saloons with leather/nav
- Results ranked by relevance score
- Minimum similarity threshold filters irrelevant matches

### FR-4: Conceptual Query Understanding
**What:** The system must interpret abstract concepts and lifestyle needs.

**Capabilities:**
- Map "family car" → 5 doors, SUV/MPV, practical features
- Map "sporty" → performance variants, coupes, M Sport
- Map "reliable" → service history, mainstream brands, lower mileage
- Map "economical" → fuel efficiency, small engines, low running costs
- Map "practical" → boot space, body types, door count

**Acceptance Criteria:**
- "Good first car" returns affordable, small, economical vehicles
- "Executive car" returns premium saloons (BMW 5/7 series, Audi A6)
- "Weekend car" returns convertibles, sports cars, fun vehicles
- Concept-to-attribute mapping explicit and explainable

### FR-5: Contextual Relevance Scoring
**What:** The system must score result relevance based on multiple factors.

**Capabilities:**
- Combine semantic similarity with structured attribute matches
- Boost results matching exact criteria (make, price range)
- Penalize weak semantic matches
- Consider recency/popularity when appropriate
- Normalize scores to 0-1 range

**Acceptance Criteria:**
- Exact make match gets higher score than semantic near-match
- Price within range boosts relevance
- Feature overlap increases score
- Zero semantic similarity returns score <0.3
- Top result always most relevant to query intent

### FR-6: Synonym and Variant Handling
**What:** The system must recognize synonyms and term variations.

**Capabilities:**
- Recognize "automatic" = "auto" = "automatic transmission"
- Recognize "navigation" = "nav" = "sat nav" = "GPS"
- Recognize "leather" = "leather seats" = "leather trim"
- Recognize UK/US variants ("estate" = "wagon", "boot" = "trunk")
- Handle brand variations ("Beamer" → "BMW")

**Acceptance Criteria:**
- "Auto transmission" matches vehicles with "Automatic"
- "Sat nav" finds vehicles with "Navigation System" or "Navigation HDD"
- "Parking sensors" matches "Parking Sensor(s)"
- Case-insensitive matching throughout

---

## 3. Semantic Search Scope

### Supported Semantic Concepts:

**Vehicle Classes:**
- Family car → 5 doors, SUV/Hatchback/MPV, practical
- Executive car → Premium saloon, luxury features
- City car → Small, economical, easy to park
- Sports car → Performance, coupe, high power
- Workhorse → Estate, high mileage tolerance, durable

**Attributes:**
- Economical → Low fuel consumption, small engine, hybrid/electric
- Reliable → Service history, mainstream brand, lower mileage
- Spacious → SUV/Estate/MPV, 5+ doors
- Luxurious → Leather, premium brand, high trim
- Practical → Good boot, 4-5 doors, flexible seating

**Features:**
- Tech features → Navigation, parking sensors, cameras
- Comfort → Leather, climate control, heated seats
- Safety → Parking sensors, cameras, modern (recent year)
- Convenience → Automatic transmission, keyless entry

### Synonym Mappings:
- **Transmission:** auto, automatic, autobox, tiptronic, DSG, DCT
- **Navigation:** nav, GPS, sat nav, satnav, maps
- **Parking:** parking assist, park assist, sensors, PDC
- **Leather:** leather seats, leather interior, leather trim
- **Body Types:** SUV/crossover, estate/wagon, saloon/sedan

---

## 4. Inputs & Outputs

### Inputs:
- **Parsed Query:** From FRD-001 (entities, constraints, qualitative terms)
- **Vehicle Embeddings:** From knowledge base (FRD-005)
- **Search Parameters:** Result count, minimum similarity threshold

### Outputs:
- **Ranked Results:** List of vehicles ordered by semantic relevance
- **Relevance Scores:** 0-1 score for each result
- **Match Explanation:** Why each vehicle matched (which concepts/features)
- **Confidence Score:** Overall confidence in result set quality

---

## 5. Dependencies

### Depends On:
- Feature 1: Natural Language Query Understanding (provides parsed queries)
- Feature 5: Knowledge Base Integration (provides vehicle data and embeddings)

### Depended On By:
- Feature 4: Hybrid Search Orchestration (combines semantic with exact matching)

---

## 6. Acceptance Criteria

### Scenario 1: Conceptual Search
**Given:** User query "Find me a good family car"  
**When:** Semantic search is performed  
**Then:**
- Returns SUVs, MPVs, 5-door hatchbacks
- Prioritizes vehicles with safety features, space
- Does NOT return 2-door coupes or sports cars
- Relevance scores >0.6 for all results
- Top result clearly family-oriented

### Scenario 2: Synonym Matching
**Given:** User query "Cars with sat nav"  
**When:** Semantic search is performed  
**Then:**
- Matches "Navigation System", "Navigation HDD", "Navigation SD Card"
- Does NOT require exact phrase "sat nav" in data
- Returns vehicles with any navigation equipment
- Match explanation shows "navigation" feature overlap

### Scenario 3: Lifestyle Intent
**Given:** User query "Economical car for commuting"  
**When:** Semantic search is performed  
**Then:**
- Returns small engines (≤1.5L) or hybrid/electric
- Prioritizes fuel-efficient vehicles
- Good mileage tolerance (not ultra-low mileage required)
- Practical body types (hatchback, saloon)
- Relevance based on combined "economical" + "commuting" concepts

### Scenario 4: Abstract Concept
**Given:** User query "Something sporty but practical"  
**When:** Semantic search is performed  
**Then:**
- Balances sporty attributes (M Sport, performance) with practical (4-5 doors)
- Returns hot hatches, sporty SUVs (Q3 S-Line, BMW X3 M Sport)
- Does NOT return pure sports cars (2-door coupes) or pure family SUVs
- Explanation shows balance of both concepts

### Scenario 5: Negative Results
**Given:** User query "Flying cars with time travel"  
**When:** Semantic search is performed  
**Then:**
- Returns 0 results (no semantic match)
- Confidence score <0.2
- Suggests alternative queries
- Does NOT return random vehicles

---

## 7. Non-Functional Requirements

### Performance:
- Query embedding generation: <100ms
- Semantic search across 10,000 vehicles: <2 seconds
- Result ranking: <500ms
- Total semantic search pipeline: <3 seconds

### Accuracy:
- 80%+ of semantic searches return relevant results
- Top-3 results relevant in 70%+ of searches
- Concept interpretation accuracy: 75%+
- Zero false positives for clearly irrelevant queries

### Scalability:
- Support 100,000 vehicle embeddings
- Handle 1000 concurrent semantic searches
- Embedding index update in <10 minutes

### Quality:
- Explainable results (users understand why vehicles matched)
- Consistent results for semantically equivalent queries
- Graceful degradation for ambiguous queries

---

## 8. Out of Scope

- Learning from user behavior or click-through rates
- Personalized semantic understanding per user
- Image-based semantic search
- Multi-language semantic search
- Real-time embedding model updates
- Custom user-defined concepts

---

## 9. Edge Cases & Error Handling

### Edge Case 1: Very Generic Query
**Query:** "Car"  
**Handling:** Return popular/featured vehicles, low confidence, suggest refinement

### Edge Case 2: Contradictory Concepts
**Query:** "Cheap luxury car"  
**Handling:** Balance both concepts, return lower-priced premium vehicles, explain trade-off

### Edge Case 3: Niche Concept
**Query:** "Diesel convertible 7-seater"  
**Handling:** Likely 0 results, relax constraints incrementally, explain what's available

### Edge Case 4: Out-of-Domain Concept
**Query:** "Spaceship"  
**Handling:** No semantic match, route to safety guardrails, suggest vehicle-related query

---

## 10. Open Questions

1. **Embedding Model:** Which pre-trained model provides best vehicle domain performance?
2. **Threshold Tuning:** What minimum similarity score ensures quality results?
3. **Concept Weights:** Should "economical" weight fuel type more than engine size?
4. **Update Frequency:** How often should vehicle embeddings be regenerated?
5. **Result Count:** How many semantic results to return (top-k)?

---

## 11. Success Metrics

- **Search Recall:** 80%+ of conceptual queries return ≥3 relevant results
- **Top-3 Relevance:** 70%+ of searches have relevant vehicle in top 3
- **Zero-Result Rate:** <10% of semantic searches return 0 results
- **User Satisfaction:** 4+ stars for semantic result relevance
- **Performance:** 95%+ of searches complete in <3 seconds

---

## 12. Conceptual Query Examples

| User Query | Semantic Interpretation | Expected Matches |
|------------|------------------------|------------------|
| "Economical city car" | Small, fuel-efficient, easy to park | A1, i3, small hatchbacks <1.5L |
| "Family SUV" | 5+ doors, spacious, practical | Q3, Q5, Q7, X3 |
| "Reliable commuter" | Service history, economical, mainstream | Audi A3, BMW 3-series, fuel-efficient |
| "Executive sedan" | Premium saloon, luxury features | A6, 5-series, 7-series |
| "Fun weekend car" | Sporty, convertible/coupe, performance | i8, 218D coupe, M Sport models |
| "Tech-heavy car" | Navigation, sensors, cameras, modern | Vehicles with extensive equipment lists |
