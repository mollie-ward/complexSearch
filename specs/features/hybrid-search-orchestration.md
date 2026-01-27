# Feature Requirements Document (FRD)

## Feature: Hybrid Search Orchestration

**Feature ID:** FRD-004  
**PRD Reference:** REQ-4, REQ-5, REQ-8  
**Version:** 1.0  
**Status:** Draft  
**Created:** January 27, 2026

---

## 1. Feature Overview

### Purpose
Combine semantic search (meaning-based) with exact structured search (filters/ranges) to deliver optimal results that balance intent understanding with precise data matching, while presenting results in clear, explainable format.

### Business Value
- Maximizes search accuracy through complementary techniques
- Handles both vague ("family car") and precise ("£15k-£20k") queries
- Reduces false positives through multi-signal validation
- Provides best-of-both-worlds search experience

### User Impact
Users get accurate results whether they search with vague concepts ("reliable commuter") or precise specs ("BMW 320d under £18k with <40k miles"). The system intelligently weighs semantic understanding against hard constraints.

---

## 2. Functional Requirements

### FR-1: Search Strategy Selection
**What:** The system must determine optimal search strategy based on query characteristics.

**Capabilities:**
- Detect queries requiring semantic-only search (conceptual, vague)
- Detect queries requiring exact-match-only search (specific make/model/price)
- Detect queries requiring hybrid approach (mix of both)
- Weight semantic vs. exact based on query specificity
- Adapt strategy based on result quality

**Acceptance Criteria:**
- "Family car" → Primarily semantic
- "BMW 320d" → Primarily exact match
- "Reliable BMW under £20k" → Hybrid (semantic "reliable" + exact "BMW" + range "£20k")
- Strategy selection happens in <100ms
- Strategy logged and explainable

### FR-2: Exact Match Filtering
**What:** The system must apply precise filters for structured attributes.

**Capabilities:**
- Exact make/model matching
- Numeric range filtering (price, mileage, year)
- Categorical exact matching (fuel type, transmission, body type)
- Multi-value matching (features: must have X AND Y)
- Location filtering (exact location or region)

**Acceptance Criteria:**
- "BMW" matches only Make=BMW (not Audi with "BMW" in description)
- "Under £20k" filters Price <= 20000 exactly
- "Automatic" matches only Transmission=Automatic
- Multiple filters apply as AND logic
- Zero false positives on exact criteria

### FR-3: Semantic Boosting
**What:** The system must use semantic understanding to boost relevant results.

**Capabilities:**
- Apply semantic similarity scores to ranking
- Boost results matching conceptual intent
- Promote vehicles with feature clusters (luxury, tech, safety)
- Consider lifestyle fit ("family", "sporty", "economical")
- Downrank poor semantic matches even if they pass filters

**Acceptance Criteria:**
- "Reliable car" boosts vehicles with service history
- "Luxury sedan" boosts premium brands and leather interiors
- "Tech-heavy" boosts navigation, sensors, cameras
- Semantic boost applied after exact filters
- Boost transparent in result explanations

### FR-4: Result Ranking & Fusion
**What:** The system must combine semantic and exact signals into unified ranking.

**Capabilities:**
- Fuse semantic similarity scores with exact match scores
- Weight exact matches higher than semantic for critical attributes (price, make)
- Apply multi-factor ranking (relevance, price, mileage, recency)
- Normalize scores across different search types
- Support configurable ranking weights

**Acceptance Criteria:**
- Exact make match gets higher rank than semantic near-match
- Price exact match within range ranked higher
- Combined score = (w1 × exact_score) + (w2 × semantic_score)
- Top results most relevant to user intent
- Ranking explainable (why result #1 over #2)

### FR-5: Multi-Criteria Complex Queries
**What:** The system must handle queries with 3+ simultaneous constraints.

**Capabilities:**
- Process queries like "BMW under £25k with <50k miles in Manchester"
- Apply all constraints simultaneously (AND logic)
- Handle range constraints + exact matches + semantic concepts
- Maintain performance with 5+ criteria
- Gracefully handle over-constrained queries (0 results)

**Acceptance Criteria:**
- All constraints extracted and applied
- Results match ALL criteria (not just some)
- Performance <3 seconds even with 5+ constraints
- Zero results handled gracefully with relaxation suggestions
- Constraint priority clear (which are hard vs. soft)

### FR-6: Result Presentation & Explanation
**What:** The system must present results in clear, structured format with explanations.

**Capabilities:**
- Display key vehicle attributes prominently (make, model, price, mileage)
- Highlight features matching query ("✓ Parking Sensors")
- Explain why each result matched ("Matches your 'family car' criteria: 5 doors, SUV body type")
- Show relevance score or confidence
- Group similar results when appropriate

**Acceptance Criteria:**
- Each result shows: Make, Model, Price, Mileage, Location, Key Features
- Match explanation provided for each result
- Matched query terms highlighted
- Results formatted consistently
- Mobile and desktop friendly presentation

### FR-7: Zero Results Handling
**What:** The system must gracefully handle searches with no matching results.

**Capabilities:**
- Detect zero results condition
- Identify over-constrained criteria (which filter eliminated all results)
- Suggest constraint relaxation ("Try increasing budget to £25k")
- Show near-miss results (relaxed by one constraint)
- Offer alternative queries

**Acceptance Criteria:**
- "0 results found" message clear and helpful
- Specific constraint causing zero results identified
- Relaxation suggestions actionable
- Near-miss results shown if available
- User guided toward successful search

---

## 3. Search Orchestration Logic

### Hybrid Search Pipeline:

```
1. Query Analysis (from FRD-001)
   ↓
2. Strategy Selection
   - Semantic weight: 0-1
   - Exact match weight: 0-1
   ↓
3. Parallel Search Execution
   ├─ Exact Match Filter (structured attributes)
   └─ Semantic Search (conceptual matching)
   ↓
4. Result Fusion
   - Combine results
   - Remove duplicates
   - Apply ranking formula
   ↓
5. Post-Processing
   - Top-k selection
   - Explanation generation
   - Formatting
   ↓
6. Result Presentation
```

### Ranking Formula:
```
final_score = (w_exact × exact_match_score) +
              (w_semantic × semantic_similarity) +
              (w_features × feature_overlap) +
              (w_recency × recency_score) +
              (w_popularity × popularity_score)

Where:
- w_exact = 0.4 (highest weight for hard constraints)
- w_semantic = 0.3 (important for conceptual queries)
- w_features = 0.15 (feature matching matters)
- w_recency = 0.1 (newer vehicles slightly preferred)
- w_popularity = 0.05 (popular models slightly boosted)
```

---

## 4. Query Type Strategies

### Strategy 1: Semantic-Heavy (w_semantic = 0.7, w_exact = 0.3)
**Use Cases:** Vague, conceptual queries  
**Examples:** "Family car", "Reliable commuter", "Fun weekend car"  
**Approach:** Semantic search primary, exact filters secondary

### Strategy 2: Exact-Heavy (w_semantic = 0.2, w_exact = 0.8)
**Use Cases:** Specific, precise queries  
**Examples:** "BMW 320d", "Cars exactly £15,000", "2024 Audi A3"  
**Approach:** Exact filtering primary, semantic for ranking only

### Strategy 3: Balanced Hybrid (w_semantic = 0.5, w_exact = 0.5)
**Use Cases:** Mixed queries  
**Examples:** "Reliable BMW under £20k", "Economical automatic hatchback"  
**Approach:** Equal weight to both signals

---

## 5. Inputs & Outputs

### Inputs:
- **Parsed Query:** From FRD-001 (intent, entities, constraints)
- **Semantic Results:** From FRD-002 (ranked by similarity)
- **Conversation Context:** From FRD-003 (composite constraints)
- **Search Parameters:** Result limit, filters, weights

### Outputs:
- **Ranked Results:** Top-k vehicles ordered by relevance
- **Result Metadata:** For each vehicle:
  - Relevance score (0-1)
  - Match explanation
  - Highlighted features
  - Why it matched query
- **Search Metadata:**
  - Total results found
  - Strategy used
  - Constraints applied
  - Performance metrics

---

## 6. Dependencies

### Depends On:
- Feature 1: Natural Language Query Understanding (provides parsed queries)
- Feature 2: Semantic Search Engine (provides semantic results)
- Feature 3: Conversational Context Management (provides composite queries)
- Feature 5: Knowledge Base Integration (provides vehicle data)

### Depended On By:
- None (this is the final orchestration layer)

---

## 7. Acceptance Criteria

### Scenario 1: Hybrid Query
**Given:** User query "Reliable BMW under £20,000"  
**When:** Hybrid search executes  
**Then:**
- Exact filter: Make=BMW AND Price<=20000
- Semantic boost: Vehicles with service history, lower mileage
- Results ranked by combination of exact match + reliability signals
- Top results are BMWs under £20k with service history
- Explanation: "BMW 320d - Matches: BMW ✓, Price £18,500 ✓, Full service history (reliable)"

### Scenario 2: Purely Semantic Query
**Given:** User query "Good first car"  
**When:** Semantic-heavy search executes  
**Then:**
- No exact filters applied
- Semantic matching for: small, affordable, economical, easy to drive
- Results include A1, i3, small hatchbacks
- Explanation: "Audi A1 - Good first car: Small size, economical 1.0L engine, affordable £15k"

### Scenario 3: Purely Exact Query
**Given:** User query "2022 BMW 320d automatic"  
**When:** Exact-heavy search executes  
**Then:**
- Exact filters: Make=BMW, Model=320d, Year=2022, Transmission=Automatic
- Minimal semantic boosting
- All results match exact criteria
- Explanation: "BMW 320d - Exact match: 2022 ✓, BMW ✓, 320d ✓, Automatic ✓"

### Scenario 4: Complex Multi-Criteria
**Given:** User query "Electric BMW under £30k with less than 20k miles and parking sensors"  
**When:** Hybrid search executes  
**Then:**
- Exact filters: Make=BMW, Fuel=Electric, Price<=30000, Mileage<20000, Features contains "Parking Sensors"
- All constraints applied (AND logic)
- Results: Only vehicles matching ALL criteria
- If 0 results: Suggest relaxing one constraint
- Explanation shows which criteria matched

### Scenario 5: Zero Results with Relaxation
**Given:** User query "BMW M3 under £10,000 with 5k miles"  
**When:** Search finds 0 results  
**Then:**
- System identifies over-constraint (unlikely to find M3 that cheap and low mileage)
- Suggests: "No results found. Try: Increase budget to £25k OR Allow up to 50k miles"
- Shows near-miss: BMW 3 series under £10k (relaxes "M3" to "3 series")

### Scenario 6: Result Explanation
**Given:** Query "Family SUV with tech features"  
**When:** Results presented  
**Then:**
- Each result shows:
  - "Audi Q5 - £24,500, 32k miles"
  - "Matches: SUV body ✓, 5 doors ✓, Navigation ✓, Parking Sensors ✓"
  - Relevance: 0.87
- Explanation clear and actionable

---

## 8. Non-Functional Requirements

### Performance:
- Complete search pipeline: <3 seconds
- Parallel execution of exact + semantic: <2 seconds
- Result fusion and ranking: <500ms
- Support 1000 concurrent searches

### Accuracy:
- 90%+ of results relevant to query intent
- Top-3 results relevant in 85%+ of searches
- Exact constraint violations: 0% (hard filters must work)
- Semantic boosting improves ranking in 70%+ of hybrid queries

### Explainability:
- Every result has match explanation
- Ranking logic transparent
- Users understand why result A ranked higher than B
- Strategy selection logged and auditable

### Scalability:
- Handle queries with 10+ constraints
- Search across 100,000 vehicles
- Maintain performance as data grows

---

## 9. Out of Scope

- Real-time result updates as user types
- Personalized ranking based on user history
- A/B testing different ranking formulas
- Machine learning-based ranking optimization
- Collaborative filtering ("users like you searched for...")
- Image-based result presentation

---

## 10. Edge Cases & Error Handling

### Edge Case 1: Conflicting Signals
**Scenario:** Exact match says low relevance, semantic says high  
**Handling:** Weight exact higher, exact match score wins

### Edge Case 2: All Results Same Score
**Scenario:** 10 vehicles all score 0.85  
**Handling:** Apply secondary sort (price low→high or mileage low→high)

### Edge Case 3: Performance Degradation
**Scenario:** Complex query takes >5 seconds  
**Handling:** Return partial results with timeout notice, suggest simplification

### Edge Case 4: Contradictory Filters
**Scenario:** "Diesel electric cars"  
**Handling:** Detected by FRD-001, clarification requested before search

---

## 11. Open Questions

1. **Ranking Weights:** Should weights be configurable per query type?
2. **Result Count:** Default top-k = 10, 20, or 50?
3. **Near-Miss Threshold:** How close to show as "almost matched"?
4. **Explanation Verbosity:** Detailed or concise match explanations?
5. **Performance vs. Accuracy:** Acceptable trade-off point?

---

## 12. Success Metrics

- **Relevance Rate:** 90%+ of top-10 results relevant
- **Top-3 Accuracy:** 85%+ of searches have relevant vehicle in top 3
- **Zero-Result Rate:** <15% of searches return 0 results
- **Performance:** 95%+ of searches complete in <3 seconds
- **User Satisfaction:** 4.5+ stars for result quality
- **Explanation Usefulness:** 80%+ users find explanations helpful

---

## 13. Search Examples

| Query Type | Query | Strategy | Result Highlights |
|------------|-------|----------|-------------------|
| Hybrid | "Reliable BMW under £20k" | Balanced | BMWs <£20k with service history |
| Semantic-Heavy | "Family car" | 70% semantic | SUVs, 5 doors, practical features |
| Exact-Heavy | "2023 Audi A3 automatic" | 80% exact | Exact match on all criteria |
| Complex | "Electric under £30k <20k miles with nav" | Hybrid | All 4 constraints applied (AND) |
| Zero Results | "Lamborghini under £10k" | Exact | Suggest alternative brands/budgets |
| Lifestyle | "Fun weekend car" | Semantic | Convertibles, coupes, sports variants |
