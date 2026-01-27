# Task: Backend API Scaffolding

**Task ID:** 001  
**GitHub Issue:** [#1](https://github.com/mollie-ward/complexSearch/issues/1)  
**Pull Request:** [#2](https://github.com/mollie-ward/complexSearch/pull/2)  
**Feature:** Infrastructure Foundation  
**Type:** Scaffolding  
**Priority:** Critical (Must complete first)  
**Estimated Complexity:** Medium

---

## Description

Create the foundational .NET 8 ASP.NET Core Web API project structure with all necessary configuration, middleware, dependency injection setup, and project organization that all feature tasks will build upon.

---

## Dependencies

**Depends on:**
- None (this is the first task)

**Blocks:**
- All backend feature tasks
- API documentation tasks
- Backend testing tasks

---

## Technical Requirements

### Project Structure

Create the following .NET solution structure:
```
src/
├── VehicleSearch.sln
├── VehicleSearch.Api/              # Main API project
├── VehicleSearch.Core/             # Core domain logic
├── VehicleSearch.Infrastructure/   # External integrations
└── VehicleSearch.Agents/           # AI Agent implementations

tests/
├── VehicleSearch.Api.Tests/
├── VehicleSearch.Core.Tests/
└── VehicleSearch.Infrastructure.Tests/
```

### API Project (VehicleSearch.Api)

**Required Configuration:**
- Target Framework: .NET 8
- Nullable reference types enabled
- ImplicitUsings enabled
- OpenAPI/Swagger support
- CORS configuration
- Health checks endpoint
- Structured logging with Serilog

**Required Middleware (in order):**
1. Exception handling middleware
2. CORS middleware
3. Request logging middleware
4. Rate limiting middleware (placeholder)
5. Authentication middleware (placeholder for v2)
6. Routing middleware
7. Endpoint middleware

**Project Dependencies:**
- Microsoft.AspNetCore.OpenApi
- Swashbuckle.AspNetCore
- Serilog.AspNetCore
- Serilog.Sinks.Console
- Azure.AI.OpenAI (for AI integration)
- Azure.Search.Documents (for search integration)

### Core Project (VehicleSearch.Core)

**Purpose:** Domain entities, interfaces, and business logic

**Structure:**
```
VehicleSearch.Core/
├── Entities/
│   └── Vehicle.cs
├── Interfaces/
│   ├── ISearchEngine.cs
│   ├── IKnowledgeBaseService.cs
│   ├── IConversationService.cs
│   └── ISafetyService.cs
├── Models/
│   ├── SearchRequest.cs
│   ├── SearchResponse.cs
│   ├── VehicleResult.cs
│   └── ConversationContext.cs
└── Exceptions/
    ├── ValidationException.cs
    └── ServiceException.cs
```

**No Dependencies:** This project should have NO external dependencies except .NET base libraries

### Infrastructure Project (VehicleSearch.Infrastructure)

**Purpose:** Implementations of external integrations

**Structure:**
```
VehicleSearch.Infrastructure/
├── AI/
│   ├── AzureOpenAIClient.cs
│   └── EmbeddingService.cs
├── Search/
│   ├── AzureSearchClient.cs
│   └── VectorSearchService.cs
└── Data/
    └── CsvDataLoader.cs
```

**Project Dependencies:**
- Azure.AI.OpenAI
- Azure.Search.Documents
- CsvHelper (for CSV parsing)
- Reference to VehicleSearch.Core

### Agents Project (VehicleSearch.Agents)

**Purpose:** AI agent implementations

**Structure:**
```
VehicleSearch.Agents/
├── SearchAgent/
├── SafetyAgent/
└── ContextAgent/
```

**Project Dependencies:**
- Microsoft.SemanticKernel or Agent Framework SDK
- Reference to VehicleSearch.Core
- Reference to VehicleSearch.Infrastructure

### Configuration Files

**appsettings.json:**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "Cors": {
    "AllowedOrigins": ["http://localhost:3000"]
  },
  "RateLimiting": {
    "RequestsPerMinute": 10,
    "RequestsPerHour": 100
  },
  "Search": {
    "MaxResults": 100,
    "DefaultResults": 10
  }
}
```

**appsettings.Development.json:**
- Include Swagger configuration
- Verbose logging
- Development-specific settings

**Program.cs Structure:**
- Service registration (DI container)
- Middleware pipeline configuration
- OpenAPI/Swagger setup
- Health checks configuration
- CORS policy registration
- Minimal API or Controller routing

### Health Check Endpoint

Implement `/api/health` endpoint returning:
- API status
- Dependencies status (placeholder)
- Timestamp
- Version information

### Error Handling

**Global Exception Middleware:**
- Catch all unhandled exceptions
- Return standardized error responses
- Log exceptions with stack traces
- Never expose internal errors to clients

**Standard Error Response Format:**
```json
{
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "Human-readable error message",
    "details": [] // Optional validation details
  },
  "timestamp": "2026-01-27T10:00:00Z",
  "traceId": "correlation-id"
}
```

### Logging Configuration

**Structured Logging with Serilog:**
- Console sink for development
- File sink optional (for later)
- Include correlation IDs
- Log request/response metadata
- Separate logs by severity

**Log Categories:**
- HTTP requests/responses
- AI agent interactions
- Search queries (anonymized)
- Errors and exceptions
- Performance metrics

---

## Acceptance Criteria

### Functional Criteria

✅ **Project Structure:**
- [ ] All 4 projects created in solution (Api, Core, Infrastructure, Agents)
- [ ] All 3 test projects created
- [ ] Solution builds without errors
- [ ] All projects target .NET 8

✅ **API Configuration:**
- [ ] API starts successfully on `https://localhost:7001` and `http://localhost:5001`
- [ ] Swagger UI accessible at `/swagger`
- [ ] Health check endpoint returns 200 OK at `/api/health`
- [ ] CORS configured for localhost:3000

✅ **Middleware Pipeline:**
- [ ] Global exception handling works (test by throwing exception)
- [ ] Request logging captures incoming requests
- [ ] Structured logging outputs to console
- [ ] Rate limiting middleware registered (implementation in later task)

✅ **Dependency Injection:**
- [ ] Services registered with appropriate lifetimes
- [ ] DI container resolves all registered services
- [ ] No circular dependencies

✅ **Error Handling:**
- [ ] Unhandled exceptions return 500 with standard error format
- [ ] Validation errors return 400 with details
- [ ] Not found errors return 404
- [ ] Errors include correlation/trace ID

### Technical Criteria

✅ **Code Quality:**
- [ ] All code follows naming conventions from AGENTS.md
- [ ] XML documentation comments on public types/methods
- [ ] No compiler warnings
- [ ] Nullable reference types handled correctly

✅ **Configuration:**
- [ ] Configuration loaded from appsettings.json
- [ ] User secrets configured for local development
- [ ] No hardcoded values (all externalized)

✅ **OpenAPI:**
- [ ] OpenAPI 3.0 spec generated
- [ ] API versioning configured (v1)
- [ ] Standard endpoints documented

---

## Testing Requirements

### Unit Tests (VehicleSearch.Api.Tests)

**Test Coverage:** N/A for scaffolding (no business logic yet)

**Infrastructure Tests:**
- [ ] Test exception middleware catches and formats errors correctly
- [ ] Test health check endpoint returns correct status
- [ ] Test CORS policy allows localhost:3000
- [ ] Test logging middleware logs requests

**Testing Framework:**
- xUnit
- Moq for mocking
- FluentAssertions for assertions

### Integration Tests

**Test Coverage:** ≥85% (once business logic added)

**Scaffolding Tests:**
- [ ] API starts successfully (WebApplicationFactory)
- [ ] Health endpoint returns 200
- [ ] Swagger endpoint accessible
- [ ] Error responses follow standard format

**Test Setup:**
- Use WebApplicationFactory<Program>
- In-memory services where possible
- Test configuration override

---

## Implementation Notes

### DO:
- ✅ Use async/await for all I/O operations
- ✅ Follow dependency injection patterns
- ✅ Implement standard RESTful conventions
- ✅ Use configuration over hardcoded values
- ✅ Include XML documentation on public APIs
- ✅ Configure CORS for frontend access

### DON'T:
- ❌ Include business logic in this task (that's for feature tasks)
- ❌ Implement actual search or AI functionality yet
- ❌ Add authentication (out of scope for v1)
- ❌ Hardcode any configuration values
- ❌ Skip error handling middleware

### Future Considerations:
- Authentication middleware (v2)
- Database context (if needed in future)
- Background job services (if needed)
- Caching layer (Redis, if needed)

---

## Definition of Done

- [ ] All 7 projects created and build successfully
- [ ] API runs locally without errors
- [ ] Swagger UI accessible and displays API info
- [ ] Health check endpoint works
- [ ] All middleware registered in correct order
- [ ] Error handling returns standardized responses
- [ ] Logging configured and working
- [ ] CORS allows frontend origin
- [ ] All unit tests pass
- [ ] All integration tests pass
- [ ] Code reviewed and approved
- [ ] Documentation updated

---

## Related Files

**Created/Modified:**
- `src/VehicleSearch.sln`
- `src/VehicleSearch.Api/Program.cs`
- `src/VehicleSearch.Api/appsettings.json`
- `src/VehicleSearch.Core/**`
- `src/VehicleSearch.Infrastructure/**`
- `src/VehicleSearch.Agents/**`
- `tests/VehicleSearch.Api.Tests/**`

**References:**
- AGENTS.md (Architecture & Standards)
- PRD Section 3 (Performance: <3s response time)
- PRD Section 8 (Non-functional requirements)
