# DevIO.OrderPay

Study project — .NET 10 Clean Architecture API exploring DevIO patterns with observability and containerization.

## Stack

- **API:** ASP.NET Core 10, EF Core (SQL Server), FluentValidation, ASP.NET Core Rate Limiting
- **Auth:** Keycloak (JWT/OAuth2) — clients `orderpay-swagger` (issues tokens) + `orderpay-webapi` (bearer-only)
- **Observability:** Serilog + Seq, OpenTelemetry
- **Messaging:** RabbitMQ + MassTransit (transactional Outbox → broker; effectively-once via consumer dedup)
- **Infrastructure:** Docker Compose, Kubernetes (Minikube), SQL Server 2025

## Architecture — 4 layers

```
DevIO.OrderPay.Core                  # Shared abstractions (IRepository, repositories, IPaymentGateway in Core/Gateway)
DevIO.OrderPay.SharedKernel          # Dependency-free — IDomainEvent, DomainEvent, AggregateRoot, IDomainEventHandler, Contracts/PaymentCapturedEvent
DevIO.OrderPay.Customer              # Domain — Customer, Email/Address value objects, DuplicateCpfException
DevIO.OrderPay.Customer.Application  # Application — CustomerService, validators, DTOs
DevIO.OrderPay.Order                 # Domain — Order (state machine + events), OrderItem, Product, Price, OrderStatus
DevIO.OrderPay.Order.Application     # Application — OrderService, ProductService, EventHandlers, validators, DTOs
DevIO.OrderPay.Payment               # Domain — Payment aggregate (state machine), Amount, PaymentMethod, PaymentAttempt
DevIO.OrderPay.Payment.Application   # Application — PaymentService (idempotent), validators, DTOs
DevIO.OrderPay.Infra                 # Infrastructure — EF Core, AppDbContext, migrations, repositories, Outbox
DevIO.OrderPay.WebApi                # API — controllers, Messaging (consumers), Outbox (worker), extensions, Program.cs
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
# first deploy (dependency order — wait between tiers so nothing crash-loops)
kubectl apply -f k8s/namespace.yaml
kubectl config set-context --current --namespace=orderpay
kubectl apply -f k8s/secrets.yaml

# data + infra deps first (rabbitmq MUST precede webapi or it crash-loops)
kubectl apply -f k8s/postgres/
kubectl apply -f k8s/sqlserver/
kubectl apply -f k8s/rabbitmq/
kubectl apply -f k8s/seq/
kubectl rollout status deploy/postgres deploy/sqlserver deploy/rabbitmq -n orderpay

# Keycloak — the k8s/keycloak/ folder also applies setup-job.yaml (ConfigMap +
# Job), which provisions the realm, roles, clients (orderpay-webapi/-swagger/-web)
# and seed users. Job self-deletes 5 min after completing (ttlSecondsAfterFinished).
kubectl apply -f k8s/keycloak/
kubectl rollout status deploy/keycloak -n orderpay
kubectl wait --for=condition=complete job/keycloak-setup -n orderpay --timeout=180s

# app tier (needs sqlserver + rabbitmq + keycloak healthy)
kubectl apply -f k8s/webapi/
kubectl rollout status deploy/orderpay-webapi -n orderpay
kubectl apply -f k8s/frontend/
kubectl apply -f k8s/nginx/

# expose services
minikube tunnel                        # terminal 1 — keep open

# re-run Keycloak setup Job (after editing setup.sh)
kubectl delete job keycloak-setup -n orderpay --ignore-not-found
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
| RabbitMQ (management) | `http://localhost:15672` | `http://127.0.0.1:15672` |
| Seq | `http://seq.localhost:8082` | `http://127.0.0.1:8082` |

## Roadmap

| Phase | Status |
|---|---|
| 1 — Authentication | ✅ Done |
| 2 — Unit Tests (239/239) | ✅ Done |
| K8s deployment | ✅ Done |
| 3 — Orders Bounded Context | ✅ Done |
| 4 — Resilience (Polly + Rate Limiting) | ✅ Done |
| 5 — CI/CD Pipeline | ✅ Done |
| 6 — Frontend (React + Next.js + Redux) | ✅ Done (Steps 1–10B) |
| 7 — Payment Bounded Context + Idempotency | ✅ Done |
| 8 — Order State Machine + Domain Events + Outbox + Idempotency | ✅ Done |
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
| 10-B | E2E tests — Playwright (`tests/e2e/` at solution root) | ✅ Done (9 specs, containerised runner) |

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

## Phase 7 — Payment Bounded Context (✅ Done)

New bounded context `DevIO.OrderPay.Payment` (domain + application), with infra in `Infra` and the gateway port in `Core`. 23 tests (18 unit + 5 integration); verified live (capture advances the order, replay never double-charges, `0000` card declines).

**Domain** (`DevIO.OrderPay.Payment`)
- `Payment` aggregate — `PaymentStatus` state machine: `Pending → Processing → Authorized → Captured → Refunded`; `Failed` terminal. Methods: `BeginProcessing/Authorize/Capture/Decline/Abandon/Refund`. **Rule: a declined attempt does NOT fail the payment** — `Decline()` returns it to `Pending` (retryable); `Abandon()` → `Failed` is the deliberate give-up.
- `Amount` value object (decimal + currency, defaults `USD`); `PaymentMethod` polymorphic (`PaymentMethodCard` / `PaymentMethodACH` + `PaymentType`).
- `PaymentAttempt` — `IdempotencyKey` (`orderId:attemptNumber`) + `Outcome` (`Pending→Succeeded|Failed`) + `ExpiresAt`. `NextNumber(existing)` picks the next attempt number.
- `InvalidPaymentTransitionException`, `DuplicatePaymentAttemptException`, `ValueLowerThanZeroException`.

**Application** (`DevIO.OrderPay.Payment.Application`)
- `PaymentService` — persists the attempt **before** the gateway call; on retry with a resolved attempt, replays the stored result (no charge). Raises `PaymentCapturedEvent` after capture.
- The gateway port `IPaymentGateway` + `PaymentGatewayResult` live in **`Core/Gateway`** (like the repository interfaces) so the Infra adapter implements them without Infra → Application. `MockPaymentGateway` (Infra) dedupes by key; cards ending `0000` decline.

**Integration / WebApi**
- `PaymentController` — `POST /api/v1/payment` (idempotent) + `GET /api/v1/payment/{orderId}`.
- `IPaymentCapturedHandler` was the Phase 7 Payment→Order seam (in-process call advancing the order to `PaymentConfirmed`). **Phase 8 replaced it with the Outbox → RabbitMQ flow and deleted the handler.**
- `JsonStringEnumConverter` registered so `PaymentType` binds from names (e.g. `"CREDIT"`).

**Persistence notes**
- `Amount` → two flat columns (`AmountValue`/`AmountCurrency`) via `OwnsOne`; ctor param names must match property names for EF binding.
- `PaymentMethod` is **polymorphic, so it's stored as a single JSON column** (EF owned types can't be polymorphic) — `PaymentMethodJson` serializes it in Infra. Not the flattened-columns option originally sketched.
- Unique index on `PaymentAttempt.IdempotencyKey` is the at-most-once backbone; `PaymentRepository.SaveChangesAsync` translates the unique-violation `SqlException` (2601/2627) into `DuplicatePaymentAttemptException` → `409`.
- `IDesignTimeDbContextFactory<AppDbContext>` (`AppDbContextFactory`) added so `Add-Migration` in VS doesn't time out booting the WebApi host.

**Frontend** (`orderpay-web`, card-only)
- `features/payments/PayOrderModal` — card form (amount derived from the order); `Pay` button on the Orders table.
- `features/orders/EditOrderModal` — order summary + add/remove items; `Edit` button. `isOrderEditable(status)` (`types/order.ts`) gates Pay/Edit to `Pending`/`AwaitingPayment` (UI-only guard — the backend `items` endpoints don't enforce it). Orders table also gained a **Discount** column.
- E2E `tests/e2e/specs/payments.spec.ts`. See `orderpay-web/CLAUDE.md` → "Phase 7 — Payment & order editing".

**Idempotency guarantee:** payment is charged at-most-once — retrying a failed call with the same key never double-charges.

**Deferred to Phase 8:** the Outbox write on `OrderProcessing`, domain-event dispatch infrastructure, and the order advance to `Processing`.

## Phase 8 — Order State Machine + Domain Events + Outbox (✅ Done)

Built in two passes the user chose explicitly: **8a** = transactional Outbox drained by an
**in-process** dispatcher; **8b** = same Outbox, but the worker **publishes to RabbitMQ via
MassTransit** and idempotent consumers apply the side effects. 239 tests; verified live on
Docker Compose (pay → `Pending → PaymentConfirmed → Processing` advances asynchronously, the
SEND/RECEIVE pair visible in the broker logs).

**Order lifecycle (allowed-transitions map)**
```
Pending → AwaitingPayment → PaymentConfirmed → Processing → Shipped → Delivered
                                                    ↓
                                          CancellationRequested → Refunding → Cancelled
```
- **`Pending → PaymentConfirmed` is allowed** so the Phase 7 payment flow keeps working. The
  enum has **no `Failed`** state. Terminal states (`Delivered`, `Cancelled`) map to `[]`.

**State machine** (`Order : AggregateRoot`)
- `Status` is `{ get; private set; }` — the aggregate is the only mutator (dropped `required`;
  `required` + private set is CS9032).
- `UpdateStatus(next)` — **no-ops if `Status == next`** (so a redelivered event can't double-fire),
  validates against `_allowedTransitions`, sets `Status` + `UpdatedAt`, then raises the matching
  event (`PaymentConfirmedEvent` / `OrderProcessingEvent` / `OrderShippedEvent` /
  `OrderDeliveredEvent` / `OrderCancelledEvent`).
- Throws `InvalidOrderTransitionException` (→ `422` in `OrderController`) for illegal moves.
- `MarkDelivered(deliveredAt, deliveredBy)` → `UpdateStatus(Delivered)` then sets the two fields.
- New fields `DeliveredAt` (`DateTime?`), `DeliveredBy` (`string?`, observation: carrier/driver/notes).

**SharedKernel project** (`src/Apps/DevIO.OrderPay.SharedKernel`, dependency-free)
- `IDomainEvent` / `DomainEvent` (base record: `EventId`, `OccurredOn`), `IDomainEventHandler<T>`,
  and `AggregateRoot` (accumulates events in a `List<IDomainEvent>`, `RaiseEvent`, `ClearDomainEvents`).
- **Lives here, not in Core** — Core references the domain projects, so putting `IDomainEvent`
  there would be circular. Domains + Infra + WebApi reference SharedKernel.
- `Contracts/PaymentCapturedEvent.cs` — the cross-context integration event (Payment → Order).
  Placed in SharedKernel so **Payment no longer references Order** (broker-decoupled, user's choice):
  `Payment.Capture()` raises it, the Order side reacts.

**Transactional Outbox** (`Infra/Outbox`)
- `OutboxMessage { Id, Type, Content, OccurredOn, ProcessedOn?, Error? }` — `Id` = the event's
  `EventId` (the dedup key). `ProcessedOutboxMessage { Id, ProcessedOn }` — consumed-event ledger.
- `ConvertDomainEventsToOutboxInterceptor` (a `SaveChangesInterceptor`) reads
  `ChangeTracker.Entries<AggregateRoot>()`, serializes each event to an `OutboxMessage` row, and
  clears the events — **in the same `SaveChanges`/transaction** as the aggregate (solves the
  dual-write problem). `AppDbContext` `Ignore`s `DomainEvents` so it's never a column.

**8b — Outbox → RabbitMQ → consumers**
- `OutboxWorker` (`BackgroundService`, 1 s poll, batch 20) claims a batch, then per message in
  its own scope deserializes the event and **`IPublishEndpoint.Publish(domainEvent, runtimeType)`**
  (runtime type → routes to the matching `IConsumer<TEvent>`), marks `ProcessedOn`. On exception it
  records `Error` and leaves the row for the next poll (at-least-once).
- **Single-claim (multi-replica safe):** `OutboxMessage` carries `ClaimedAt`/`ClaimedBy`. On SQL
  Server the worker claims atomically — one `UPDATE … SET ClaimedAt/ClaimedBy WHERE Id IN (SELECT
  TOP(n) … WITH (ROWLOCK, READPAST) WHERE ProcessedOn IS NULL AND (ClaimedAt IS NULL OR ClaimedAt <
  now-lease) ORDER BY OccurredOn)` stamps a unique per-poll token, then it loads only its own rows
  by token. `READPAST` lets a sibling replica skip locked rows instead of blocking; the `lease`
  (60 s) reclaims rows a crashed worker claimed but never processed. So with `replicas: 2` each row
  is published by exactly **one** worker (verified live: a 3-event order split 2/1 across two pods).
  The **InMemory** test provider has no atomic claim — `db.Database.IsSqlServer()` falls back to a
  plain `WHERE ProcessedOn IS NULL` scan (no concurrency in tests). Filtered index
  `(ProcessedOn, OccurredOn) WHERE ProcessedOn IS NULL` backs the claim (migration `AddOutboxClaim`).
- Consumers (`WebApi/Messaging`): `PaymentCapturedConsumer`, `PaymentConfirmedConsumer`. Each is
  thin plumbing — it runs `IdempotentConsumer.HandleOnce` (dedup gate: skip if `EventId` already in
  `ProcessedOutboxMessage`, else run + record) and delegates to the **business handler in
  `Order.Application/EventHandlers`** (`ConfirmOrderOnPaymentCaptured`, `StartProcessingOnOrderConfirmed`).
  Business logic stays out of WebApi/Infra.
- MassTransit registered in `Program.cs`; `Messaging:Transport` config key selects the transport —
  `RabbitMq` (default) or `InMemory` (the test factory sets this via `UseSetting`, so the suite needs
  no broker). RabbitMQ connection via `RabbitMq:Host/Username/Password` keys.
- **NuGet pinned to MassTransit `8.5.2`** (the free OSS line) — `dotnet add` resolved v9.1.2, which
  moved to commercial licensing; pinned back to avoid a runtime license prompt in a study project.
- RabbitMQ container added to `docker-compose.yml` (`rabbitmq:4-management`, ports 5672 / 15672) and
  `k8s/rabbitmq/` (+ `rabbitmq-user`/`rabbitmq-password` secrets, env wired into `webapi/deployment.yaml`).

**Eventual consistency:** the order badge lags the payment response by ~1–2 s (the worker poll +
broker round-trip). Accepted by the user; the frontend just re-polls.

**`OrderProcessingEvent` is published but has no consumer yet** — it fans out to an unbound exchange
and is discarded. **Phase 9** binds it to the logistics dispatch consumer.

**Idempotency guarantee:** every downstream side effect runs at-least-once but is safe to repeat —
dedup by the event `Id` (`= OutboxMessage.Id = EventId`) against `ProcessedOutboxMessage`.

**End-to-end guarantee:** Phase 7 (at-most-once charge) + Phase 8 (at-least-once + idempotent
consumers via RabbitMQ/MassTransit) = effectively-once semantics across the payment/order pipeline.

**Deferred to Phase 9:** `ILogisticsClient`/`HttpLogisticsClient`, the `OrderProcessingEvent`
consumer (+ Shipped/Delivered/Cancelled consumers), and the inbound logistics webhook.

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
