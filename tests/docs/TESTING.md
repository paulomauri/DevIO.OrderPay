# DevIO.OrderPay — Test Guide

## Overview

The project has **three** test suites:

| Suite | Location | Stack | How to run |
|---|---|---|---|
| Backend (unit/integration) | `tests/DevIO.OrderPay.Tests/` | xUnit, FluentAssertions, Moq, NetArchTest, EF InMemory | [`dotnet test`](#running-backend-tests-net) |
| Frontend (component) | `orderpay-web/src/**/*.test.tsx` | Jest + React Testing Library | [Node container](#frontend-tests-jest--react-testing-library) |
| End-to-end | `tests/e2e/` | Playwright (real browser → full stack) | [containerised runner](#end-to-end-tests-playwright) |

> **Why containers for the JS suites?** This dev machine has only Windows Node via WSL
> interop (`node` isn't on the Linux PATH), so Jest and Playwright run inside Docker. If you
> have a Linux Node, you can run them natively too (noted in each section).

The backend project uses **xUnit** as the test runner, **FluentAssertions** for readable assertions, **Moq** for mocking, **NetArchTest.Rules** for architecture enforcement, and **EF Core InMemory** for integration/repository tests.

---

## Test Types

### 1. Unit Tests — Domain

**Location:** `Domain/`

Tests pure domain logic with no external dependencies.

| File | What it covers |
|---|---|
| `Domain/OrderTests.cs` | Order aggregate — AddItem, RemoveItem, DuplicateOrderItemException |
| `Domain/OrderItemTests.cs` | OrderItem construction and value defaults |
| `Domain/ProductTests.cs` | Product construction |
| `Domain/PriceTests.cs` | Price value object — rejects negative values |
| `Domain/CustomerTests.cs` | Customer aggregate — Update, Email/Address |
| `Domain/EmailTests.cs` | Email value object — format validation |

**Rules:** No I/O, no mocks, no database. Pure in-memory objects only.

---

### 2. Unit Tests — Application Services

**Location:** `Application/`

Tests service logic with all dependencies mocked via Moq.

| File | What it covers |
|---|---|
| `Application/OrderServiceTests.cs` | GetById, Add, Delete, UpdateStatus, UpdateDeliveryDate, AddItem, RemoveItem |
| `Application/ProductServiceTests.cs` | GetById, GetAll, Add, Update, Delete, UpdateSku |
| `Application/CustomerServiceTests.cs` | GetById, GetAll, Add (duplicate CPF), Update, Delete |
| `Application/OrderRequestValidatorTests.cs` | CustomerId, Items, Quantity, Price, Discount boundaries |
| `Application/ProductRequestValidatorTests.cs` | Name, SKU, Description validation |
| `Application/CustomerRequestValidatorTests.cs` | CPF, Email, Name validation |

**Rules:** All repository calls use `Mock<IRepository>`. Verify that repository methods are called with `Times.Once`. Test both happy path and unhappy path (null, duplicates, not found).

---

### 3. Unit Tests — Controllers

**Location:** `WebApi/`  
**Files:** `OrderControllerTests.cs`, `ProductControllerTests.cs`, `CustomerControllerTests.cs`

Tests HTTP response mapping with all service calls mocked.

Covers:
- 200 OK with correct response body
- 404 Not Found when service returns null
- 409 Conflict on domain exceptions (DuplicateCpfException, DuplicateOrderItemException)
- 400 Bad Request on validation exceptions (ValueLowerThanZeroException)

---

### 4. Integration Tests — Controllers

**Location:** `WebApi/`  
**Files:** `OrderControllerIntegrationTests.cs`, `ProductControllerIntegrationTests.cs`, `CustomerControllerIntegrationTests.cs`, `RateLimitingIntegrationTests.cs`

Spins up the real ASP.NET Core pipeline against an **EF Core InMemory** database. No mocks for the database layer.

Uses `OrderPayWebApplicationFactory` (`Infrastructure/`) which:
- Replaces SQL Server with InMemory via `UseInMemoryDatabase`
- Provides `CreateClientWithRoles(params string[] roles)` for auth-protected endpoints

Covers:
- 401 Unauthorized — no token
- 403 Forbidden — wrong role (`admin` vs `customer`)
- 400 Bad Request — FluentValidation failures
- 201 Created — full request/response roundtrip
- 200 OK — read operations
- 404 Not Found — non-existing IDs
- 409 Conflict — duplicate CPF, duplicate order item
- 429 Too Many Requests — rate-limiting policies (`RateLimitingIntegrationTests.cs`); the
  limiter is disabled by default in the factory and re-enabled per-test via
  `PostConfigure<RateLimiterSettings>`

---

### 5. Repository Integration Tests

**Location:** `Repository/`  
**Files:** `OrderRepositoryTests.cs`, `ProductRepositoryTests.cs`

Tests EF Core repository implementations directly against InMemory database. Each test creates its own isolated database (`Guid.NewGuid()` as the DB name).

Covers:
- Price value object persistence (stored as decimal, read back as Price)
- Cascade delete (Order → OrderItems)
- Eager loading (GetByIdAsync includes Items)
- AddOrderItemAsync / RemoveOrderItemAsync explicit state management
- UpdateStatusAsync / UpdateDeliveryDateAsync / UpdateSkuAsync

---

### 6. Architecture Tests

**Location:** `Architecture/ArchitectureTests.cs`

Uses **NetArchTest.Rules** to enforce layer dependency rules at build time.

Rules enforced:

| Rule | What is checked |
|---|---|
| Domain has no EF Core | `Customer` and `Order` assemblies must not reference `Microsoft.EntityFrameworkCore` |
| Domain has no ASP.NET Core | Domain must not reference `Microsoft.AspNetCore` |
| Domain has no Application layer | Domain must not reference its own application project |
| Application has no EF Core | Application layers must not reference EF Core |
| Application has no ASP.NET Core | Application layers must not reference the HTTP stack |
| Application has no Infrastructure | Application must not reference `DevIO.OrderPay.Infra` |
| Infrastructure has no Application | Infra must not reference Application projects |
| Infrastructure has no WebApi | Infra must not reference WebApi |
| Interfaces start with `I` | All interfaces across all layers must be named `IFoo` |
| Repositories live in Infra.Repositories | No concrete repositories outside Infrastructure |
| Services live in Application.Services | No concrete services outside Application |
| Validators live in Application.Validators | No validators outside Application |
| Domain exceptions live in Domain.Exceptions | No domain exceptions outside Domain |

---

### 7. Resilience Tests (Polly)

**Location:** `Resilience/PollyResilienceTests.cs`

Tests the Polly pipeline configured for Keycloak backchannel calls (Retry → Circuit Breaker → Timeout).

Uses isolated pipeline instances with zero/minimal delays so tests run fast:
- `BuildRetryOnly()` — `minimumThroughput: 1000` prevents CB from opening during retry tests
- `BuildCircuitBreakerOnly()` — `maxRetryAttempts: 0` so each failure counts as one call
- `BuildTimeoutOnly()` — isolated timeout with no CB or retry

Covers:
- Retry on `HttpRequestException` (3 attempts)
- Retry on 5xx responses
- No retry on 4xx responses
- Circuit breaker opens after N failures
- Circuit breaker rejects calls while open
- Timeout fires after configured duration
- Request completes within timeout

---

## Infrastructure

**Location:** `Infrastructure/`

| File | Purpose |
|---|---|
| `OrderPayWebApplicationFactory.cs` | `WebApplicationFactory<Program>` — replaces SQL Server with InMemory, provides `CreateClientWithRoles()` |
| `FakeAuthHandler.cs` | Custom `AuthenticationHandler` — injects roles into `ClaimsPrincipal` without Keycloak |

---

## Running backend tests (.NET)

### Run all tests
```bash
dotnet test tests/DevIO.OrderPay.Tests
```

### Run a specific category
```bash
# Unit tests only (Domain + Application + Controller unit tests)
dotnet test tests/DevIO.OrderPay.Tests --filter "FullyQualifiedName~Domain|FullyQualifiedName~Application"

# Integration tests only
dotnet test tests/DevIO.OrderPay.Tests --filter "FullyQualifiedName~IntegrationTests"

# Repository tests
dotnet test tests/DevIO.OrderPay.Tests --filter "FullyQualifiedName~Repository"

# Architecture tests
dotnet test tests/DevIO.OrderPay.Tests --filter "FullyQualifiedName~Architecture"

# Resilience tests
dotnet test tests/DevIO.OrderPay.Tests --filter "FullyQualifiedName~Resilience"
```

### Run a single test class
```bash
dotnet test tests/DevIO.OrderPay.Tests --filter "FullyQualifiedName~OrderServiceTests"
```

### Run a single test method
```bash
dotnet test tests/DevIO.OrderPay.Tests --filter "FullyQualifiedName=DevIO.OrderPay.Tests.Application.OrderServiceTests.AddAsync_ValidRequest_CreatesOrderWithCorrectTotals"
```

### With detailed output
```bash
dotnet test tests/DevIO.OrderPay.Tests --logger "console;verbosity=detailed"
```

### With code coverage
```bash
dotnet test tests/DevIO.OrderPay.Tests --collect:"XPlat Code Coverage"
# Report at: TestResults/<guid>/coverage.cobertura.xml
```

---

## Mutation Tests (Stryker.NET)

Stryker deliberately breaks production code one mutation at a time and checks if tests catch it.

### Install (one-time)
```bash
dotnet tool install -g dotnet-stryker
```

### Run against Order.Application
```bash
dotnet-stryker \
  --config-file tests/DevIO.OrderPay.Tests/stryker-config.json \
  --project "DevIO.OrderPay.Order.Application.csproj"
```

### Run against Customer.Application
```bash
dotnet-stryker \
  --config-file tests/DevIO.OrderPay.Tests/stryker-config.json \
  --project "DevIO.OrderPay.Customer.Application.csproj"
```

### View HTML report
```bash
# Find the latest report path
find StrykerOutput -name "mutation-report.html" | sort | tail -1
```
Open that path in a browser. Each file shows mutants color-coded:
- **Green (Killed)** — tests caught the mutation ✅
- **Red (Survived)** — tests missed it — gap to fill
- **Yellow (No Coverage)** — no test even executes that code
- **Grey (Timeout)** — mutation caused an infinite loop

### Score thresholds (configured in `stryker-config.json`)
| Score | Label |
|---|---|
| ≥ 80% | High (green) |
| 60–79% | Low (yellow) |
| < 60% | Break (fails the run) |

Current score: **84.21%** (High) — `DevIO.OrderPay.Order.Application`

---

## Frontend Tests (Jest + React Testing Library)

Component/unit tests for the Next.js app. Files live next to source as `*.test.tsx`
(`Button`, `Badge`/`OrderStatusBadge`, `AdminOnly`, `useIsAdmin`, `CustomersPage`).

The production `orderpay-web` image doesn't ship the source/test files, so run Jest in a
throwaway Node container that mounts the frontend source:

```bash
# from the repo root — run ALL frontend tests
docker run --rm -v "$PWD/orderpay-web:/app" -w /app node:22-bookworm-slim \
  sh -c "npm ci && npx jest --ci"
```

Variations (change the part after `npm ci &&`):

```bash
# a single file
... sh -c "npm ci && npx jest Badge"

# a single test by name
... sh -c "npm ci && npx jest -t 'shows Spinner when loading'"

# with coverage
... sh -c "npm ci && npx jest --coverage"
```

Watch mode (interactive — note the `-it` and the cached node_modules volume so re-installs
are skipped):

```bash
docker run --rm -it \
  -v "$PWD/orderpay-web:/app" -v orderpay_web_node:/app/node_modules \
  -w /app node:22-bookworm-slim \
  sh -c "npm ci && npx jest --watch"
```

**With a Linux Node on the host** you can skip Docker:
```bash
cd orderpay-web && npm install && npm test
```

---

## End-to-End Tests (Playwright)

Drives a real browser through the entire running stack
(`browser → nginx → Next.js → WebApi → Keycloak → SQL Server`). 10 specs: auth, customers,
products, orders, payments. See [`tests/e2e/README.md`](../e2e/README.md) for structure.

**Prerequisite — the stack must be up and healthy:**
```bash
docker compose up -d
docker compose ps        # wait until keycloak = healthy
```

**Run the whole suite** (containerised runner — shares nginx's network namespace, so no host
Node or host DNS is needed):
```bash
docker compose -f docker-compose.yml -f docker-compose.e2e.yml run --rm playwright
```

**Run a subset** — override the command, keeping the `/etc/hosts` patch + wait-on:
```bash
# one spec file
docker compose -f docker-compose.yml -f docker-compose.e2e.yml run --rm playwright \
  sh -c "echo '127.0.0.1 www.localhost api.localhost id.localhost' >> /etc/hosts \
         && npm install && npx wait-on http://www.localhost --timeout 120000 \
         && npx playwright test customers.spec.ts"

# by title substring
docker compose -f docker-compose.yml -f docker-compose.e2e.yml run --rm playwright \
  sh -c "echo '127.0.0.1 www.localhost api.localhost id.localhost' >> /etc/hosts \
         && npm install && npx wait-on http://www.localhost --timeout 120000 \
         && npx playwright test -g 'creates an order'"
```

**CI / one-shot with exit code:**
```bash
docker compose -f docker-compose.yml -f docker-compose.e2e.yml \
  up --exit-code-from playwright --abort-on-container-exit playwright
```

**Results** (written to the bind-mounted host folder):
```
tests/e2e/playwright-report/    # HTML report — open index.html
tests/e2e/test-results/         # screenshots + trace.zip on failure
```
Open the report from WSL: `explorer.exe tests/e2e/playwright-report`.

> The runner is headless. Debug failures via the trace viewer / screenshots — `--ui` and
> `--headed` need a Linux Node + display on the host (not available on this machine).

**With a Linux Node on the host:**
```bash
cd tests/e2e && npm install && npx playwright install chromium && npx playwright test
```

---

## Test Count Summary

### Backend — .NET (`tests/DevIO.OrderPay.Tests`)

| Category | Count |
|---|---|
| Domain unit tests | ~30 |
| Application service unit tests | ~35 |
| Controller unit tests | ~20 |
| Controller integration tests | ~55 |
| Repository integration tests | 22 |
| Architecture tests | 20 |
| Resilience (Polly) tests | 9 |
| Rate limiting integration tests | 4 |
| Payment tests (18 unit + 5 integration) | 23 |
| Phase 8 — order state machine + domain events, outbox interceptor, order event handlers, idempotent consumer dedup | 18 |
| **Total** | **239** |

Per-category numbers are approximate (`[Theory]` cases expand via `[InlineData]`); the
executed total is **239/239**.

### Frontend & E2E (JavaScript)

| Suite | Count |
|---|---|
| Jest component tests (`orderpay-web`) | 30 |
| Playwright E2E specs (`tests/e2e`) | 10 |
