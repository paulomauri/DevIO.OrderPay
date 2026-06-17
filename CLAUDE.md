# DevIO.OrderPay

Study project — .NET 10 Clean Architecture API exploring DevIO patterns with observability and containerization.

## Stack

- **API:** ASP.NET Core 10, EF Core (SQL Server), FluentValidation, ASP.NET Core Rate Limiting
- **Auth:** Keycloak (JWT/OAuth2) — clients `orderpay-swagger` (issues tokens) + `orderpay-webapi` (bearer-only)
- **Observability:** Serilog + Seq, OpenTelemetry
- **Infrastructure:** Docker Compose, Kubernetes (Minikube), SQL Server 2025

## Architecture — 4 layers

```
DevIO.OrderPay.Core                  # Shared abstractions (IRepository, ICustomerRepository, IOrderRepository, IProductRepository)
DevIO.OrderPay.Customer              # Domain — Customer, Email/Address value objects, DuplicateCpfException
DevIO.OrderPay.Customer.Application  # Application — CustomerService, validators, DTOs
DevIO.OrderPay.Order                 # Domain — Order, OrderItem, Product, Price value object, OrderStatus, DuplicateOrderItemException
DevIO.OrderPay.Order.Application     # Application — OrderService, ProductService, validators, DTOs
DevIO.OrderPay.Infra                 # Infrastructure — EF Core, AppDbContext, migrations, all repositories
DevIO.OrderPay.WebApi                # API — controllers, extensions, auth middleware, Program.cs
```

Bounded contexts live under `src/Contexts/`. Shared infrastructure under `src/Apps/`.

## Backend Development Conventions

### Architecture rules — what goes where

| Layer | Allowed dependencies | Forbidden |
|---|---|---|
| Domain (`Customer`) | nothing | EF Core, HTTP, Application |
| Application (`Customer.Application`) | Domain, Core interfaces | EF Core, HTTP, DbContext |
| Infrastructure (`Infra`) | Domain, Core interfaces, EF Core | Application, WebApi |
| WebApi | Application, Infra (DI only) | Direct domain rule enforcement |

### Error handling pattern

- Domain exceptions (e.g. `DuplicateCpfException`) are thrown by services, never by controllers
- Controllers catch domain exceptions and map to HTTP status codes (409, 404, etc.)
- FluentValidation handles input validation — runs automatically before the action method
- Never return raw exceptions to the client — always map to a consistent error response

### Naming conventions

- Interfaces: `I<Name>` — `ICustomerService`, `ICustomerRepository`
- Domain exceptions: `<Rule>Exception` — `DuplicateCpfException`
- Validators: `<Request>Validator` — `CustomerRequestValidator`
- DTOs: `<Entity>Request` / `<Entity>Response`
- Repository methods: async suffix — `GetByIdAsync`, `ExistsByCpfAsync`

### Domain model rules

- Models do NOT enforce business rules — no validation logic inside setters or constructors (except value objects)
- Value objects validate format on construction and throw `ArgumentException` on invalid input
- New value objects live in the domain project alongside the model that uses them

### Testing conventions

- Unit tests: pure, no I/O, mock all dependencies via `Moq`
- Integration tests: `OrderPayWebApplicationFactory` + EF InMemory — no mocks for DB
- `FakeAuthHandler` injects roles for controller-level auth tests
- One test class per production class; test method name: `Method_Scenario_ExpectedResult`
- Always test the unhappy path (duplicate CPF, not found, invalid input)

### General rules

- No comments unless the WHY is non-obvious
- No backwards-compatibility shims — delete unused code
- No error handling for scenarios that cannot happen
- Prefer editing existing files over creating new ones
- Keycloak roles: `admin` → full CRUD, `customer` → GET only
- `HostRewritingHandler` in `KeycloakExtensions.cs` rewrites `id.localhost → keycloak:8085` for JWKS backchannel inside Docker/K8s — only activates when `Keycloak:MetadataAddress` config key is present. It matches on **host** and swaps the whole authority (scheme/host/port), so a ported public URL never yields a double-port target.

## Keycloak realm & clients

- `keycloak/setup.sh` is the **single source of truth** for all clients (`orderpay-webapi` bearer-only, `orderpay-swagger` public+direct-grant, `orderpay-web` confidential), their audience mappers (`aud: orderpay-webapi`), and the seed users. The `keycloak-setup` job runs it once after Keycloak is healthy.
- `keycloak/realm-export.json` (imported via `--import-realm`) is only the realm shell — realm settings + `admin`/`customer` roles. It deliberately defines **no** clients/users so it can't shadow `setup.sh`.
- **`KC_HOSTNAME` must be a full URL** (`http://id.localhost`), not a bare host. A bare host makes Keycloak stamp the request port into the discovery doc (`jwks_uri: http://id.localhost:8085/...`); the `HostRewritingHandler` rewrite then builds the invalid `http://keycloak:8085:8085/...` → JWKS fetch fails → `IDX10500` → 401 → frontend `/login` loop. The full URL keeps issuer + `jwks_uri` port-free.

## Commands

### Run locally
```bash
dotnet run --project src/Apps/DevIO.OrderPay.WebApi
```

### Tests
```bash
dotnet test tests/DevIO.OrderPay.Tests
```

### Docker Compose
```bash
docker compose up -d                   # start all services
docker compose down -v                 # stop + remove volumes (fresh start)
docker compose logs -f devio.orderpay.webapi
```

### Build & push images
```bash
# Backend
docker build \
    -t paulomauri/orderpay-webapi:1.0.5 \
    -t paulomauri/orderpay-webapi:latest \
    -f src/Apps/DevIO.OrderPay.WebApi/Dockerfile .

docker push paulomauri/orderpay-webapi:1.0.5
docker push paulomauri/orderpay-webapi:latest

# Frontend (NEXT_PUBLIC_API_URL="" → same-origin via nginx)
docker build \
    -t paulomauri/orderpay-web:1.0.1 \
    -t paulomauri/orderpay-web:latest \
    --build-arg NEXT_PUBLIC_API_URL="" \
    -f orderpay-web/Dockerfile \
    orderpay-web/

docker push paulomauri/orderpay-web:1.0.1
docker push paulomauri/orderpay-web:latest
```

### Kubernetes (Minikube)
```bash
# first deploy (in order)
kubectl apply -f k8s/namespace.yaml
kubectl config set-context --current --namespace=orderpay
kubectl apply -f k8s/secrets.yaml
kubectl apply -f k8s/postgres/
kubectl apply -f k8s/keycloak/
kubectl apply -f k8s/sqlserver/
kubectl apply -f k8s/seq/
kubectl apply -f k8s/webapi/
kubectl apply -f k8s/frontend/
kubectl apply -f k8s/nginx/

# expose services
minikube tunnel                        # terminal 1 — keep open

# re-run Keycloak setup Job
kubectl delete job keycloak-setup -n orderpay
kubectl apply -f k8s/keycloak/setup-job.yaml

# get JWT token
curl -s -X POST http://localhost:8085/realms/orderpay/protocol/openid-connect/token \
  -d "client_id=orderpay-swagger" \
  -d "username=admin@orderpay.com" \
  -d "password=Mauri@22" \
  -d "grant_type=password" | jq -r '.access_token'
```

## Service URLs

| Service | Docker Compose | Kubernetes |
|---|---|---|
| Frontend | `http://www.localhost` | `http://127.0.0.1` |
| Swagger | `http://api.localhost/swagger` | `http://127.0.0.1/swagger` |
| Keycloak Admin | `http://id.localhost/admin` | `http://localhost:8085/admin` |
| Keycloak (direct) | `http://localhost:8085/admin` | — |
| Seq | `http://seq.localhost:8082` | `http://127.0.0.1:8082` |

## Roadmap

| Phase | Status |
|---|---|
| 1 — Authentication | ✅ Done |
| 2 — Unit Tests (198/198) | ✅ Done |
| K8s deployment | ✅ Done |
| 3 — Orders Bounded Context | ✅ Done |
| 4 — Resilience (Polly + Rate Limiting) | ✅ Done |
| 5 — CI/CD Pipeline | ✅ Done |
| 6 — Frontend (React + Next.js + Redux) | 🔄 Steps 1–10A done / Step 10B pending |
| 7 — Payment Bounded Context + Idempotency | pending |
| 8 — Order State Machine + Domain Events + Outbox + Idempotency | pending |
| 9 — Logistics Integration (outbound dispatch + inbound webhook) | pending |
| 10 — Datadog (cloud observability) | pending |

## Phase 6 — Frontend steps

| Step | Description | Status |
|---|---|---|
| 1 | Project setup — Next.js 15, Styled Components v6, TypeScript, Dockerfile | ✅ Done |
| 2 | Theme + Global styles — `theme.ts`, `GlobalStyle.ts`, `styled.d.ts` | ✅ Done |
| 3 | Auth — NextAuth.js v4 + Keycloak OIDC, session, protected routes | ✅ Done |
| 4 | API layer — Axios instance (JWT interceptor) + React Query, typed service files | ✅ Done |
| 5 | Redux Toolkit — `uiSlice` (modals, sidebar) + `cartSlice` (draft order) | ✅ Done |
| 6 | Pages + layout — AppShell, Sidebar, Header, Dashboard, Customers, Products, Orders | ✅ Done |
| 7 | UI primitives — Button, Input, Badge, Card, Table, Modal, Spinner, AdminOnly | ✅ Done |
| 8 | Forms + validation — react-hook-form + zod, ModalManager, CRUD modals, mutations | ✅ Done |
| 9 | Error handling + loading states — toasts, skeletons, empty states, error boundary | ✅ Done |
| 10-A | Unit/component tests — Jest + React Testing Library | ✅ Done |
| 10-B | E2E tests — Playwright (`tests/e2e/` at solution root) | pending |

## Phase 6 — Step 8: Forms + validation (pending)

**Install**
- `react-hook-form`, `zod`, `@hookform/resolvers`

**Components**
- `ModalManager` — reads Redux `uiSlice.activeModal`, renders the correct modal
- `CustomerFormModal` — create + edit: name, CPF, email, street, city, zip
- `ProductFormModal` — create + edit: name, description, price, stock
- `CreateOrderModal` — select customer + add items from product list, submit
- `UpdateStatusModal` — admin selects new status from allowed transitions
- `ConfirmDeleteModal` — generic confirmation with entity name

**Pattern per modal**
- `useForm` with `zodResolver` for typed validation
- `useMutation` + `queryClient.invalidateQueries` on success
- Field-level error messages via `Input` `error` prop

## Phase 6 — Step 9: Error handling + loading states (pending)

- `ErrorBoundary` wraps `AppShell` — catches render errors, shows fallback UI
- `toast()` helper — called in mutation `onSuccess` / `onError`
- `TableSkeleton` — animated placeholder rows while `isLoading`
- `EmptyState` — shown when query returns empty array
- API 401 → `signOut()` via Axios response interceptor

## Phase 6 — Step 10-A: Unit tests — Jest + RTL (pending)

**Setup:** `jest.config.ts`, `jest.setup.ts` inside `orderpay-web/`; packages: `@testing-library/react`, `@testing-library/user-event`, `@testing-library/jest-dom`

**Test targets**
- `Button` — renders variants, shows Spinner when `loading`, disabled when `loading`
- `Badge` / `OrderStatusBadge` — correct color per status
- `AdminOnly` — hides children when `useIsAdmin()` returns false
- `useIsAdmin` — returns true/false based on mocked session roles
- `CustomersPage` — renders table rows from mocked React Query data

## Phase 6 — Step 10-B: E2E tests — Playwright (pending)

Separate project at solution root — tests the full running stack (browser → nginx → Next.js → WebApi → Keycloak → SQL Server).

**Location**
```
tests/
  DevIO.OrderPay.Tests/        ← existing .NET xUnit tests
  e2e/
    package.json
    playwright.config.ts
    specs/
      auth.spec.ts             ← login, redirect, session
      customers.spec.ts        ← CRUD + role gating
      products.spec.ts         ← CRUD + role gating
      orders.spec.ts           ← create order, status badge
    fixtures/
      auth.fixture.ts          ← reusable login helpers (admin + customer sessions)
      .auth-admin.json         ← saved browser storage state (gitignored)
      .auth-customer.json      ← saved browser storage state (gitignored)
```

**playwright.config.ts**
```ts
export default defineConfig({
  baseURL: "http://www.localhost",
  use: { headless: true, screenshot: "only-on-failure" },
  projects: [{ name: "chromium", use: { ...devices["Desktop Chrome"] } }],
  retries: 1,
});
```

**Covered scenarios**
| Spec | Scenario |
|---|---|
| `auth` | Admin logs in via Keycloak → lands on `/dashboard` |
| `auth` | Unauthenticated user redirected to Keycloak login |
| `customers` | Admin creates a customer → row appears in table |
| `customers` | Customer role sees table but no Create / Delete buttons (`AdminOnly`) |
| `products` | Admin creates + deletes a product |
| `orders` | Admin creates an order → appears with `Pending` badge |
| `orders` | Admin updates status → badge color changes |

**Auth fixture** — stores browser storage state after login so subsequent tests skip the Keycloak login form:
```ts
// fixtures/auth.fixture.ts
export async function saveAdminSession(browser: Browser) {
  const page = await browser.newPage();
  await page.goto("/");
  await page.fill("#username", "admin@orderpay.com");
  await page.fill("#password", "Mauri@22");
  await page.click("[type=submit]");
  await page.context().storageState({ path: "fixtures/.auth-admin.json" });
  await page.close();
}
```

**Run commands — local (host)**
```bash
cd tests/e2e
npm install
npx playwright install chromium
npx playwright test              # headless (requires docker compose up -d)
npx playwright test --ui         # interactive UI mode
npx playwright show-report       # HTML report after run
```

**Run commands — containerised (isolated)**
```bash
# start the full stack + playwright runner in one shot
docker compose -f docker-compose.yml -f docker-compose.e2e.yml \
  up --exit-code-from playwright --abort-on-container-exit playwright
```

**Containerised setup — `docker-compose.e2e.yml`**

The playwright service joins the same Docker network as nginx and resolves the three
subdomains via network aliases on the nginx service — no host DNS changes needed.

```yaml
services:
  nginx:
    networks:
      default:
        aliases:
          - www.localhost
          - api.localhost
          - id.localhost

  playwright:
    image: mcr.microsoft.com/playwright:v1.54-noble
    working_dir: /e2e
    volumes:
      - ./tests/e2e:/e2e
    command: >
      sh -c "npx wait-on http://www.localhost --timeout 60000 && npx playwright test"
    depends_on:
      - nginx
      - webapi
      - keycloak
```

`wait-on` waits for nginx to be reachable before running tests — needed because Keycloak
takes ~30 s to finish booting even after the container reports healthy.
`baseURL` in `playwright.config.ts` stays `http://www.localhost` — no env var needed.

**CI integration** — `playwright` job in `.github/workflows/ci.yml`:
1. `docker compose -f docker-compose.yml -f docker-compose.e2e.yml up --exit-code-from playwright --abort-on-container-exit playwright`
2. Upload HTML report (`playwright-report/`) as artifact on failure

**Dependencies**
- Keycloak must have `admin@orderpay.com` + `user@orderpay.com` (created by `setup.sh`)
- `tests/e2e/fixtures/.auth-*.json` are gitignored (generated at test runtime)
- `wait-on` npm package added to `tests/e2e/package.json` dev dependencies

## Phase 7 — Payment Bounded Context (pending)

New bounded context `DevIO.OrderPay.Payment` with its own domain, application, and infra layers.

**Domain**
- `Payment` aggregate — `PaymentStatus` state machine: `Pending → Processing → Authorized → Captured → Refunded / Failed`
- `PaymentMethod` value object — card brand, last 4 digits, expiry
- `Amount` value object
- `PaymentAttempt` entity — holds `IdempotencyKey` (composite: `orderId:attemptNumber`), persisted before gateway call
- `InvalidPaymentTransitionException`, `DuplicatePaymentAttemptException`

**Application**
- `PaymentService` — calls `IPaymentGateway` with the idempotency key; on retry, finds existing `PaymentAttempt` by key and skips the charge
- `IPaymentGateway` — abstraction over Stripe/mock; adapter receives the key and returns cached result if already processed
- Raises `PaymentCapturedEvent` on success

**Integration**
- `OrderService` reacts to `PaymentCapturedEvent` → advances `OrderStatus`: `AwaitingPayment → PaymentConfirmed → Processing`
- Advancing to `Processing` writes an `OutboxMessage` (logistics notification payload) atomically in the same EF Core transaction

**Idempotency guarantee:** payment is charged at-most-once — retrying a failed call with the same key never double-charges.

## Phase 8 — Order State Machine + Domain Events + Outbox (pending)

**Full order lifecycle**
```
Created → AwaitingPayment → PaymentConfirmed → Processing → Shipped → Delivered
                                                    ↓
                                          CancellationRequested → Refunding → Cancelled
                                          Failed
```

**State machine**
- `Order.UpdateStatus(newStatus)` validates the transition against an allowed-transitions map
- Throws `InvalidOrderTransitionException` for illegal moves (e.g. `Pending → Delivered`)
- `Order.MarkDelivered(deliveredAt, deliveredBy)` — calls `UpdateStatus(Delivered)` then sets `DeliveredAt` and `DeliveredBy`

**New Order fields**
- `DeliveredAt` (`DateTime?`) — set when status transitions to `Delivered`
- `DeliveredBy` (`string?`) — observation field: carrier name, driver, or any delivery notes

**Domain events**
- `Order` accumulates `IDomainEvent` instances in a `List<IDomainEvent>`
- Events: `PaymentConfirmedEvent`, `OrderProcessingEvent`, `OrderShippedEvent`, `OrderDeliveredEvent`, `OrderCancelledEvent`
- Application layer dispatches them after `SaveChanges`

**Outbox pattern**
- `OutboxMessage` table — written atomically in the same EF Core transaction as the aggregate save; each message has a stable GUID `Id`
- `ProcessedOutboxMessage` table — stores consumed message IDs
- `OutboxWorker` (`IHostedService`) — polls every N seconds; for `OrderProcessingEvent` messages, calls `ILogisticsClient.NotifyOrderAsync`; for other events, publishes to RabbitMQ via MassTransit; marks done after success

**Outbound logistics notification** (triggered by `OrderProcessingEvent` via Outbox)
- `ILogisticsClient` (abstraction in `Order.Application`) — `NotifyOrderAsync(order)`
- `HttpLogisticsClient` (implementation in `Infra`) — HTTP POST to configured logistics endpoint
- Payload: `OrderId`, `Items`, `ShippingAddress`, `CreatedAt`, `IdempotencyKey` (= `OutboxMessage.Id`)
- Retried automatically by the Outbox worker on failure (at-least-once delivery)

**Message broker — RabbitMQ + MassTransit**
- RabbitMQ as the message broker — exchange/queue routing for domain events
- MassTransit as the .NET abstraction — handles retries, dead-letter queues, consumer registration
- NuGet: `MassTransit`, `MassTransit.RabbitMQ`
- Consumers: `PaymentConfirmedConsumer`, `OrderProcessingConsumer`, `OrderShippedConsumer`, `OrderDeliveredConsumer`, `OrderCancelledConsumer`
- Each consumer checks `ProcessedOutboxMessage` before executing (idempotency dedup by `OutboxMessage.Id`)
- RabbitMQ container added to `docker-compose.yml` and K8s manifests

**Idempotency guarantee:** every downstream side effect runs at-least-once but is safe to repeat — dedup by `OutboxMessage.Id` against `ProcessedOutboxMessage`.

**End-to-end guarantee:** Phase 7 (at-most-once charge) + Phase 8 (at-least-once + idempotent consumers via RabbitMQ/MassTransit) = effectively-once semantics across the payment and order pipeline.

## Phase 9 — Logistics Integration (pending)

Bidirectional logistics integration: we notify the logistics company when an order is ready to ship (outbound), and they notify us of status changes (inbound).

### Outbound — Order dispatch notification

When the Outbox worker processes an `OrderProcessingEvent`:
- `ILogisticsClient.NotifyOrderAsync` sends `POST {Logistics:BaseUrl}/orders`
- Payload: `OrderId`, `Items[]`, `ShippingAddress`, `CreatedAt`, `IdempotencyKey`
- Retried by Outbox worker until logistics endpoint returns `2xx`
- `HttpLogisticsClient` configured via `Logistics:BaseUrl` and `Logistics:ApiKey` appsettings keys

### Inbound — Status update webhook

Endpoint called by the logistics company to push shipment status changes:
- `POST /api/v1/webhook/logistics` — outside Keycloak auth; verified via HMAC-SHA256 `X-Signature: sha256=<hex>` header
- Returns `202 Accepted` immediately; never exposes internal errors to the caller

**DTO**
- `LogisticsWebhookRequest` — `EventId` (string), `OrderId` (Guid), `LogisticsStatus` (string), `OccurredAt` (DateTimeOffset), `CarrierName` (string?)

**Status mapping** (`LogisticsStatus → OrderStatus`)
```
IN_TRANSIT          → Shipped
OUT_FOR_DELIVERY    → Shipped
DELIVERED           → Delivered  + DeliveredAt = OccurredAt
                                 + DeliveredBy = CarrierName
FAILED              → CancellationRequested
RETURNED            → Refunding
```

**Idempotency**
- `ProcessedWebhookEvent` table — stores consumed `EventId` values
- Before processing: check if `EventId` already exists → skip if so
- Written in the same EF Core transaction as the `Order` status update

**Application layer**
- `ILogisticsWebhookService` + `LogisticsWebhookService` in `Order.Application`
- `DELIVERED` status calls `Order.MarkDelivered(OccurredAt, CarrierName)` — sets fields + validates state machine transition
- Illegal moves throw `InvalidOrderTransitionException` → `422 Unprocessable Entity`

**Security**
- HMAC-SHA256 signature computed over raw request body with shared secret from `appsettings` / K8s secret
- IP allowlist optional — configure at nginx level for the logistics company's CIDR

### Mock logistics service (for testing)

Since there is no real logistics company, a lightweight mock lives inside the WebApi:
```
POST /api/v1/mock-logistics/receive    ← receives our outbound notification, stores it in memory
GET  /api/v1/mock-logistics/orders     ← lists received orders (debug)
POST /api/v1/mock-logistics/callback   ← simulates logistics calling our inbound webhook
```
Enables full end-to-end testing of the dispatch → shipped → delivered cycle without an external service.

**Dependencies**
- Requires Phase 8 state machine (`Order.UpdateStatus`, `Order.MarkDelivered`)
- Reuses Outbox pattern from Phase 8 for outbound delivery guarantee
- `HttpLogisticsClient` registered in `Infra`; `ILogisticsClient` abstraction in `Order.Application`

## Phase 10 — Datadog (pending)

Cloud observability alongside the existing Seq setup. Both exporters run simultaneously — Seq for local dev, Datadog for cloud dashboards, alerts, and APM flame graphs.

**Integration approach**
- No Datadog agent needed — uses the existing OpenTelemetry pipeline with a second OTLP exporter
- `OpenTelemetryExtensions.cs` gets a conditional second exporter: if `Datadog:ApiKey` config key is present, register the Datadog OTLP endpoint; otherwise skip silently (Seq-only local dev)

**What gets exported**
- Traces → Datadog APM (service map, flame graphs, latency percentiles per endpoint)
- Metrics → Infrastructure + custom (request count, error rate, DB query duration)
- Logs → via Serilog Datadog sink (`Serilog.Sinks.Datadog.Logs`) — correlated to traces via `dd.trace_id`

**Changes**
- `OpenTelemetryExtensions.cs` — add `AddOtlpExporter` pointing to `https://otlp.datadoghq.com:4317` with `DD-API-KEY` header
- `SerilogExtensions.cs` — add `WriteTo.DatadogLogs(apiKey)` alongside existing Seq sink
- `appsettings.json` — add `Datadog:ApiKey` and `Datadog:ServiceName` config keys
- `.env` / K8s secrets — `DD_API_KEY` secret
- `docker-compose.yml` — pass `Datadog__ApiKey` env var to webapi container
- K8s `webapi/deployment.yaml` — mount `DD_API_KEY` from secret

**NuGet packages**
- `OpenTelemetry.Exporter.OpenTelemetryProtocol` (already present for OTLP)
- `Serilog.Sinks.Datadog.Logs`

**Free tier coverage**
- 5 hosts, 1-day metric/trace/log retention — sufficient for study/demo
- Pre-built .NET dashboards available in Datadog marketplace at zero cost

**Dependencies**
- None — purely additive, does not affect any other phase
