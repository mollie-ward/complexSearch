# Test Execution Guide

This document provides comprehensive instructions for executing all tests in the Vehicle Search System.

## Table of Contents

- [Prerequisites](#prerequisites)
- [Test Categories](#test-categories)
- [Running Tests Locally](#running-tests-locally)
- [CI/CD Pipeline](#cicd-pipeline)
- [Test Coverage](#test-coverage)
- [Troubleshooting](#troubleshooting)

## Prerequisites

### Required Software

- **.NET 8.0 SDK** or later
- **Node.js 20+** and npm
- **Docker** (optional, for containerized testing)
- **k6** (for load testing)
- **Playwright browsers** (auto-installed)

### Environment Setup

1. **Clone the repository:**
   ```bash
   git clone https://github.com/mollie-ward/complexSearch.git
   cd complexSearch
   ```

2. **Install dependencies:**
   ```bash
   # Root level (Playwright)
   npm install
   
   # Frontend dependencies
   cd frontend
   npm install
   cd ..
   
   # .NET dependencies
   cd src
   dotnet restore
   cd ..
   ```

3. **Install Playwright browsers:**
   ```bash
   npx playwright install
   ```

4. **Install k6** (for load testing):
   ```bash
   # macOS
   brew install k6
   
   # Ubuntu/Debian
   sudo gpg -k
   sudo gpg --no-default-keyring --keyring /usr/share/keyrings/k6-archive-keyring.gpg --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys C5AD17C747E3415A3642D57D77C6C491D6AC1D69
   echo "deb [signed-by=/usr/share/keyrings/k6-archive-keyring.gpg] https://dl.k6.io/deb stable main" | sudo tee /etc/apt/sources.list.d/k6.list
   sudo apt-get update
   sudo apt-get install k6
   
   # Windows
   choco install k6
   ```

## Test Categories

### 1. Unit Tests

**Backend Unit Tests:**
- **Location:** `tests/VehicleSearch.*.Tests/`
- **Framework:** xUnit
- **Coverage:** Core business logic, services, utilities

**Frontend Unit Tests:**
- **Location:** `frontend/tests/`
- **Framework:** Jest + React Testing Library
- **Coverage:** Components, hooks, utilities

### 2. Integration Tests

**Location:** `tests/VehicleSearch.Integration.Tests/`
**Framework:** xUnit
**Coverage:** 
- End-to-end flow tests
- FRD acceptance tests (6 FRDs)
- System integration validation

### 3. E2E Tests

**Location:** `tests/e2e/`
**Framework:** Playwright
**Coverage:**
- Search flow (TC-001 to TC-004)
- Conversation flow (TC-005 to TC-006)
- Safety guardrails (TC-007 to TC-010)
- User interactions (TC-011 to TC-013)

### 4. Performance Tests

**Location:** `tests/VehicleSearch.Performance.Tests/`
**Framework:** BenchmarkDotNet
**Benchmarks:**
- Simple exact search (<500ms target)
- Semantic search (<2s target)
- Hybrid search (<3s target)
- Conversation context

### 5. Load Tests

**Location:** `tests/load/`
**Framework:** k6
**Profile:** 0→50→100 concurrent users over 8 minutes

## Running Tests Locally

### Backend Unit Tests

```bash
# Run all backend tests
cd src
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test project
dotnet test tests/VehicleSearch.Core.Tests

# Run with verbose output
dotnet test --logger "console;verbosity=detailed"
```

### Frontend Unit Tests

```bash
# Run all frontend tests
cd frontend
npm test

# Run with coverage
npm test -- --coverage

# Run in watch mode
npm test -- --watch

# Run specific test file
npm test -- SearchInput.test.tsx
```

### Integration Tests

```bash
# Run integration tests
cd tests/VehicleSearch.Integration.Tests
dotnet test

# Run specific test class
dotnet test --filter "FullyQualifiedName~EndToEndFlowTests"

# Run with detailed output
dotnet test --logger "console;verbosity=detailed"
```

### E2E Tests (Playwright)

```bash
# Run all E2E tests (all browsers)
npm run test:e2e

# Run with UI mode (interactive)
npm run test:e2e:ui

# Run specific browser
npm run test:e2e:chromium
npm run test:e2e:firefox
npm run test:e2e:mobile

# Run specific test file
npx playwright test tests/e2e/search-flow.spec.ts

# Run in debug mode
npm run test:e2e:debug

# View test report
npm run test:e2e:report
```

### Performance Benchmarks

```bash
# Run performance benchmarks
cd tests/VehicleSearch.Performance.Tests
dotnet run -c Release

# View results
# Results are saved to BenchmarkDotNet.Artifacts/results/
```

### Load Tests

```bash
# Run load test (requires backend API running)
k6 run tests/load/search-load.js

# Run with custom parameters
k6 run tests/load/search-load.js -e API_URL=http://localhost:5001

# Run specific scenario
# Edit search-load.js to use spikeOptions, stressOptions, or soakOptions
```

## Test Execution Order

### Recommended Local Testing Workflow

1. **Unit Tests First** (fastest feedback):
   ```bash
   # Backend
   cd src && dotnet test
   
   # Frontend
   cd frontend && npm test
   ```

2. **Integration Tests** (medium speed):
   ```bash
   cd tests/VehicleSearch.Integration.Tests
   dotnet test
   ```

3. **E2E Tests** (slowest, most realistic):
   ```bash
   npm run test:e2e:chromium  # Start with one browser
   ```

4. **Performance Tests** (optional, for performance validation):
   ```bash
   cd tests/VehicleSearch.Performance.Tests
   dotnet run -c Release
   ```

5. **Load Tests** (optional, requires running backend):
   ```bash
   # Terminal 1: Start backend
   npm run dev:api
   
   # Terminal 2: Run load test
   k6 run tests/load/search-load.js
   ```

## CI/CD Pipeline

### Automated Testing in CI

The CI/CD pipeline (`github/workflows/ci.yml`) automatically runs all tests on:
- Push to `main`, `develop`, or `copilot/**` branches
- Pull requests to `main` or `develop`

**Pipeline Jobs:**

1. **backend-tests**
   - Runs .NET unit tests
   - Collects code coverage
   - Enforces ≥85% coverage threshold
   - Generates HTML coverage report

2. **frontend-tests**
   - Runs Jest tests
   - Collects code coverage
   - Enforces ≥80% coverage threshold
   - Runs linter

3. **e2e-tests**
   - Runs Playwright tests on 3 browsers (Chromium, Firefox, Mobile)
   - Parallel execution with matrix strategy
   - Captures screenshots/videos on failure
   - Uploads test artifacts

4. **integration-tests**
   - Runs C# integration tests
   - Validates end-to-end flows
   - Tests FRD acceptance criteria

5. **coverage-upload**
   - Uploads coverage to Codecov
   - Comments on PR with coverage report

6. **build-status**
   - Final status check
   - Fails if any test job fails
   - Posts summary to GitHub Actions

### Viewing CI Results

- **GitHub Actions Tab:** View detailed logs for each job
- **PR Comments:** Coverage report automatically posted
- **Artifacts:** Download test reports, screenshots, videos
- **Status Checks:** Required checks must pass before merge

## Test Coverage

### Coverage Thresholds

| Component | Threshold | Current | Status |
|-----------|-----------|---------|--------|
| Backend   | ≥85%      | TBD     | 🔄     |
| Frontend  | ≥80%      | TBD     | 🔄     |

### Generating Coverage Reports

**Backend:**
```bash
cd src
dotnet test --collect:"XPlat Code Coverage"
reportgenerator \
  -reports:**/coverage.cobertura.xml \
  -targetdir:coverage-report \
  -reporttypes:Html
  
# Open coverage-report/index.html
```

**Frontend:**
```bash
cd frontend
npm test -- --coverage --coverageReporters=html

# Open coverage/index.html
```

### Viewing Coverage

- **Local:** Open generated HTML reports in browser
- **CI:** Download artifacts from GitHub Actions
- **Codecov:** View at https://codecov.io/gh/mollie-ward/complexSearch

## Test Data

### Sample Data Files

- **sampleData.csv:** 60 sample vehicles for indexing
- **sample_vehicles.csv:** Simplified test data

### Test Environment

**Backend:**
- API: `http://localhost:5001`
- Swagger: `http://localhost:5001/swagger`

**Frontend:**
- Dev Server: `http://localhost:3000`

**Azure Services (Test Environment):**
- Azure AI Search: Test index
- Azure OpenAI: Test deployment (with budget limits)
- Redis: Local or test instance

## Troubleshooting

### Common Issues

**1. Playwright browsers not installed**
```bash
npx playwright install
```

**2. Frontend tests fail with "Cannot find module"**
```bash
cd frontend
rm -rf node_modules package-lock.json
npm install
```

**3. Backend tests fail with missing dependencies**
```bash
cd src
dotnet clean
dotnet restore
dotnet build
```

**4. E2E tests timeout**
- Increase timeout in `playwright.config.ts`
- Check if dev server is running
- Verify network connectivity

**5. Load tests fail with connection errors**
- Ensure backend API is running: `npm run dev:api`
- Check API URL: `k6 run tests/load/search-load.js -e API_URL=http://localhost:5001`

**6. Coverage below threshold**
- Add more unit tests
- Review coverage report to identify untested code
- Focus on critical paths first

### Getting Help

- **GitHub Issues:** https://github.com/mollie-ward/complexSearch/issues
- **Documentation:** See `docs/` directory
- **CI Logs:** Check GitHub Actions for detailed error messages

## Best Practices

### Before Committing

1. Run unit tests: `dotnet test && cd frontend && npm test`
2. Run linter: `cd frontend && npm run lint`
3. Run E2E tests: `npm run test:e2e:chromium` (at minimum)
4. Check coverage: Ensure new code has tests

### Before Creating PR

1. Run all tests locally
2. Verify coverage thresholds met
3. Review test failures in CI
4. Update tests if changing functionality

### Continuous Testing

- Use watch mode for rapid iteration: `npm test -- --watch`
- Run relevant tests frequently during development
- Fix failing tests immediately (don't accumulate)

## Performance Targets

### Response Times (P95)

- **Simple Exact Search:** <1 second
- **Complex Semantic Search:** <3 seconds
- **Hybrid Search:** <3 seconds

### Load Testing Targets

- **Concurrent Users:** 100
- **Error Rate:** <1%
- **Successful Searches:** >95%

### Memory

- **Simple Search:** <10MB allocated
- **Semantic Search:** <50MB allocated
- **Hybrid Search:** <100MB allocated
- **No memory leaks:** Stable across iterations

## Appendix

### Test Execution Checklist

**Pre-Release Validation:**

- [ ] All unit tests pass (backend + frontend)
- [ ] All integration tests pass
- [ ] All E2E tests pass (all browsers)
- [ ] Coverage thresholds met (backend ≥85%, frontend ≥80%)
- [ ] Performance benchmarks within targets
- [ ] Load tests handle 100 concurrent users
- [ ] No critical bugs
- [ ] All FRD acceptance tests pass
- [ ] Documentation updated
- [ ] CI pipeline green

**Production Deployment:**

- [ ] Final E2E test run against staging
- [ ] Load test against staging environment
- [ ] Smoke tests configured for production
- [ ] Monitoring and alerts configured
- [ ] Rollback plan in place

---

**Last Updated:** 2026-01-28  
**Version:** 1.0  
**Maintained by:** Development Team
