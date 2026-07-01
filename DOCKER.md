# DevIO.OrderPay — Docker Guide

How the full stack runs under Docker Compose: WebApi, SQL Server, Keycloak (+ Postgres),
RabbitMQ, the Next.js frontend, nginx reverse proxy, and Seq.

## Prerequisites

- Docker Engine + Docker Compose v2
- WSL2 (Ubuntu 24.04) on Windows
- .NET 10 SDK (only needed for running/testing outside containers)

---

## Services

`docker-compose.yml` defines eight services on a single bridge network (`app_network`):

| Service | Image / build | Role | Ports (host:container) |
|---|---|---|---|
| `devio.orderpay.webapi` | built from `src/Apps/DevIO.OrderPay.WebApi/Dockerfile` | ASP.NET Core API | `8080:8080` |
| `orderpay-web` | built from `orderpay-web/Dockerfile` | Next.js frontend | internal `3000` (via nginx) |
| `nginx` | `nginx:alpine` | Reverse proxy / single entry point | `80:80` |
| `sqlserver` | built from root `Dockerfile` | SQL Server 2025 (app DB) | `1433:1433` |
| `keycloak` | `quay.io/keycloak/keycloak:latest` | Auth server (OIDC/JWT) | `8085:8085` |
| `keycloak-setup` | `alpine` + `keycloak/setup.sh` | One-shot realm/clients/users bootstrap | — (runs once, exits) |
| `postgres` | `postgres:16-alpine` | Keycloak database | internal `5432` |
| `rabbitmq` | `rabbitmq:4-management` | Message broker (Outbox → MassTransit) | `5672:5672`, `15672:15672` |
| `seq` | `datalust/seq:latest` | Structured logs | `8082:80`, `5341:5341` |

The compose file is the source of truth — read it directly rather than relying on a copy here.

### Subdomain routing (nginx)

nginx routes by `Host` header, so everything is reachable through port 80:

| Hostname | Routed to |
|---|---|
| `www.localhost` | frontend (`orderpay-web:3000`); `/api/auth/*` → frontend, `/api/*` → webapi |
| `api.localhost` | webapi (`devio.orderpay.webapi:8080`) — Swagger + REST |
| `id.localhost` | keycloak (`keycloak:8085`) |

`*.localhost` resolves to `127.0.0.1` on most systems automatically; otherwise add the three
hostnames to your hosts file.

---

## Key files

| File | Purpose |
|---|---|
| `docker-compose.yml` | Full stack definition |
| `Dockerfile` (root) | SQL Server 2025 custom image + `entrypoint.sh` (fixes volume perms, runs SQL init scripts) |
| `src/Apps/DevIO.OrderPay.WebApi/Dockerfile` | WebApi production image (multi-stage SDK → aspnet) |
| `src/Apps/DevIO.OrderPay.WebApi/Dockerfile.dev` | WebApi dev image (`dotnet run`) |
| `orderpay-web/Dockerfile` | Next.js production image (`npm ci` → `next build` → `npm start`); `NEXT_PUBLIC_API_URL=""` build arg = same-origin |
| `orderpay-web/.dockerignore` | Keeps `node_modules`, `.next`, `.env.local` out of the build context |
| `nginx/nginx.conf` | Reverse-proxy config (mounted into the nginx container) |
| `keycloak/realm-export.json` | Imported realm shell — realm settings + `admin`/`customer` roles only |
| `keycloak/setup.sh` | Single source of truth for **all** Keycloak clients (`orderpay-webapi`, `orderpay-swagger`, `orderpay-web`), audience mappers, and users |

---

## Daily development

```bash
cd /home/paulomauri/projects/DevIO.OrderPay

# start everything
docker compose up -d

# rebuild a service after code changes (frontend example)
docker compose up -d --build orderpay-web

# follow logs
docker compose logs -f devio.orderpay.webapi
docker compose logs -f orderpay-web

# stop (keeps data)
docker compose down

# stop + wipe all volumes (fresh Keycloak realm, empty DB)
docker compose down -v
```

After `down -v`, the first `up` re-imports the realm and re-runs `keycloak-setup`. Keycloak
takes ~30 s to become healthy before the setup job runs and before logins work.

---

## First-time setup

```bash
# 1. provide secrets (copy and fill in)
cp .env.example .env
#    set MSSQL_SA_PASSWORD, KEYCLOAK_CLIENT_SECRET, etc.

# 2. start the stack
docker compose up -d --build

# 3. watch health
docker compose ps

# 4. confirm the Keycloak bootstrap finished
docker compose logs keycloak-setup    # ends with "Keycloak setup complete!"
```

EF Core migrations run automatically on WebApi startup (`db.Database.Migrate()` in `Program.cs`).

### Access

| What | URL |
|---|---|
| Frontend | http://www.localhost |
| Swagger | http://api.localhost/swagger |
| Keycloak admin | http://id.localhost/admin (or http://localhost:8085/admin) |
| Seq | http://localhost:8082 |
| SQL Server | `localhost,1433` (sa / value of `MSSQL_SA_PASSWORD`) |

Default Keycloak users (created by `setup.sh`):

| User | Password | Roles |
|---|---|---|
| `admin@orderpay.com` | `Mauri@22` | `admin`, `customer` |
| `user@orderpay.com` | `User@123` | `customer` |

---

## Auth / Keycloak gotcha (important)

`KC_HOSTNAME` **must be a full URL** (`http://id.localhost`), not a bare host. With a bare
host Keycloak stamps the request port into its discovery document (`jwks_uri:
http://id.localhost:8085/...`). The WebApi's `HostRewritingHandler` then rewrites
`http://id.localhost` → `http://keycloak:8085`, producing an invalid **double-port** URL
(`http://keycloak:8085:8085/...`). The JWKS fetch fails → `IDX10500: no signing keys` → every
token is rejected with 401 → the frontend bounces back to `/login` in a loop.

Pinning `KC_HOSTNAME: http://id.localhost` keeps the issuer (`http://id.localhost/realms/orderpay`)
and `jwks_uri` port-free so the rewrite resolves cleanly to `http://keycloak:8085`.

---

## Build & push images

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
    -f orderpay-web/Dockerfile orderpay-web/
docker push paulomauri/orderpay-web:1.0.1
docker push paulomauri/orderpay-web:latest
```

---

## Useful commands

```bash
# health / status
docker compose ps
docker inspect keycloak --format='{{.State.Health.Status}}'

# env of a running container
docker compose exec devio.orderpay.webapi printenv | grep -i keycloak

# SQL Server inside the container
docker exec -it sqlserver2025 /opt/mssql-tools18/bin/sqlcmd \
    -S localhost -U SA -P "$MSSQL_SA_PASSWORD" -No -Q 'SELECT @@VERSION'

# get a JWT (swagger client supports direct grant)
curl -s -X POST http://id.localhost/realms/orderpay/protocol/openid-connect/token \
    -d client_id=orderpay-swagger \
    -d username=admin@orderpay.com -d password=Mauri@22 \
    -d grant_type=password | jq -r .access_token

# re-run the Keycloak bootstrap without a full reset
docker compose up -d --force-recreate keycloak-setup
```

---

## Ports reference

| Service | Container | Host | Notes |
|---|---|---|---|
| nginx | 80 | 80 | single entry point for `*.localhost` |
| WebApi | 8080 | 8080 | also reachable directly; normally via `api.localhost` |
| Frontend | 3000 | — | only via nginx (`www.localhost`) |
| Keycloak | 8085 | 8085 | also via `id.localhost` |
| SQL Server | 1433 | 1433 | |
| Postgres | 5432 | — | Keycloak DB, internal only |
| Seq UI | 80 | 8082 | |
| Seq ingest | 5341 | 5341 | Serilog + OTel |

---

## Architecture

```
Browser
  │  http://www.localhost / api.localhost / id.localhost  (port 80)
  ▼
nginx (reverse proxy, Host-based routing)
  ├── www.localhost ─────────► orderpay-web (Next.js :3000)
  │        /api/auth/* ───────► orderpay-web (NextAuth)
  │        /api/*      ───────► webapi
  ├── api.localhost ─────────► webapi (:8080) ── Swagger + REST
  └── id.localhost ──────────► keycloak (:8085)

webapi  ──► sqlserver (:1433)               app data
        ──► keycloak  (:8085) backchannel   JWKS / discovery
        ──► seq       (:5341)               structured logs
keycloak ──► postgres (:5432)               realm storage
```

---

## Troubleshooting

### Login loops back to `/login`
Almost always the JWKS double-port issue above. Check `docker compose logs
devio.orderpay.webapi | grep IDX` — `IDX10500` (no signing keys) or `IDX10205` (issuer
mismatch) both point at `KC_HOSTNAME`. Ensure it is `http://id.localhost`.

### `keycloak-setup` failed / clients missing
```bash
docker compose logs keycloak-setup
docker compose up -d --force-recreate keycloak-setup
```

### Migrations not applied
```bash
docker compose logs devio.orderpay.webapi | grep -i migrat
```

### SSL/TLS error connecting to SQL Server (DBeaver/VS Code)
Set driver properties: `encrypt = false`, `trustServerCertificate = true`.

### Build fails with `npm ETIMEDOUT` / `dotnet restore` network errors
BuildKit runs builds in an isolated network namespace that can intermittently fail to reach
`registry.npmjs.org` / NuGet — even when the host and normal containers can. The `orderpay-web`
and `webapi` **build** configs in `docker-compose.yml` pin `network: host` to use the host's
network during the build, which fixes it. If you ever build outside Compose, add `--network=host`:
```bash
docker build --network=host --build-arg NEXT_PUBLIC_API_URL="" \
  -t devioorderpay-orderpay-web -f orderpay-web/Dockerfile orderpay-web/
```
This is build-time only — it has no effect on the running containers (still on `app_network`).
