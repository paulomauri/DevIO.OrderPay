# DevIO.OrderPay — Docker Guide

## Prerequisites

- Docker Engine 29.x+
- Docker Compose v5.x+
- WSL2 (Ubuntu 24.04)
- .NET 10 SDK

---

## Project Structure

```
DevIO.OrderPay/
├── docker-compose.yml              ← production config
├── docker-compose.override.yml     ← development config (hot reload)
├── Dockerfile                      ← SQL Server custom image
├── entrypoint.sh                   ← SQL Server entrypoint script
└── src/
    └── Apps/
        └── DevIO.OrderPay.WebApi/
            ├── Dockerfile          ← WebApi production image
            └── Dockerfile.dev      ← WebApi development image (dotnet watch)
```

---

## Files Reference

### `Dockerfile` (solution root — SQL Server)

```dockerfile
FROM mcr.microsoft.com/mssql/server:2025-latest

USER root

RUN mkdir -p /var/opt/mssql/data \
             /var/opt/mssql/log \
             /var/opt/mssql/backup \
             /usr/src/app/scripts/sql \
    && chown -R mssql:mssql /var/opt/mssql \
    && chmod -R 755 /var/opt/mssql

COPY ./entrypoint.sh /usr/src/app/scripts/entrypoint.sh

RUN chown mssql:mssql /usr/src/app/scripts/entrypoint.sh \
    && chmod +x /usr/src/app/scripts/entrypoint.sh

ENV ACCEPT_EULA=Y \
    MSSQL_PID=Developer \
    MSSQL_SA_PASSWORD=Mauri@22 \
    MSSQL_COLLATION=SQL_Latin1_General_CP1_CI_AS \
    MSSQL_AGENT_ENABLED=true \
    MSSQL_TCP_PORT=1433

EXPOSE 1433

ENTRYPOINT ["/bin/bash", "/usr/src/app/scripts/entrypoint.sh"]
```

---

### `entrypoint.sh` (solution root)

```bash
#!/bin/bash
set -e

MSSQL_DIR="/var/opt/mssql"
MSSQL_USER="mssql"

echo "🔧  Fixing volume ownership for '${MSSQL_USER}'..."
chown -R ${MSSQL_USER}:${MSSQL_USER} "${MSSQL_DIR}"
chmod -R 755 "${MSSQL_DIR}"
echo "✅  Ownership fixed: ${MSSQL_DIR} → ${MSSQL_USER}"

init_scripts() {
    local SCRIPT_DIR="/usr/src/app/scripts/sql"
    local MAX_RETRIES=40
    local RETRY=0

    echo "⏳  Waiting for SQL Server to be ready..."
    until /opt/mssql-tools18/bin/sqlcmd \
            -S localhost -U SA -P "${MSSQL_SA_PASSWORD}" \
            -No -Q "SELECT 1" > /dev/null 2>&1; do
        RETRY=$((RETRY + 1))
        if [ "${RETRY}" -ge "${MAX_RETRIES}" ]; then
            echo "❌  SQL Server did not become ready after ${MAX_RETRIES} attempts."
            exit 1
        fi
        echo "   attempt ${RETRY}/${MAX_RETRIES} — retrying in 2s..."
        sleep 2
    done

    echo "✅  SQL Server is ready."

    shopt -s nullglob
    scripts=("${SCRIPT_DIR}"/*.sql)
    if [ ${#scripts[@]} -eq 0 ]; then
        echo "ℹ️   No SQL init scripts found in ${SCRIPT_DIR} — skipping."
    else
        for script in "${scripts[@]}"; do
            echo "▶  Running: $(basename "${script}")"
            /opt/mssql-tools18/bin/sqlcmd \
                -S localhost -U SA -P "${MSSQL_SA_PASSWORD}" \
                -No -i "${script}"
            echo "✅  Done: $(basename "${script}")"
        done
    fi

    echo "🚀  Initialisation complete. SQL Server is running."
}

init_scripts &

echo "▶  Starting SQL Server 2025 Developer Edition as '${MSSQL_USER}'..."
exec su -s /bin/bash ${MSSQL_USER} -c "exec /opt/mssql/bin/sqlservr"
```

---

### `src/Apps/DevIO.OrderPay.WebApi/Dockerfile` (production)

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Apps/DevIO.OrderPay.WebApi/DevIO.OrderPay.WebApi.csproj", "src/Apps/DevIO.OrderPay.WebApi/"]
RUN dotnet restore "./src/Apps/DevIO.OrderPay.WebApi/DevIO.OrderPay.WebApi.csproj"
COPY . .
WORKDIR "/src/src/Apps/DevIO.OrderPay.WebApi"
RUN dotnet build "./DevIO.OrderPay.WebApi.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./DevIO.OrderPay.WebApi.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DevIO.OrderPay.WebApi.dll"]
```

---

### `src/Apps/DevIO.OrderPay.WebApi/Dockerfile.dev` (development)

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0

WORKDIR /src

COPY ["src/Apps/DevIO.OrderPay.WebApi/DevIO.OrderPay.WebApi.csproj",         "src/Apps/DevIO.OrderPay.WebApi/"]
COPY ["src/Apps/DevIO.OrderPay.Core/DevIO.OrderPay.Core.csproj",             "src/Apps/DevIO.OrderPay.Core/"]
COPY ["src/Apps/DevIO.OrderPay.Infra/DevIO.OrderPay.Infra.csproj",           "src/Apps/DevIO.OrderPay.Infra/"]
COPY ["src/Contexts/DevIO.OrderPay.Customer/DevIO.OrderPay.Customer.csproj", "src/Contexts/DevIO.OrderPay.Customer/"]
COPY ["src/Contexts/DevIO.OrderPay.Customer.Application/DevIO.OrderPay.Customer.Application.csproj", "src/Contexts/DevIO.OrderPay.Customer.Application/"]

RUN dotnet restore "src/Apps/DevIO.OrderPay.WebApi/DevIO.OrderPay.WebApi.csproj"

COPY . .

WORKDIR /src/src/Apps/DevIO.OrderPay.WebApi

EXPOSE 8080

ENTRYPOINT ["dotnet", "watch", "run", "--no-launch-profile"]
```

---

### `docker-compose.yml`

```yaml
services:
  devio.orderpay.webapi:
    image: ${DOCKER_REGISTRY-}devioorderpaywebapi
    build:
      context: .
      dockerfile: src/Apps/DevIO.OrderPay.WebApi/Dockerfile
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ASPNETCORE_URLS=http://+:8080
      - ConnectionStrings__DefaultConnection=Server=sqlserver,1433;Database=OrderPayDb;User Id=sa;Password=Mauri@22;TrustServerCertificate=True;
    depends_on:
      sqlserver:
        condition: service_healthy
    ports:
      - "8080:8080"
    networks:
      - app_network

  sqlserver:
    build:
      context: .
      dockerfile: Dockerfile
    container_name: sqlserver2025
    hostname: sqlserver
    restart: unless-stopped
    environment:
      ACCEPT_EULA: "Y"
      MSSQL_PID: "Developer"
      MSSQL_SA_PASSWORD: "${MSSQL_SA_PASSWORD:-Mauri@22}"
      MSSQL_COLLATION: "SQL_Latin1_General_CP1_CI_AS"
      MSSQL_AGENT_ENABLED: "true"
      MSSQL_TCP_PORT: "1433"
    ports:
      - "${SQL_HOST_PORT:-1433}:1433"
    volumes:
      - sqlserver_data:/var/opt/mssql/data
      - sqlserver_log:/var/opt/mssql/log
      - sqlserver_backup:/var/opt/mssql/backup
    networks:
      - app_network
    healthcheck:
      test:
        - CMD-SHELL
        - |
          /opt/mssql-tools18/bin/sqlcmd -S localhost -U SA \
          -P "$$MSSQL_SA_PASSWORD" -No -Q "SELECT 1" || exit 1
      interval: 15s
      timeout: 10s
      retries: 10
      start_period: 30s

  seq:
    image: datalust/seq:latest
    container_name: seq
    environment:
      ACCEPT_EULA: "Y"
      SEQ_FIRSTRUN_ADMINPASSWORD: "${SEQ_ADMIN_PASSWORD:-Admin@123}"
    ports:
      - "5341:5341"
      - "8082:80"
    volumes:
      - seq_data:/data
    networks:
      - app_network

volumes:
  sqlserver_data:
  sqlserver_log:
  sqlserver_backup:
  seq_data:

networks:
  app_network:
    driver: bridge
```

---

### `docker-compose.override.yml` (development — hot reload)

```yaml
services:
  devio.orderpay.webapi:
    build:
      context: .
      dockerfile: src/Apps/DevIO.OrderPay.WebApi/Dockerfile.dev
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ASPNETCORE_URLS=http://+:8080
      - DOTNET_USE_POLLING_FILE_WATCHER=true
      - DOTNET_WATCH_RESTART_ON_RUDE_EDIT=true
      - ConnectionStrings__DefaultConnection=Server=sqlserver,1433;Database=OrderPayDb;User Id=sa;Password=Mauri@22;TrustServerCertificate=True;
    volumes:
      - ./src:/src/src
      - ~/.microsoft/usersecrets:/root/.microsoft/usersecrets:ro
```

---

## Step by Step — Daily Development

### 1. Start all services (dev mode with hot reload)

```bash
cd /mnt/c/projetos-estudo/DevIO.OrderPay
docker-compose up --build -d
```

### 2. Watch WebApi logs

```bash
docker logs -f devioorderpay-devio.orderpay.webapi-1
```

### 3. Watch SQL Server logs

```bash
docker logs -f sqlserver2025
```

### 4. Stop containers (keeps data)

```bash
docker-compose down
```

### 5. Stop and wipe all data

```bash
# ⚠️ destroys database — use only when needed
docker-compose down -v
```

---

## Step by Step — First Time Setup

### 1. Clone and navigate

```bash
cd /mnt/c/projetos-estudo/DevIO.OrderPay
```

### 2. Start containers

```bash
docker-compose up --build -d
```

### 3. Check all containers are healthy

```bash
docker ps
```

Expected:
```
CONTAINER ID   IMAGE              STATUS
xxx            devioorderpay...   Up (healthy)
yyy            sqlserver2025      Up (healthy)
zzz            seq                Up
```

### 4. Apply EF Core migrations

```bash
# Option A — from Visual Studio Package Manager Console
Update-Database -Project DevIO.OrderPay.Infra -StartupProject DevIO.OrderPay.WebApi

# Option B — migrations run automatically on startup via Program.cs
# db.Database.Migrate() handles this
```

### 5. Access services

```
WebApi Swagger  →  http://localhost:8080/swagger
Seq Dashboard   →  http://localhost:8082
SQL Server      →  localhost:1433 (user: sa / password: Mauri@22)
```

---

## Step by Step — Docker Hub Push

### 1. Login

```bash
docker login
```

### 2. Tag images

```bash
docker tag devioorderpaywebapi paulomauri/orderpay-webapi:1.0.0
docker tag devioorderpaywebapi paulomauri/orderpay-webapi:latest
```

### 3. Push

```bash
docker push paulomauri/orderpay-webapi:1.0.0
docker push paulomauri/orderpay-webapi:latest
```

### 4. Verify

```
https://hub.docker.com/u/paulomauri
```

---

## Useful Commands

### Container management

```bash
# list running containers
docker ps

# list all containers including stopped
docker ps -a

# check restart count
docker inspect sqlserver2025 --format='{{.RestartCount}}'

# check container health
docker inspect sqlserver2025 --format='{{.State.Health.Status}}'
```

### Image management

```bash
# list all images
docker images

# remove unused images
docker image prune

# remove all unused resources
docker system prune
```

### Network

```bash
# get WSL2 IP (for connecting from Windows tools)
ip addr show eth0 | grep "inet " | awk '{print $2}' | cut -d/ -f1

# test port connectivity from Windows
Test-NetConnection -ComputerName localhost -Port 1433
```

### SQL Server inside container

```bash
# connect to SQL Server inside container
docker exec -it sqlserver2025 /opt/mssql-tools18/bin/sqlcmd \
    -S localhost -U SA -P 'Mauri@22' -No -Q 'SELECT @@VERSION'

# backup database
docker exec sqlserver2025 /opt/mssql-tools18/bin/sqlcmd \
    -S localhost -U SA -P 'Mauri@22' -No \
    -Q "BACKUP DATABASE OrderPayDb TO DISK='/var/opt/mssql/backup/OrderPayDb.bak'"
```

---

## Environment Variables Reference

| Variable | Value | Description |
|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Development` | ASP.NET environment |
| `ASPNETCORE_URLS` | `http://+:8080` | Forces HTTP only inside Docker |
| `DOTNET_USE_POLLING_FILE_WATCHER` | `true` | Required for hot reload in WSL2 |
| `DOTNET_WATCH_RESTART_ON_RUDE_EDIT` | `true` | Restart on breaking changes |
| `MSSQL_SA_PASSWORD` | `Mauri@22` | SQL Server SA password |
| `ACCEPT_EULA` | `Y` | Accept SQL Server EULA |
| `SEQ_FIRSTRUN_ADMINPASSWORD` | `Mauri@22` | Seq admin password |

---

## Ports Reference

| Service | Internal Port | External Port | URL |
|---|---|---|---|
| WebApi | 8080 | 8080 | http://localhost:8080/swagger |
| SQL Server | 1433 | 1433 | localhost:1433 |
| Seq UI | 80 | 8082 | http://localhost:8082 |
| Seq Ingest | 5341 | 5341 | http://localhost:5341 |

---

## Troubleshooting

### Container keeps restarting

```bash
# check restart count
docker inspect <container> --format='{{.RestartCount}}'

# check logs for errors
docker logs <container> --tail 50
```

### SQL Server port not accessible from Windows

```bash
# check WSL2 port forwarding (PowerShell as Admin)
netsh interface portproxy add v4tov4 \
    listenaddress=0.0.0.0 \
    listenport=1433 \
    connectaddress=<WSL2-IP> \
    connectport=1433
```

### SSL/TLS error in DBeaver

Add to Driver Properties:
```
encrypt = false
trustServerCertificate = true
```

### Hot reload not detecting changes

```bash
# ensure polling watcher is enabled
DOTNET_USE_POLLING_FILE_WATCHER=true
```

### Migrations not applied

```bash
# check migration status
docker logs devioorderpay-devio.orderpay.webapi-1 | grep -i migration
```

---

## Architecture

```
Windows (Host)
├── DBeaver / VS Code         →  127.0.0.1:1433 (SQL Server)
├── Browser                   →  localhost:8080 (WebApi)
└── Browser                   →  localhost:8082 (Seq)

WSL2
└── Docker Engine
    └── app_network (bridge)
        ├── sqlserver2025     →  sqlserver:1433
        ├── webapi            →  devio.orderpay.webapi:8080
        └── seq               →  seq:5341 / seq:80
```
