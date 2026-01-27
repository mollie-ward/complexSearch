# Feature Requirements Document (FRD)

## Feature: Conversational Context Management

**Feature ID:** FRD-003  
**PRD Reference:** REQ-3  
**Version:** 1.0  
**Status:** Draft  
**Created:** January 27, 2026

---

## 1. Feature Overview

### Purpose
Maintain conversational state across multiple user queries, enabling users to refine and iterate on searches without repeating information, creating a natural dialogue flow.

### Business Value
- Improves user experience through natural interaction
- Reduces user effort (no need to repeat full queries)
- Increases engagement and session depth
- Differentiates from traditional static search

### User Impact
Users can have conversations like:
1. "Show me Audi SUVs" → Returns Q3, Q5, Q7
2. "Under £20,000" → Filters previous results by price
3. "With leather seats" → Further narrows to leather-equipped vehicles
4. "What about manual transmission?" → Changes automatic to manual constraint

---

## 2. Functional Requirements

### FR-1: Session Management
**What:** The system must maintain conversation sessions for each user.

**Capabilities:**
- Create unique session on first query
- Track session lifetime and activity
- Store conversation history within session
- Support session timeout after inactivity
- Allow explicit session reset ("start over", "new search")

**Acceptance Criteria:**
- New users get unique session ID
- Sessions persist for 30 minutes of inactivity
- Session history includes queries, results, and context
- Users can start fresh search explicitly
- Concurrent sessions for same user supported (multi-tab)

### FR-2: Context Preservation
**What:** The system must preserve search context across queries in a session.

**Capabilities:**
- Store previous query constraints
- Store previous search results
- Track applied filters and refinements
- Maintain attribute values from earlier queries
- Preserve user preferences stated in session

**Acceptance Criteria:**
- After "Show me BMW cars", constraint Make=BMW persists
- After "under £20k", price constraint persists for follow-up queries
- Context includes up to last 10 queries in session
- Context cleared on explicit reset or timeout

### FR-3: Reference Resolution
**What:** The system must resolve pronouns and references to previous context.

**Capabilities:**
- Interpret "them" / "those" / "these" → previous results
- Interpret "it" → specific previous vehicle
- Interpret "cheaper ones" → same criteria, lower price
- Interpret "bigger" → larger than previous body type/engine
- Interpret "similar" → semantically close to previous results

**Acceptance Criteria:**
- "Show me cheaper ones" reduces max price from previous query
- "What about bigger engines?" increases engine size constraint
- "More like that one" uses specific vehicle as reference
- Ambiguous references prompt clarification

### FR-4: Constraint Evolution Tracking
**What:** The system must track how search constraints change over conversation.

**Capabilities:**
- Distinguish constraint additions vs. modifications vs. removals
- Track which constraints are user-specified vs. system-inferred
- Maintain constraint history (what changed when)
- Support constraint rollback ("go back", "undo")
- Detect conflicting constraint changes

**Acceptance Criteria:**
- "Under £20k" after "Under £25k" modifies price constraint
- "What about electric?" adds fuel constraint, keeps other filters
- "Automatic" after "Manual" replaces transmission constraint
- "Remove price limit" removes price constraint
- Constraint history logged for each session

### FR-5: Multi-Turn Query Composition
**What:** The system must compose complete search criteria from multiple turns.

**Capabilities:**
- Combine constraints from all turns in session
- Apply most recent value for conflicting constraints
- Maintain AND logic across turns (all constraints apply)
- Support incremental refinement (narrowing results)
- Support relaxation (broadening results)

**Acceptance Criteria:**
- Turn 1: "BMW" + Turn 2: "Under £20k" + Turn 3: "Automatic" = BMW AND Price<=20k AND Transmission=Automatic
- Latest constraint wins for same attribute
- Complete query reconstructable from conversation history
- Results reflect cumulative constraints

### FR-6: Context-Aware Clarification
**What:** The system must ask for clarification when context is ambiguous.

**Capabilities:**
- Detect ambiguous references ("it", "that one" with multiple options)
- Request clarification before executing query
- Provide context options for user to choose
- Handle clarification responses as continuation
- Timeout clarification requests (don't wait indefinitely)

**Acceptance Criteria:**
- "Show me more like that" after viewing 5 vehicles prompts "Which vehicle?"
- Clarification options presented clearly
- User response to clarification continues conversation
- Unanswered clarifications default to most likely interpretation

---

## 3. Conversation Patterns

### Supported Patterns:

**Refinement (Narrowing):**
```
User: "Show me BMW cars"
System: [Returns 20 BMW vehicles]
User: "Under £20,000"
System: [Returns 8 BMW vehicles under £20k]
User: "With parking sensors"
System: [Returns 3 BMW vehicles under £20k with parking sensors]
```

**Modification (Changing Criteria):**
```
User: "Show me Audi A3"
System: [Returns Audi A3 vehicles]
User: "What about A4 instead?"
System: [Returns Audi A4 vehicles, replaces model constraint]
```

**Exploration (Alternatives):**
```
User: "Electric cars under £30k"
System: [Returns electric vehicles]
User: "What about hybrids?"
System: [Returns hybrid vehicles under £30k, keeps price]
```

**Comparison:**
```
User: "Show me BMW 3 series"
System: [Returns BMW 3 series]
User: "Compare to Audi A4"
System: [Shows comparison of both models]
```

**Relaxation (Broadening):**
```
User: "BMW M3 under £25k with less than 10k miles"
System: [Returns 0 results]
User: "What if I increase the mileage limit?"
System: [Suggests alternatives, relaxes mileage constraint]
```

---

## 4. Context Storage Structure

### Session Object:
```
{
  session_id: string,
  created_at: timestamp,
  last_activity: timestamp,
  conversation_history: [
    {
      turn_number: int,
      user_query: string,
      parsed_intent: object,
      applied_constraints: object,
      results_count: int,
      top_results: array,
      timestamp: timestamp
    }
  ],
  current_constraints: {
    make: string,
    model: string,
    price_max: number,
    mileage_max: number,
    fuel: string,
    transmission: string,
    features: array,
    location: string,
    ...
  },
  constraint_history: [
    {attribute: string, old_value: any, new_value: any, turn: int}
  ]
}
```

---

## 5. Inputs & Outputs

### Inputs:
- **User Query:** Current turn input
- **Session ID:** Identifies conversation
- **Parsed Query:** From FRD-001
- **Session Context:** Previous conversation state

### Outputs:
- **Updated Context:** Modified session state
- **Composite Query:** Combined constraints from all turns
- **Context Metadata:** What changed, what persisted
- **Clarification Request:** When ambiguous (optional)

---

## 6. Dependencies

### Depends On:
- Feature 1: Natural Language Query Understanding (provides parsed queries and detects refinement intent)
- Feature 5: Knowledge Base Integration (retrieves results based on composite queries)

### Depended On By:
- Feature 4: Hybrid Search Orchestration (uses composite query)

---

## 7. Acceptance Criteria

### Scenario 1: Simple Refinement
**Given:** Session exists with previous query "Show me Audi cars"  
**And:** Previous results returned 15 Audi vehicles  
**When:** User asks "Under £20,000"  
**Then:**
- Make constraint (Audi) persists
- Price constraint (<= 20000) added
- Composite query: Make=Audi AND Price<=20000
- Results re-executed with combined constraints
- Session context updated with new constraint

### Scenario 2: Constraint Replacement
**Given:** Session has constraint Transmission=Manual  
**When:** User asks "What about automatic?"  
**Then:**
- Transmission constraint updated to Automatic
- All other constraints persist
- Constraint history shows Manual → Automatic change
- New results reflect automatic transmission

### Scenario 3: Multi-Turn Composition
**Given:** Clean session (no previous queries)  
**When:** 
  - Turn 1: "BMW"
  - Turn 2: "Under £25k"
  - Turn 3: "Less than 50k miles"
  - Turn 4: "Automatic"
**Then:**
- Final composite query: Make=BMW AND Price<=25000 AND Mileage<50000 AND Transmission=Automatic
- All 4 constraints active simultaneously
- Results match all criteria

### Scenario 4: Reference Resolution
**Given:** Previous query "Show me electric BMWs" returned 5 vehicles  
**When:** User asks "Show me cheaper ones"  
**Then:**
- "Cheaper" interpreted relative to previous results
- Make=BMW and Fuel=Electric constraints persist
- Price constraint set to max(previous_results.price) - buffer
- Reference to "ones" resolved to previous result set

### Scenario 5: Ambiguous Reference
**Given:** Previous results showed 10 vehicles  
**When:** User asks "Tell me more about that one"  
**Then:**
- System detects ambiguous "that one"
- Clarification requested: "Which vehicle? [List of 10 options]"
- Conversation paused pending clarification
- User's next response identifies specific vehicle

### Scenario 6: Session Reset
**Given:** Session with 5 turns and complex constraints  
**When:** User says "Start a new search"  
**Then:**
- Session context cleared
- Constraint history reset
- Conversation history archived
- Next query treated as fresh search

---

## 8. Non-Functional Requirements

### Performance:
- Context retrieval: <50ms
- Context update: <100ms
- Session storage: <1KB per session
- Support 10,000 concurrent sessions

### Reliability:
- Session persistence across service restarts
- No context loss during session
- Graceful handling of corrupted session data

### Usability:
- Context changes explained to user
- Clear indication of active constraints
- Easy way to reset or modify context

---

## 9. Out of Scope

- Cross-session learning or memory
- User preference persistence beyond session
- Conversation history export or sharing
- Multi-user collaborative sessions
- Voice conversation support
- Emotional tone or sentiment tracking

---

## 10. Edge Cases & Error Handling

### Edge Case 1: Very Long Session
**Scenario:** User has 50+ turns in session  
**Handling:** Keep last 10 turns, archive older context, summarize constraints

### Edge Case 2: Contradictory References
**Query:** "Show me cheaper more expensive ones"  
**Handling:** Detect contradiction, request clarification

### Edge Case 3: Session Timeout
**Scenario:** User returns after 45 minutes (beyond timeout)  
**Handling:** Session expired message, start fresh, optionally show previous query

### Edge Case 4: Reference to Non-Existent Context
**Query:** "What about that blue one?" (but no blue vehicle in previous results)  
**Handling:** Notify no matching reference, suggest alternatives

---

## 11. Open Questions

1. **Session Duration:** 30 minutes timeout appropriate, or should it be configurable?
2. **History Depth:** How many turns to maintain in active context?
3. **Constraint Display:** Should active constraints be shown to user after each turn?
4. **Cross-Device:** Should sessions sync across devices for same user?
5. **Clarification Timeout:** How long to wait for clarification response?

---

## 12. Success Metrics

- **Session Depth:** Average 3+ queries per session
- **Refinement Rate:** 60%+ of sessions include refinement queries
- **Reference Resolution:** 90%+ of references correctly interpreted
- **Session Completion:** <5% of sessions abandoned due to context confusion
- **User Satisfaction:** 4+ stars for conversational flow

---

## 13. Conversation Examples

### Example 1: Progressive Refinement
```
1. User: "Show me Audi SUVs"
   Context: {make: "Audi", body: "SUV"}
   Results: Q3, Q5, Q7 models (12 vehicles)

2. User: "Under £25,000"
   Context: {make: "Audi", body: "SUV", price_max: 25000}
   Results: Q3 models + some Q5 (6 vehicles)

3. User: "With leather"
   Context: {make: "Audi", body: "SUV", price_max: 25000, features: ["leather"]}
   Results: 3 vehicles with leather trim

4. User: "Actually, increase budget to £30k"
   Context: {make: "Audi", body: "SUV", price_max: 30000, features: ["leather"]}
   Results: 5 vehicles (some Q5 models now included)
```

### Example 2: Alternative Exploration
```
1. User: "Electric cars"
   Context: {fuel: "Electric"}
   Results: i3, i4, iX, iX3 models

2. User: "What about hybrids instead?"
   Context: {fuel: "Hybrid"}  // Replaced electric
   Results: 330e, 530e, hybrid models

3. User: "Both electric and hybrid"
   Context: {fuel: ["Electric", "Hybrid"]}
   Results: Combined results from both
```
