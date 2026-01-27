# Infrastructure & Orchestration Setup

This document describes the infrastructure scaffolding implemented for the VehicleSearch application.

## Overview

The VehicleSearch application uses a modern microservices architecture with two main components:
- **Backend API**: ASP.NET Core 8.0 Web API
- **Frontend**: Next.js 16 React application

Three orchestration options are available:
1. **.NET Aspire** (recommended for local development)
2. **Docker Compose** (for containerized local development)
3. **Individual Services** (for granular debugging)

## Project Structure

```
src/
├── VehicleSearch.Api/          # Backend API
├── VehicleSearch.Core/         # Domain models
├── VehicleSearch.Infrastructure/ # External services
├── VehicleSearch.Agents/       # AI agent implementations
├── VehicleSearch.ServiceDefaults/ # Shared configuration
└── VehicleSearch.AppHost/      # Aspire orchestration

frontend/                       # Next.js frontend application
```

## Components

### 1. ServiceDefaults Project

**Purpose**: Shared configuration for all services

**Features**:
- OpenTelemetry instrumentation (traces, metrics, logs)
- Health checks configuration
- Service discovery setup
- HTTP resilience patterns (Polly)
- OTLP exporter for production observability

**Key Classes**:
- `Extensions.cs`: Extension methods for configuring services

### 2. AppHost Project

**Purpose**: Orchestrates all services using .NET Aspire

**Features**:
- Manages backend API lifecycle
- Manages frontend application lifecycle
- Configures service-to-service communication
- Provides Aspire dashboard for monitoring
- Manages connection strings for Azure services

**Configuration**:
- Azure AI Search connection string
- Azure OpenAI connection string
- Automatic service endpoint discovery

### 3. Docker Configuration

#### Backend Dockerfile

**Location**: `src/VehicleSearch.Api/Dockerfile`

**Build Stages**:
1. **base**: Runtime image (aspnet:8.0)
2. **build**: Build environment (sdk:8.0)
3. **publish**: Published artifacts
4. **final**: Production image

**Exposed Ports**:
- 80 (HTTP)
- 443 (HTTPS)

#### Frontend Dockerfile

**Location**: `frontend/Dockerfile`

**Build Stages**:
1. **base**: Node 20 Alpine base
2. **deps**: Dependencies installation
3. **builder**: Build Next.js application
4. **runner**: Production runtime

**Features**:
- Standalone output for minimal image size
- Non-root user for security
- Static and server-side rendering support

**Exposed Port**: 3000

### 4. Docker Compose

**Location**: `docker-compose.yml`

**Services**:
- `api`: Backend API (ports 5001:8080, 7001:8081)
- `frontend`: Frontend app (port 3000:3000)

**Network**: `vehiclesearch-network` (bridge)

**Environment Variables**: Configured via .env file

## Environment Configuration

### Required Variables

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

# OpenTelemetry (optional)
OTEL_EXPORTER_OTLP_ENDPOINT=
```

### Configuration Methods

1. **.env file**: For Docker Compose
2. **User Secrets**: For local .NET development
3. **Aspire Configuration**: Via AppHost connection strings
4. **Environment Variables**: For containerized deployments

## Development Workflows

### Using .NET Aspire (Recommended)

**Prerequisites**:
- .NET 8.0 SDK
- .NET Aspire workload installed
- Node.js 20+

**Commands**:
```bash
# Start entire stack
npm run dev

# Access Aspire Dashboard
http://localhost:15000
```

**Benefits**:
- Single command startup
- Integrated dashboard
- Service discovery
- Aggregated logs
- Distributed tracing

### Using Docker Compose

**Prerequisites**:
- Docker Desktop
- Docker Compose

**Commands**:
```bash
# Build images
npm run docker:build

# Start services
npm run docker:up

# Stop services
npm run docker:down

# View logs
npm run docker:logs
```

**Benefits**:
- No Aspire workload required
- Containerized environment
- Production-like setup

### Running Individual Services

**Backend**:
```bash
npm run dev:api
# or
dotnet run --project src/VehicleSearch.Api
```

**Frontend**:
```bash
npm run dev:frontend
# or
cd frontend && npm run dev
```

**Benefits**:
- Granular control
- Faster iteration
- Individual debugging

## Health Checks

### Endpoints

- `/health`: Overall health status
- `/alive`: Liveness probe (always returns 200 when app is running)

### Implementation

Health checks are configured via `ServiceDefaults` extensions and include:
- Basic liveness check
- Future: Database connectivity
- Future: Azure service connectivity

## Observability

### OpenTelemetry

**Instrumentation**:
- ASP.NET Core requests
- HTTP client calls
- .NET runtime metrics

**Exporters**:
- Console (development)
- OTLP (production, when configured)

### Metrics

- Request duration
- Request count
- Error rates
- Runtime metrics (GC, memory, threads)

### Tracing

- Distributed tracing across services
- HTTP request/response tracking
- Custom spans (can be added per requirement)

### Logging

- Structured logging via Serilog
- OpenTelemetry log integration
- Aggregated in Aspire dashboard

## Build & Test

### Build Commands

```bash
# Build entire solution
npm run build

# Build frontend only
npm run build:frontend

# Build backend only
dotnet build src/VehicleSearch.slnx
```

### Test Commands

```bash
# Run all .NET tests
npm run test

# Run frontend tests
npm run test:frontend
```

## Next Steps

### Future Enhancements

1. **Production Deployment**:
   - Azure Container Apps deployment
   - Azure Kubernetes Service (AKS)
   - CI/CD pipelines

2. **Advanced Observability**:
   - Application Insights integration
   - Custom metrics dashboards
   - Alert rules

3. **Additional Health Checks**:
   - Azure AI Search connectivity
   - Azure OpenAI availability
   - Database health (if added)

4. **Performance**:
   - Response caching
   - CDN integration for frontend
   - API rate limiting

5. **Security**:
   - Authentication/Authorization
   - API key management
   - HTTPS enforcement

## Troubleshooting

### Common Issues

**Aspire won't start**:
- Ensure Aspire workload is installed: `dotnet workload install aspire`
- Check ports 15000, 3000, 5001, 7001 are available

**Docker build fails**:
- Clear Docker cache: `docker system prune -a`
- Check .dockerignore is not excluding required files

**Services can't communicate**:
- Verify network configuration in docker-compose.yml
- Check environment variables are set correctly
- Review Aspire dashboard for service status

**Frontend build fails**:
- Clear Next.js cache: `rm -rf frontend/.next`
- Reinstall dependencies: `cd frontend && rm -rf node_modules && npm install`

## References

- [.NET Aspire Documentation](https://learn.microsoft.com/en-us/dotnet/aspire/)
- [Docker Best Practices](https://docs.docker.com/develop/dev-best-practices/)
- [Next.js Docker Documentation](https://nextjs.org/docs/deployment#docker-image)
- [OpenTelemetry .NET](https://opentelemetry.io/docs/instrumentation/net/)
