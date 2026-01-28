
# spec2cloud

**Spec2Cloud** is an AI-powered development workflow that transforms high-level product ideas into production-ready applications deployed on Azure—using specialized GitHub Copilot agents working together.

## 🎯 Overview

This repository provides a preconfigured development environment and agent-driven workflow that works in two directions:

- **Greenfield (Build New)**: Transform product ideas into deployed applications through structured specification-driven development

https://github.com/user-attachments/assets/f0529e70-f437-4a14-93bc-4ab5a0450540


- **Greenfield (Shell-Based)**: Start from a predefined “shell” baseline and let coding agents translate natural language requirements to fill in the gaps via code.
   - https://github.com/EmeaAppGbb/shell-dotnet
   - https://github.com/EmeaAppGbb/agentic-shell-dotnet
   - https://github.com/EmeaAppGbb/agentic-shell-python



- **Brownfield (Document Existing + Modernize)**: Reverse engineer existing codebases into comprehensive product and technical documentation and optionally modernize codebases

Both workflows use specialized GitHub Copilot agents working together to maintain consistency, traceability, and best practices.

## 🚀 Quick Start

### Option 1: Use This Repository as a Template (Full Environment)

**Greenfield (New Project)**:
1. **Use this repo as a template** - Click "Use this template" to create your own GitHub repository
2. **Open in Dev Container** - Everything is preconfigured in `.devcontainer/`
3. **Describe your app idea** - The more specific, the better
4. **Follow the workflow** - Use the prompts to guide specialized agents through each phase

**Brownfield (Existing Codebase)**:
1. **Use this repo as a template** - Click "Use this template" to create your own GitHub repository
2. **copy your existing codebase** into the new repository
3. **Open in Dev Container** - Everything is preconfigured in `.devcontainer/`
4. **Run `/rev-eng`** - Reverse engineer codebase into specs and documentation
5. **Run `/modernize`** - (optional) Create modernization plan and tasks
6. **Run `/plan`** - (optional) Execute modernization tasks planned by the modernization agent

### Option 2: Install Into Existing Project using VSCode Extension

TODO

### Option 3: Install Into Existing Project using APM CLI

TODO

## 🚗 Vehicle Search Application - Local Development

This repository now contains a fully functional AI-powered vehicle search application. Here's how to run it locally:

### Prerequisites

- .NET 8.0 SDK or later
- Node.js 20+
- Docker (optional, for containerized deployment)
- .NET Aspire workload (optional, for Aspire orchestration - see installation below)

### Installing .NET Aspire Workload (Optional)

If you want to use .NET Aspire for orchestration:

```bash
dotnet workload update
dotnet workload install aspire
```

Note: Aspire is optional. You can run services individually or use Docker Compose instead.

### Running with .NET Aspire (Recommended - requires Aspire workload)

.NET Aspire provides the easiest way to run the entire stack with a single command.

**Important**: Requires .NET Aspire workload to be installed (see Prerequisites above).

1. **Clone the repository** (if you haven't already)
2. **Install dependencies:**
   ```bash
   cd frontend
   npm install
   cd ..
   ```

3. **Configure environment variables** (optional for local dev):
   ```bash
   cp .env.example .env
   # Edit .env with your Azure credentials if needed
   ```

4. **Run the entire stack:**
   ```bash
   npm run dev
   # or directly: dotnet run --project src/VehicleSearch.AppHost
   ```

5. **Access the applications:**
   - **Aspire Dashboard**: http://localhost:15000 (Service orchestration and monitoring)
   - **Frontend**: http://localhost:3000
   - **Backend API**: http://localhost:5001
   - **API Swagger**: http://localhost:5001/swagger

The Aspire dashboard provides:
- Real-time service status
- Aggregated logs from all services
- Distributed tracing
- Health check monitoring

### Running Services Individually

If you prefer to run services separately:

**Backend API:**
```bash
npm run dev:api
# or: dotnet run --project src/VehicleSearch.Api
```

**Frontend:**
```bash
npm run dev:frontend
# or: cd frontend && npm run dev
```

### Running with Docker Compose

For a containerized setup without Aspire:

1. **Build the containers:**
   ```bash
   npm run docker:build
   # or: docker-compose build
   ```

2. **Start the services:**
   ```bash
   npm run docker:up
   # or: docker-compose up
   ```

3. **Access the applications:**
   - Frontend: http://localhost:3000
   - Backend API: http://localhost:5001

4. **Stop the services:**
   ```bash
   npm run docker:down
   # or: docker-compose down
   ```

### Building the Solution

**Build all .NET projects:**
```bash
npm run build
# or: dotnet build src/VehicleSearch.slnx
```

**Build frontend:**
```bash
npm run build:frontend
# or: cd frontend && npm run build
```

### Running Tests

**Run all .NET tests:**
```bash
npm run test
# or: dotnet test src/VehicleSearch.slnx
```

**Run frontend tests:**
```bash
npm run test:frontend
# or: cd frontend && npm test
```

**Run E2E tests:**
```bash
npm run test:e2e
```

For detailed testing documentation, see [Testing](#testing) section below.

### Environment Configuration

The application uses the following environment variables (see `.env.example` for a complete list):

- `AZURE_SEARCH_ENDPOINT`: Azure AI Search endpoint
- `AZURE_SEARCH_API_KEY`: Azure AI Search API key
- `AZURE_OPENAI_ENDPOINT`: Azure OpenAI endpoint
- `AZURE_OPENAI_API_KEY`: Azure OpenAI API key
- `AZURE_OPENAI_DEPLOYMENT`: GPT model deployment name
- `AZURE_OPENAI_EMBEDDING_DEPLOYMENT`: Embedding model deployment name
- `GITHUB_TOKEN`: GitHub token for GitHub Models (alternative to Azure OpenAI)

For local development, you can use user secrets for the .NET API:
```bash
cd src/VehicleSearch.Api
dotnet user-secrets set "ConnectionStrings:AzureAISearch" "your-endpoint"
dotnet user-secrets set "ConnectionStrings:AzureOpenAI" "your-endpoint"
```

### Troubleshooting

**Services not starting:**
- Ensure all dependencies are installed (`npm install` in frontend)
- Check that ports 3000, 5001, 7001, and 15000 are available
- Review logs in the Aspire dashboard

**Build errors:**
- Run `dotnet restore` in the src directory
- Ensure .NET 8.0 SDK is installed: `dotnet --version`

**Frontend errors:**
- Check Node.js version: `node --version` (should be 20+)
- Clear Next.js cache: `cd frontend && rm -rf .next`

---


### Option 4: Install Into Existing Project using Manual Script

Transform any existing project into a spec2cloud-enabled development environment:

**One-Line Install** (Recommended):
```bash
curl -fsSL https://raw.githubusercontent.com/EmeaAppGbb/spec2cloud/main/scripts/quick-install.sh | bash
```

**Manual Install**:
```bash
# Download latest release
curl -L https://github.com/EmeaAppGbb/spec2cloud/releases/latest/download/spec2cloud-full-latest.zip -o spec2cloud.zip
unzip spec2cloud.zip -d spec2cloud
cd spec2cloud

# Run installer
./scripts/install.sh --full                    # Linux/Mac
.\scripts\install.ps1 -Full                    # Windows

# Start using workflows
code .
# Use @pm, @dev, @azure agents and /prd, /frd, /plan, /deploy prompts
```

**What Gets Installed**:
- ✅ 8 specialized AI agents (PM, Dev Lead, Dev, Azure, Rev-Eng, Modernizer, Planner, Architect)
- ✅ 13 workflow prompts
- ✅ MCP server configuration (optional)
- ✅ Dev container setup (optional)
- ✅ APM configuration (optional)

See **[INTEGRATION.md](INTEGRATION.md)** for detailed installation options and troubleshooting.


## 📚 Documentation

Longer guides are in the `docs/` folder (MkDocs-ready structure).

- Docs index: [docs/index.md](docs/index.md)
- Shell baselines: [docs/shells.md](docs/shells.md)
- Architecture: [docs/architecture.md](docs/architecture.md)
- Workflows: [docs/workflows.md](docs/workflows.md)
- Generated docs structure: [docs/specs-structure.md](docs/specs-structure.md)
- Standards / APM: [docs/apm.md](docs/apm.md)
- Examples: [docs/examples.md](docs/examples.md)
- Benefits: [docs/benefits.md](docs/benefits.md)

For installation/integration scenarios, see [INTEGRATION.md](INTEGRATION.md).

## 🧪 Testing

The Vehicle Search application has comprehensive test coverage across multiple levels:

### Test Types

**1. Unit Tests**
- **Backend:** xUnit tests for all core services (Coverage target: ≥85%)
- **Frontend:** Jest + React Testing Library tests for components (Coverage target: ≥80%)

**2. Integration Tests**
- End-to-end flow validation
- FRD (Functional Requirements Document) acceptance tests
- All 6 FRDs validated with automated tests

**3. E2E Tests (Playwright)**
- 24 test scenarios across 4 suites
- Multi-browser testing (Chrome, Firefox, Mobile)
- Tests for search flows, conversation context, safety guardrails, and user interactions

**4. Performance Tests**
- BenchmarkDotNet for backend performance
- Targets: Simple search <500ms, Semantic search <2s, Hybrid search <3s

**5. Load Tests (k6)**
- Tests with 100 concurrent users
- Load profile: 0→50→100 users over 8 minutes
- Thresholds: P95 <3s, Error rate <1%

### Quick Start

**Run all tests:**
```bash
# Backend unit tests
npm run test

# Frontend unit tests  
npm run test:frontend

# E2E tests (all browsers)
npm run test:e2e

# E2E tests (specific browser)
npm run test:e2e:chromium
npm run test:e2e:firefox
npm run test:e2e:mobile

# E2E tests with UI (interactive mode)
npm run test:e2e:ui

# Performance benchmarks
cd tests/VehicleSearch.Performance.Tests
dotnet run -c Release

# Load tests (requires backend running)
k6 run tests/load/search-load.js
```

### Test Coverage

Current coverage reports are generated automatically in CI/CD and can be viewed locally:

**Backend Coverage:**
```bash
cd src
dotnet test --collect:"XPlat Code Coverage"
reportgenerator -reports:**/coverage.cobertura.xml -targetdir:coverage-report
# Open coverage-report/index.html
```

**Frontend Coverage:**
```bash
cd frontend
npm test -- --coverage
# Open coverage/index.html
```

### CI/CD Integration

All tests run automatically on:
- Every push to `main`, `develop`, or `copilot/**` branches
- Every pull request

**Quality Gates:**
- ✅ All unit tests must pass
- ✅ Backend coverage ≥85%
- ✅ Frontend coverage ≥80%
- ✅ All E2E tests must pass on Chrome, Firefox, and Mobile
- ✅ Integration tests must pass

### Documentation

For detailed testing instructions, see:
- **[Test Execution Guide](docs/TEST_EXECUTION.md)** - Complete testing documentation
- **[Performance Report](docs/PERFORMANCE_REPORT.md)** - Performance benchmarks and analysis

## 🤝 Contributing

Contributions welcome! Extend with additional agents, prompts, or MCP servers.

---

**From idea to production in minutes, not months.** 🚀
