# DevIO.OrderPay.WebApi

API entry point — controllers, extensions, auth middleware, Program.cs.

- `Controllers/` — CustomerController, OrderController, ProductController, PaymentController
- `Outbox/OutboxWorker.cs` (Phase 8) — `BackgroundService` that drains the Outbox (single-claim on SQL Server) and publishes each event to RabbitMQ via `IPublishEndpoint`
- `Messaging/` (Phase 8) — MassTransit consumers (`PaymentCapturedConsumer`, `PaymentConfirmedConsumer`) + `IdempotentConsumer` dedup gate; each delegates to a business handler in `Order.Application/EventHandlers`. MassTransit is registered in `Program.cs` (`Messaging:Transport` = `RabbitMq` | `InMemory`)
- `Auth/KeycloakRoleClaimTransformer.cs` — maps Keycloak realm roles into ASP.NET Core role claims
- `Extensions/KeycloakExtensions.cs` — JwtBearer + HostRewritingHandler (Docker/K8s JWKS fix)
- `Extensions/DatabaseExtensions.cs` — registers DbContext, runs migrations on startup
- `Extensions/SerilogExtensions.cs` — Serilog + Seq
- `Extensions/OpenTelemetryExtensions.cs` — OTLP tracing/metrics
- `Extensions/RateLimitingExtensions.cs` — two named policies: `"general"` (fixed window, reads) and `"writes"` (sliding window, mutations); keyed by `sub` claim → IP fallback; limits configurable via `RateLimiting:*` config keys; disabled in tests via `PostConfigure<RateLimiterSettings>`

## Rate limiting config keys

| Key | Default | Purpose |
|---|---|---|
| `RateLimiting:Enabled` | `true` | Master switch — set `false` in tests |
| `RateLimiting:General:PermitLimit` | `100` | Max requests/min for read endpoints |
| `RateLimiting:Writes:PermitLimit` | `20` | Max requests/min for write endpoints |

Middleware order: `UseAuthentication` → `UseRateLimiter` → `UseAuthorization` (rate limiter runs after auth so `sub` claim is available for per-user partitioning).

## Auth config keys

| Key | Purpose |
|---|---|
| `Keycloak:Authority` | Public issuer URL (local dev) |
| `Keycloak:Audience` | Must be `orderpay-webapi` |
| `Keycloak:MetadataAddress` | Internal URL for Docker/K8s — triggers HostRewritingHandler |
| `Keycloak:ValidIssuer` | Validates `iss` claim |
| `Keycloak:RequireHttpsMetadata` | `false` for dev/k8s |

`UseAuthentication` must come before `UseAuthorization` in Program.cs.
