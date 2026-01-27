# Task: Session Context Storage & Management

**Task ID:** 012  
**Feature:** Conversational Context Management  
**Type:** Backend Implementation  
**Priority:** Medium  
**Estimated Complexity:** Medium  
**FRD Reference:** FRD-003 (FR-1, FR-2, FR-3)

---

## Description

Implement session management to store conversation history, track search state, maintain user context across multiple queries, and enable stateful conversations.

---

## Dependencies

**Depends on:**
- Task 001: Backend API Scaffolding (for infrastructure)

**Blocks:**
- Task 013: Reference Resolution & Query Refinement
- Task 014: Search Strategy Selection (needs context)

---

## Technical Requirements

### Session Service Interface

```csharp
public interface IConversationSessionService
{
    Task<ConversationSession> CreateSessionAsync();
    Task<ConversationSession> GetSessionAsync(string sessionId);
    Task AddMessageAsync(string sessionId, ConversationMessage message);
    Task UpdateSearchStateAsync(string sessionId, SearchState state);
    Task<ConversationHistory> GetHistoryAsync(string sessionId, int maxMessages = 10);
    Task ClearSessionAsync(string sessionId);
    Task<bool> SessionExistsAsync(string sessionId);
}

public class ConversationSession
{
    public string SessionId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastAccessedAt { get; set; }
    public List<ConversationMessage> Messages { get; set; }
    public SearchState CurrentSearchState { get; set; }
    public Dictionary<string, object> Metadata { get; set; }
    public int MessageCount { get; set; }
}

public class ConversationMessage
{
    public string MessageId { get; set; }
    public DateTime Timestamp { get; set; }
    public MessageRole Role { get; set; }
    public string Content { get; set; }
    public ParsedQuery ParsedQuery { get; set; }
    public SearchResults Results { get; set; }
}

public enum MessageRole
{
    User,
    Assistant,
    System
}

public class SearchState
{
    public ParsedQuery LastQuery { get; set; }
    public List<string> LastResultIds { get; set; }
    public Dictionary<string, SearchConstraint> ActiveFilters { get; set; }
    public List<string> ViewedVehicleIds { get; set; }
    public DateTime LastSearchTime { get; set; }
}

public class ConversationHistory
{
    public string SessionId { get; set; }
    public List<ConversationMessage> Messages { get; set; }
    public int TotalMessages { get; set; }
}
```

### Session Storage Implementation

**In-Memory Storage (Development):**

```csharp
public class InMemoryConversationSessionService : IConversationSessionService
{
    private readonly ConcurrentDictionary<string, ConversationSession> _sessions = new();
    private readonly ILogger<InMemoryConversationSessionService> _logger;
    
    public Task<ConversationSession> CreateSessionAsync()
    {
        var session = new ConversationSession
        {
            SessionId = Guid.NewGuid().ToString(),
            CreatedAt = DateTime.UtcNow,
            LastAccessedAt = DateTime.UtcNow,
            Messages = new List<ConversationMessage>(),
            Metadata = new Dictionary<string, object>()
        };
        
        _sessions.TryAdd(session.SessionId, session);
        _logger.LogInformation("Created session {SessionId}", session.SessionId);
        
        return Task.FromResult(session);
    }
    
    public Task<ConversationSession> GetSessionAsync(string sessionId)
    {
        if (_sessions.TryGetValue(sessionId, out var session))
        {
            session.LastAccessedAt = DateTime.UtcNow;
            return Task.FromResult(session);
        }
        
        throw new SessionNotFoundException(sessionId);
    }
    
    public Task AddMessageAsync(string sessionId, ConversationMessage message)
    {
        if (_sessions.TryGetValue(sessionId, out var session))
        {
            message.MessageId = Guid.NewGuid().ToString();
            message.Timestamp = DateTime.UtcNow;
            session.Messages.Add(message);
            session.MessageCount = session.Messages.Count;
            session.LastAccessedAt = DateTime.UtcNow;
            
            _logger.LogInformation(
                "Added message to session {SessionId}, total messages: {Count}",
                sessionId,
                session.MessageCount
            );
            
            return Task.CompletedTask;
        }
        
        throw new SessionNotFoundException(sessionId);
    }
}
```

**Distributed Storage (Production - Redis):**

```csharp
public class RedisConversationSessionService : IConversationSessionService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisConversationSessionService> _logger;
    
    public async Task<ConversationSession> CreateSessionAsync()
    {
        var session = new ConversationSession
        {
            SessionId = Guid.NewGuid().ToString(),
            CreatedAt = DateTime.UtcNow,
            LastAccessedAt = DateTime.UtcNow,
            Messages = new List<ConversationMessage>(),
            Metadata = new Dictionary<string, object>()
        };
        
        var db = _redis.GetDatabase();
        var key = GetSessionKey(session.SessionId);
        var value = JsonSerializer.Serialize(session);
        
        await db.StringSetAsync(key, value, TimeSpan.FromHours(24));
        
        return session;
    }
    
    public async Task<ConversationSession> GetSessionAsync(string sessionId)
    {
        var db = _redis.GetDatabase();
        var key = GetSessionKey(sessionId);
        var value = await db.StringGetAsync(key);
        
        if (value.IsNullOrEmpty)
            throw new SessionNotFoundException(sessionId);
        
        var session = JsonSerializer.Deserialize<ConversationSession>(value);
        session.LastAccessedAt = DateTime.UtcNow;
        
        // Update last accessed time
        await db.StringSetAsync(key, JsonSerializer.Serialize(session), TimeSpan.FromHours(24));
        
        return session;
    }
    
    private string GetSessionKey(string sessionId) => $"session:{sessionId}";
}
```

### Session Lifecycle Management

**Session Creation:**
- Generate unique session ID (GUID)
- Initialize empty message list
- Set creation timestamp
- Return session ID to client

**Session Access:**
- Retrieve session by ID
- Update last accessed timestamp
- Extend TTL (Time To Live)

**Session Expiration:**
- In-Memory: 4 hours of inactivity
- Redis: 24 hours of inactivity
- Cleanup: Background job every hour

**Cleanup Service:**

```csharp
public class SessionCleanupService : BackgroundService
{
    private readonly IConversationSessionService _sessionService;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(1);
    private readonly TimeSpan _sessionTimeout = TimeSpan.FromHours(4);
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await CleanupExpiredSessionsAsync();
            await Task.Delay(_cleanupInterval, stoppingToken);
        }
    }
    
    private async Task CleanupExpiredSessionsAsync()
    {
        // Implementation depends on storage type
        // For in-memory: iterate and remove old sessions
        // For Redis: TTL handles this automatically
    }
}
```

### Search State Tracking

**Track active filters and results:**

```csharp
public async Task UpdateSearchStateAsync(string sessionId, SearchState state)
{
    var session = await GetSessionAsync(sessionId);
    
    session.CurrentSearchState = state;
    session.LastAccessedAt = DateTime.UtcNow;
    
    // Persist updated session
    await SaveSessionAsync(session);
}
```

**State includes:**
- Last parsed query
- Last search results (IDs only, not full objects)
- Active filters
- Viewed vehicles (for "show me others" scenarios)
- Last search timestamp

### API Endpoints

**POST /api/v1/conversation**

Create new session:
```json
Request: {}
Response: {
  "sessionId": "abc123-def456-ghi789",
  "createdAt": "2026-01-27T10:00:00Z"
}
```

**GET /api/v1/conversation/{sessionId}**

Get session details:
```json
Response: {
  "sessionId": "abc123",
  "createdAt": "2026-01-27T10:00:00Z",
  "lastAccessedAt": "2026-01-27T10:15:00Z",
  "messageCount": 5,
  "currentSearchState": {
    "lastQuery": "BMW under £20k",
    "lastResultIds": ["V001", "V002", "V003"],
    "activeFilters": {
      "make": { "value": "BMW" },
      "price": { "value": 20000, "operator": "le" }
    }
  }
}
```

**GET /api/v1/conversation/{sessionId}/history**

Get conversation history:
```json
Response: {
  "sessionId": "abc123",
  "messages": [
    {
      "messageId": "msg1",
      "timestamp": "2026-01-27T10:00:00Z",
      "role": "User",
      "content": "Show me BMW cars",
      "parsedQuery": { /* ... */ }
    },
    {
      "messageId": "msg2",
      "timestamp": "2026-01-27T10:00:05Z",
      "role": "Assistant",
      "content": "I found 12 BMW vehicles matching your criteria",
      "results": { "count": 12 }
    }
  ],
  "totalMessages": 2
}
```

**DELETE /api/v1/conversation/{sessionId}**

Clear session:
```json
Response: {
  "success": true,
  "message": "Session cleared"
}
```

---

## Acceptance Criteria

### Functional Criteria

✅ **Session Management:**
- [ ] Sessions created with unique IDs
- [ ] Sessions retrieved by ID
- [ ] Sessions expire after inactivity (4 hours in-memory, 24 hours Redis)
- [ ] Sessions can be deleted manually

✅ **Message History:**
- [ ] Messages added to session
- [ ] Message history retrieved (last 10 by default)
- [ ] Messages include timestamp, role, content
- [ ] Parsed queries and results stored

✅ **Search State:**
- [ ] Last query tracked
- [ ] Active filters stored
- [ ] Viewed vehicles tracked
- [ ] State updated on each search

✅ **Persistence:**
- [ ] In-memory storage works (dev)
- [ ] Redis storage works (prod)
- [ ] TTL enforced correctly

### Technical Criteria

✅ **Performance:**
- [ ] Session creation <50ms
- [ ] Session retrieval <100ms (in-memory), <200ms (Redis)
- [ ] Message addition <50ms
- [ ] Supports 10,000 concurrent sessions

✅ **Reliability:**
- [ ] No data loss on server restart (Redis)
- [ ] Graceful handling of expired sessions
- [ ] Concurrent access safe (thread-safe)

---

## Testing Requirements

### Unit Tests

**Test Coverage:** ≥85%

**Test Cases:**
```csharp
[Fact]
public async Task CreateSession_ReturnsUniqueSessionId()

[Fact]
public async Task GetSession_ExistingSession_ReturnsSession()

[Fact]
public async Task GetSession_NonExistentSession_ThrowsNotFoundException()

[Fact]
public async Task AddMessage_ValidSession_AddsMessage()

[Fact]
public async Task UpdateSearchState_ValidSession_UpdatesState()

[Fact]
public async Task SessionExpiration_InactiveSession_ExpiresAfterTimeout()
```

### Integration Tests

**Test Cases:**
- [ ] Create 100 sessions concurrently
- [ ] Retrieve sessions under load
- [ ] Test Redis integration (if used)
- [ ] Test session cleanup job

---

## Implementation Notes

### DO:
- ✅ Use in-memory storage for development
- ✅ Use Redis for production (scalability)
- ✅ Implement TTL for automatic cleanup
- ✅ Store only essential data (limit message size)
- ✅ Make storage implementation swappable (DI)
- ✅ Log all session operations

### DON'T:
- ❌ Store full vehicle objects in session (only IDs)
- ❌ Allow unlimited session growth
- ❌ Skip TTL implementation
- ❌ Make storage implementation non-configurable

### Configuration:
```json
{
  "ConversationSession": {
    "StorageType": "Redis",  // "InMemory" or "Redis"
    "RedisConnectionString": "localhost:6379",
    "SessionTimeoutHours": 24,
    "MaxMessagesPerSession": 100,
    "CleanupIntervalHours": 1
  }
}
```

---

## Definition of Done

- [ ] Session service interface defined
- [ ] In-memory storage implemented
- [ ] Redis storage implemented (optional for v1)
- [ ] Session lifecycle management working
- [ ] Search state tracking functional
- [ ] API endpoints functional
- [ ] Session cleanup job implemented
- [ ] All unit tests pass (≥85% coverage)
- [ ] Integration tests pass
- [ ] Documentation updated
- [ ] Code reviewed and approved

---

## Related Files

**Created/Modified:**
- `src/VehicleSearch.Core/Interfaces/IConversationSessionService.cs`
- `src/VehicleSearch.Core/Models/ConversationSession.cs`
- `src/VehicleSearch.Infrastructure/Session/InMemoryConversationSessionService.cs`
- `src/VehicleSearch.Infrastructure/Session/RedisConversationSessionService.cs`
- `src/VehicleSearch.Infrastructure/Session/SessionCleanupService.cs`
- `src/VehicleSearch.Api/Controllers/ConversationController.cs`
- `tests/VehicleSearch.Infrastructure.Tests/ConversationSessionServiceTests.cs`

**References:**
- FRD-003: Conversational Context Management (FR-1, FR-2, FR-3)
- Task 013: Reference resolution needs session context
