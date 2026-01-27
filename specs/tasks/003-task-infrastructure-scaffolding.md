# Task: Infrastructure & Orchestration Scaffolding

**Task ID:** 003  
**GitHub Issue:** [#5](https://github.com/mollie-ward/complexSearch/issues/5)  
**Status:** Assigned to Copilot (PR pending)  
**Feature:** Infrastructure Foundation  
**Type:** Scaffolding  
**Priority:** Critical (Must complete before deployment)  
**Estimated Complexity:** Low-Medium

---

## Description

Set up .NET Aspire for local development orchestration, Docker containerization, and deployment configuration for both backend API and frontend applications.

---

## Dependencies

**Depends on:**
- Task 001: Backend API Scaffolding
- Task 002: Frontend Application Scaffolding

**Blocks:**
- Local development workflow
- Deployment tasks
- Integration testing with full stack

---

## Technical Requirements

### .NET Aspire Setup

**Aspire Host Project:**
Create `src/VehicleSearch.AppHost/` project to orchestrate all services.

**Structure:**
```
src/VehicleSearch.AppHost/
├── Program.cs
├── VehicleSearch.AppHost.csproj
└── appsettings.json
```

**Dependencies:**
- Aspire.Hosting
- Aspire.Hosting.Azure (for Azure resource integration)

**Program.cs Configuration:**
```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Add backend API
var api = builder.AddProject<Projects.VehicleSearch_Api>("api")
    .WithHttpEndpoint(port: 5001, name: "http")
    .WithHttpsEndpoint(port: 7001, name: "https");

// Add frontend
var frontend = builder.AddNpmApp("frontend", "../frontend")
    .WithHttpEndpoint(port: 3000)
    .WithEnvironment("NEXT_PUBLIC_API_URL", api.GetEndpoint("http"));

// Add Azure AI Search (for local dev, use connection string)
var search = builder.AddConnectionString("AzureAISearch");

// Add Azure OpenAI
var openai = builder.AddConnectionString("AzureOpenAI");

builder.Build().Run();
```

### Service Defaults Project

Create `src/VehicleSearch.ServiceDefaults/` for shared configuration.

**Structure:**
```
src/VehicleSearch.ServiceDefaults/
├── Extensions.cs
└── VehicleSearch.ServiceDefaults.csproj
```

**Extensions.cs:**
- OpenTelemetry configuration
- Health checks configuration
- Service discovery setup
- Resilience patterns (Polly)

### Docker Configuration

**Backend Dockerfile:**
```dockerfile
# src/VehicleSearch.Api/Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["VehicleSearch.Api/VehicleSearch.Api.csproj", "VehicleSearch.Api/"]
COPY ["VehicleSearch.Core/VehicleSearch.Core.csproj", "VehicleSearch.Core/"]
COPY ["VehicleSearch.Infrastructure/VehicleSearch.Infrastructure.csproj", "VehicleSearch.Infrastructure/"]
COPY ["VehicleSearch.Agents/VehicleSearch.Agents.csproj", "VehicleSearch.Agents/"]
RUN dotnet restore "VehicleSearch.Api/VehicleSearch.Api.csproj"
COPY . .
WORKDIR "/src/VehicleSearch.Api"
RUN dotnet build "VehicleSearch.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "VehicleSearch.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "VehicleSearch.Api.dll"]
```

**Frontend Dockerfile:**
```dockerfile
# frontend/Dockerfile
FROM node:20-alpine AS base

FROM base AS deps
WORKDIR /app
COPY package*.json ./
RUN npm ci

FROM base AS builder
WORKDIR /app
COPY --from=deps /app/node_modules ./node_modules
COPY . .
RUN npm run build

FROM base AS runner
WORKDIR /app
ENV NODE_ENV production
COPY --from=builder /app/public ./public
COPY --from=builder /app/.next/standalone ./
COPY --from=builder /app/.next/static ./.next/static

EXPOSE 3000
ENV PORT 3000
CMD ["node", "server.js"]
```

### Docker Compose (Alternative to Aspire for simple orchestration)

**docker-compose.yml:**
```yaml
version: '3.8'

services:
  api:
    build:
      context: ./src
      dockerfile: VehicleSearch.Api/Dockerfile
    ports:
      - "5001:80"
      - "7001:443"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - AzureAISearch__Endpoint=${AZURE_SEARCH_ENDPOINT}
      - AzureOpenAI__Endpoint=${AZURE_OPENAI_ENDPOINT}
    depends_on:
      - frontend

  frontend:
    build:
      context: ./frontend
      dockerfile: Dockerfile
    ports:
      - "3000:3000"
    environment:
      - NEXT_PUBLIC_API_URL=http://api:80
```

### Environment Configuration

**.env (for Aspire/Docker Compose):**
```bash
# Azure AI Search
AZURE_SEARCH_ENDPOINT=https://your-search.search.windows.net
AZURE_SEARCH_API_KEY=your-key

# Azure OpenAI
AZURE_OPENAI_ENDPOINT=https://your-openai.openai.azure.com
AZURE_OPENAI_API_KEY=your-key
AZURE_OPENAI_DEPLOYMENT=gpt-4
AZURE_OPENAI_EMBEDDING_DEPLOYMENT=text-embedding-ada-002

# GitHub Models (alternative)
GITHUB_TOKEN=your-github-token
```

### Development Scripts

**Package.json scripts (root):**
```json
{
  "scripts": {
    "dev": "dotnet run --project src/VehicleSearch.AppHost",
    "dev:api": "dotnet run --project src/VehicleSearch.Api",
    "dev:frontend": "cd frontend && npm run dev",
    "build": "dotnet build",
    "test": "dotnet test",
    "docker:build": "docker-compose build",
    "docker:up": "docker-compose up",
    "docker:down": "docker-compose down"
  }
}
```

### Health Checks Dashboard

Aspire provides automatic dashboard at `http://localhost:15000` showing:
- All running services
- Health status
- Logs aggregation
- Distributed tracing
- Metrics

---

## Acceptance Criteria

### Functional Criteria

✅ **Aspire Orchestration:**
- [ ] Aspire host project created
- [ ] Backend API registered in Aspire
- [ ] Frontend app registered in Aspire
- [ ] All services start with single command (`dotnet run`)
- [ ] Aspire dashboard accessible and shows all services

✅ **Service Communication:**
- [ ] Frontend can call backend API
- [ ] Service discovery works (frontend finds API URL)
- [ ] Environment variables passed correctly between services

✅ **Docker Configuration:**
- [ ] Backend Dockerfile builds successfully
- [ ] Frontend Dockerfile builds successfully
- [ ] Docker Compose orchestrates both services
- [ ] Services communicate in Docker network

✅ **Development Experience:**
- [ ] Single command starts entire stack
- [ ] Hot reload works for both frontend and backend
- [ ] Logs aggregated and accessible
- [ ] Easy to start/stop individual services

### Technical Criteria

✅ **Configuration:**
- [ ] Environment variables externalized
- [ ] No secrets in source control
- [ ] User secrets configured for local dev
- [ ] Production config ready (placeholder)

✅ **Observability:**
- [ ] Health checks configured
- [ ] OpenTelemetry tracing setup (basic)
- [ ] Logs structured and queryable
- [ ] Metrics endpoint available

---

## Testing Requirements

### Integration Tests

**Test Coverage:** Verify orchestration works

**Tests:**
- [ ] All services start successfully
- [ ] Frontend can reach backend health endpoint
- [ ] Environment variables propagate correctly
- [ ] Service dependencies resolve

**Testing Approach:**
- Manual testing for orchestration
- Automated tests for service health checks

---

## Implementation Notes

### DO:
- ✅ Use Aspire for local development (simplifies setup)
- ✅ Provide Docker alternative for teams without Aspire
- ✅ Externalize all configuration
- ✅ Setup basic observability (logs, traces, metrics)
- ✅ Document how to run the stack locally

### DON'T:
- ❌ Include production deployment config yet
- ❌ Setup CI/CD pipelines (separate task)
- ❌ Configure load balancing (not needed for v1)
- ❌ Include database orchestration (not needed)

### Developer Experience:
- One command to start everything
- Clear error messages if services fail
- Easy to debug individual services
- Fast startup time (<30 seconds)

---

## Definition of Done

- [ ] Aspire host project created and configured
- [ ] All services registered in Aspire
- [ ] Entire stack starts with `dotnet run`
- [ ] Aspire dashboard shows all services healthy
- [ ] Backend and frontend communicate successfully
- [ ] Docker Compose alternative works
- [ ] Environment variables configured
- [ ] Health checks functional
- [ ] Documentation updated (how to run locally)
- [ ] Code reviewed and approved

---

## Related Files

**Created/Modified:**
- `src/VehicleSearch.AppHost/Program.cs`
- `src/VehicleSearch.AppHost/VehicleSearch.AppHost.csproj`
- `src/VehicleSearch.ServiceDefaults/Extensions.cs`
- `src/VehicleSearch.Api/Dockerfile`
- `frontend/Dockerfile`
- `docker-compose.yml`
- `.env.example`
- `README.md` (setup instructions)

**References:**
- AGENTS.md (Infrastructure section)
- Task 001 (Backend API)
- Task 002 (Frontend App)
- .NET Aspire documentation
