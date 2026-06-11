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
- `HostRewritingHandler` in `KeycloakExtensions.cs` rewrites `localhost:8085 → keycloak:8085` for JWKS backchannel inside Docker/K8s — only activates when `Keycloak:MetadataAddress` config key is present

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

### Build & push image
```bash
docker build \
    -t paulomauri/orderpay-webapi:1.0.3 \
    -t paulomauri/orderpay-webapi:latest \
    -f src/Apps/DevIO.OrderPay.WebApi/Dockerfile .

docker push paulomauri/orderpay-webapi:1.0.3
docker push paulomauri/orderpay-webapi:latest
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
| Swagger | `http://localhost:8080/swagger` | `http://127.0.0.1/swagger` |
| Keycloak | `http://localhost:8085/admin` | `http://localhost:8085/admin` |
| Seq | `http://localhost:8082` | `http://127.0.0.1:8082` |

## Roadmap

| Phase | Status |
|---|---|
| 1 — Authentication | ✅ Done |
| 2 — Unit Tests (198/198) | ✅ Done |
| K8s deployment | ✅ Done |
| 3 — Orders Bounded Context | ✅ Done |
| 4 — Resilience (Polly + Rate Limiting) | ✅ Done |
| 5 — CI/CD Pipeline | ✅ Done |
| 6 — Frontend (React + Next.js + Redux) | 🔄 Steps 1–7 done / Steps 8–10 pending |
| 7 — Payment Bounded Context + Idempotency | pending |
| 8 — Order State Machine + Domain Events + Outbox + Idempotency | pending |
| 9 — Logistics Webhook (inbound status updates) | pending |

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
- `OrderService` reacts to `PaymentCapturedEvent` → advances `OrderStatus` from `AwaitingPayment → PaymentConfirmed`

**Idempotency guarantee:** payment is charged at-most-once — retrying a failed call with the same key never double-charges.

## Phase 8 — Order State Machine + Domain Events + Outbox (pending)

**State machine**
- `Order.UpdateStatus(newStatus)` validates the transition against an allowed-transitions map
- Throws `InvalidOrderTransitionException` for illegal moves (e.g. `Pending → Delivered`)

**Domain events**
- `Order` accumulates `IDomainEvent` instances in a `List<IDomainEvent>`
- Events: `PaymentConfirmedEvent`, `OrderShippedEvent`, `OrderCancelledEvent`
- Application layer dispatches them after `SaveChanges`

**Outbox pattern**
- `OutboxMessage` table — written atomically in the same EF Core transaction as the aggregate save; each message has a stable GUID `Id`
- `ProcessedOutboxMessage` table — stores consumed message IDs
- `OutboxWorker` (`IHostedService`) — polls, publishes to RabbitMQ via MassTransit, marks done after success

**Message broker — RabbitMQ + MassTransit**
- RabbitMQ as the message broker — exchange/queue routing for domain events
- MassTransit as the .NET abstraction — handles retries, dead-letter queues, consumer registration
- NuGet: `MassTransit`, `MassTransit.RabbitMQ`
- Consumers: `PaymentConfirmedConsumer`, `OrderShippedConsumer`, `OrderCancelledConsumer`
- Each consumer checks `ProcessedOutboxMessage` before executing (idempotency dedup by `OutboxMessage.Id`)
- RabbitMQ container added to `docker-compose.yml` and K8s manifests

**Idempotency guarantee:** every downstream side effect runs at-least-once but is safe to repeat — dedup by `OutboxMessage.Id` against `ProcessedOutboxMessage`.

**End-to-end guarantee:** Phase 7 (at-most-once charge) + Phase 8 (at-least-once + idempotent consumers via RabbitMQ/MassTransit) = effectively-once semantics across the payment and order pipeline.

## Phase 9 — Logistics Webhook (pending)

Inbound webhook endpoint called by a logistics company to push shipment status changes into the order pipeline.

**Endpoint**
- `POST /api/v1/webhook/logistics` — outside Keycloak auth policy; verified via HMAC-SHA256 `X-Signature` header using a shared secret
- Returns `202 Accepted` immediately; never exposes internal errors to the caller

**DTO**
- `LogisticsWebhookRequest` — `EventId` (string), `OrderId` (Guid), `LogisticsStatus` (string), `OccurredAt` (DateTimeOffset)

**Status mapping** (`LogisticsStatus → OrderStatus`)
```
IN_TRANSIT          → Shipped
OUT_FOR_DELIVERY    → Shipped
DELIVERED           → Delivered
FAILED              → CancellationRequested
RETURNED            → Refunding
```

**Idempotency**
- `ProcessedWebhookEvent` table — stores consumed `EventId` values
- Before processing: check if `EventId` already exists → skip if so
- Written in the same EF Core transaction as the `Order` status update

**Application layer**
- `ILogisticsWebhookService` + `LogisticsWebhookService` in `Order.Application`
- Calls `Order.UpdateStatus(mappedStatus)` — Phase 8 state machine validates the transition; illegal moves (`Delivered → Pending`) throw `InvalidOrderTransitionException` and return `422 Unprocessable Entity`

**Security**
- HMAC-SHA256 signature: `X-Signature: sha256=<hex>` computed over the raw request body with the shared secret stored in `appsettings` / K8s secret
- IP allowlist optional — configure at nginx level for the logistics company's CIDR

**Dependencies**
- Requires Phase 8 (`Order.UpdateStatus` state machine) to validate transitions
- Reuses Outbox pattern from Phase 8 — status change raises `OrderShippedEvent` / domain event as normal
