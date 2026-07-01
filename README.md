# DevIO.OrderPay

[![CI](https://github.com/paulomauri/DevIO.OrderPay/actions/workflows/ci.yml/badge.svg)](https://github.com/paulomauri/DevIO.OrderPay/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
![.NET](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&logoColor=white)
![Next.js](https://img.shields.io/badge/Next.js-16-000000?logo=nextdotjs&logoColor=white)
![Docker](https://img.shields.io/badge/Docker-Compose-2496ED?logo=docker&logoColor=white)
![Kubernetes](https://img.shields.io/badge/Kubernetes-Minikube-326CE5?logo=kubernetes&logoColor=white)

A study project exploring a **production-shaped order & payment platform**: a .NET 10
Clean Architecture / DDD backend, a Next.js frontend, Keycloak authentication, full
observability, and container/Kubernetes deployment — all wired together with end-to-end tests.

> Built to practice patterns end-to-end, not as a commercial product. Every layer is
> deliberately strict (architecture tests, zero-warning builds, idempotency, resilience).

---

## Highlights

- **Clean Architecture + DDD** — domain, application, infrastructure, and API layers with
  dependencies enforced at build time by architecture tests.
- **Bounded contexts** — `Customer` and `Order`, each with its own domain, value objects,
  and application layer.
- **Authentication** — Keycloak (OAuth2 / OIDC). Bearer-only API, Swagger client, and a
  confidential frontend client; role-based authorization (`admin` / `customer`).
- **Resilience** — Polly (retry / circuit breaker / timeout) on the Keycloak backchannel +
  ASP.NET Core rate limiting (fixed window for reads, sliding window for writes).
- **Observability** — Serilog → Seq, OpenTelemetry traces/metrics.
- **Frontend** — Next.js 16 (App Router), TypeScript, Styled Components, Redux Toolkit,
  React Query, NextAuth, with form validation that mirrors the backend validators.
- **Fully tested** — 198 backend unit/integration tests (xUnit), frontend component tests
  (Jest + RTL), and 9 Playwright end-to-end specs against the live stack.
- **Deployable** — one-command Docker Compose and a Kubernetes (Minikube) manifest set,
  fronted by nginx.

---

## Architecture

```
                                  ┌────────────────────────┐
        Browser ───────────────▶  │  nginx (reverse proxy) │
   www / api / id .localhost      └───────────┬────────────┘
                                  ┌───────────┼───────────────────────────┐
                                  ▼           ▼                           ▼
                         orderpay-web     WebApi (ASP.NET Core)        Keycloak
                         (Next.js)        ├─ Customer context          (OIDC/JWT)
                                          ├─ Order context                  │
                                          ├─ Polly + Rate limiting          ▼
                                          └─ Serilog/OTel            PostgreSQL
                                                 │      │
                                                 ▼      ▼
                                           SQL Server   Seq
```

### Backend layers

```
DevIO.OrderPay.Core                  # Shared abstractions (repository interfaces)
DevIO.OrderPay.Customer              # Customer domain — Email/Address value objects
DevIO.OrderPay.Customer.Application  # CustomerService, validators, DTOs
DevIO.OrderPay.Order                 # Order domain — OrderItem, Product, Price, OrderStatus
DevIO.OrderPay.Order.Application     # OrderService, ProductService, validators, DTOs
DevIO.OrderPay.Infra                 # EF Core, AppDbContext, migrations, repositories
DevIO.OrderPay.WebApi                # Controllers, auth, extensions, Program.cs
```

| Layer | May depend on | Must not touch |
|---|---|---|
| Domain | nothing | EF Core, HTTP, Application |
| Application | Domain, Core interfaces | EF Core, HTTP, DbContext |
| Infrastructure | Domain, Core, EF Core | Application, WebApi |
| WebApi | Application, Infra (DI only) | domain rule enforcement |

---

## Tech stack

| Area | Technology |
|---|---|
| API | ASP.NET Core 10, EF Core (SQL Server 2025), FluentValidation, Polly, Rate Limiting |
| Auth | Keycloak (OAuth2 / OIDC, JWT) |
| Observability | Serilog + Seq, OpenTelemetry |
| Frontend | Next.js 16, TypeScript, Styled Components v6, Redux Toolkit, React Query, NextAuth v4 |
| Data | SQL Server 2025 (app), PostgreSQL (Keycloak) |
| Infra | Docker Compose, Kubernetes (Minikube), nginx |
| Testing | xUnit, FluentAssertions, Moq, NetArchTest, Jest + RTL, Playwright |
| CI/CD | GitHub Actions (build + test + coverage, Docker build & push) |

---

## Repository structure

```
DevIO.OrderPay/
├── src/
│   ├── Apps/                  # Core, Infra, WebApi
│   └── Contexts/             # Customer & Order bounded contexts
├── orderpay-web/             # Next.js frontend
├── tests/
│   ├── DevIO.OrderPay.Tests/ # .NET xUnit (unit, integration, architecture, resilience)
│   └── e2e/                  # Playwright end-to-end
├── keycloak/                 # realm-export.json + setup.sh (clients, users, mappers)
├── nginx/                    # reverse-proxy config
├── k8s/                      # Kubernetes manifests
├── docker-compose.yml        # full stack
└── docker-compose.e2e.yml    # containerised Playwright runner
```

---

## Getting started (Docker Compose)

### Prerequisites
- Docker Engine + Docker Compose v2
- (Optional) .NET 10 SDK to run/test outside containers

### Run

```bash
git clone https://github.com/paulomauri/DevIO.OrderPay.git
cd DevIO.OrderPay

cp .env.example .env          # then fill in passwords / secrets

docker compose up -d --build
docker compose ps             # wait until keycloak is healthy
docker compose logs keycloak-setup   # ends with "Keycloak setup complete!"
```

EF Core migrations run automatically on WebApi startup.

### Service URLs

| Service | URL |
|---|---|
| Frontend | http://www.localhost |
| Swagger | http://api.localhost/swagger |
| Keycloak admin | http://id.localhost/admin (or http://localhost:8085/admin) |
| Seq (logs) | http://localhost:8082 |
| SQL Server | `localhost,1433` |

`*.localhost` resolves to `127.0.0.1` automatically on most systems; otherwise add
`www.localhost`, `api.localhost`, `id.localhost` to your hosts file.

### Default users (created by `keycloak/setup.sh`)

| User | Password | Roles |
|---|---|---|
| `admin@orderpay.com` | `Mauri@22` | `admin`, `customer` |
| `user@orderpay.com` | `User@123` | `customer` |

> See [DOCKER.md](DOCKER.md) for the full service/port reference and the important
> `KC_HOSTNAME` note. For Kubernetes, see [k8s/KUBERNETES.md](k8s/KUBERNETES.md).

---

## Testing

```bash
# Backend — .NET xUnit (198 tests: unit, integration, architecture, resilience, rate limiting)
dotnet test tests/DevIO.OrderPay.Tests

# Frontend — Jest + React Testing Library (run in the container; host has no Linux Node)
docker compose exec orderpay-web npm test

# End-to-end — Playwright against the live stack (browser → nginx → Next.js → WebApi → Keycloak → SQL Server)
docker compose -f docker-compose.yml -f docker-compose.e2e.yml run --rm playwright
```

Test details: [tests/docs/TESTING.md](tests/docs/TESTING.md) (backend) and
[tests/e2e/README.md](tests/e2e/README.md) (E2E).

---

## Roadmap

| Phase | Status |
|---|---|
| 1 — Authentication (Keycloak JWT) | ✅ Done |
| 2 — Unit tests (239) | ✅ Done |
| K8s deployment (Minikube) | ✅ Done |
| 3 — Orders bounded context | ✅ Done |
| 4 — Resilience (Polly + rate limiting) | ✅ Done |
| 5 — CI/CD pipeline | ✅ Done |
| 6 — Frontend (Next.js + Redux) | ✅ Done |
| 7 — Payment bounded context + idempotency | ✅ Done |
| 8 — Order state machine + domain events + outbox (RabbitMQ/MassTransit) | ✅ Done |
| 9 — Logistics integration (outbound + inbound webhook) | ⏳ Planned |
| 10 — Datadog (cloud observability) | ⏳ Planned |

---

## Documentation

| Doc | Contents |
|---|---|
| [DOCKER.md](DOCKER.md) | Full Docker Compose stack, services, ports, troubleshooting |
| [DEVOPS_COMMANDS.md](DEVOPS_COMMANDS.md) | Docker / Minikube / kubectl command reference |
| [k8s/KUBERNETES.md](k8s/KUBERNETES.md) | Kubernetes (Minikube) deployment guide |
| [tests/docs/TESTING.md](tests/docs/TESTING.md) | Backend test strategy |
| [orderpay-web/README.md](orderpay-web/README.md) | Frontend setup & conventions |
| [CLAUDE.md](CLAUDE.md) | Architecture rules & conventions (also guides AI assistants) |

---

## License

[MIT](LICENSE) © 2026 Paulo Mauri
