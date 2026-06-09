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
| 2 — Unit Tests (29/29) | ✅ Done |
| K8s deployment | ✅ Done |
| 3 — Orders Bounded Context | ✅ Done |
| 4 — Resilience (Polly + Rate Limiting) | ✅ Done |
| 5 — CI/CD Pipeline | ✅ Done |
| 6 — Frontend | optional |
