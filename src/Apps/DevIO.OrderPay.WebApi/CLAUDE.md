# DevIO.OrderPay.WebApi

API entry point — controllers, extensions, auth middleware, Program.cs.

- `Controllers/` — CustomerController, OrderController, ProductController
- `Auth/KeycloakRoleClaimTransformer.cs` — maps Keycloak realm roles into ASP.NET Core role claims
- `Extensions/KeycloakExtensions.cs` — JwtBearer + HostRewritingHandler (Docker/K8s JWKS fix)
- `Extensions/DatabaseExtensions.cs` — registers DbContext, runs migrations on startup
- `Extensions/SerilogExtensions.cs` — Serilog + Seq
- `Extensions/OpenTelemetryExtensions.cs` — OTLP tracing/metrics

## Auth config keys

| Key | Purpose |
|---|---|
| `Keycloak:Authority` | Public issuer URL (local dev) |
| `Keycloak:Audience` | Must be `orderpay-webapi` |
| `Keycloak:MetadataAddress` | Internal URL for Docker/K8s — triggers HostRewritingHandler |
| `Keycloak:ValidIssuer` | Validates `iss` claim |
| `Keycloak:RequireHttpsMetadata` | `false` for dev/k8s |

`UseAuthentication` must come before `UseAuthorization` in Program.cs.
