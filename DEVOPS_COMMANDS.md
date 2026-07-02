# DevOps Commands Guide
## Docker, Minikube & Kubectl — Step by Step

---

## Table of Contents

1. [Docker Basics](#docker-basics)
2. [Docker Compose](#docker-compose)
3. [Docker Hub](#docker-hub)
4. [Minikube](#minikube)
5. [Kubectl — Basics](#kubectl-basics)
6. [Kubectl — Deployments](#kubectl-deployments)
7. [Kubectl — Services](#kubectl-services)
8. [Kubectl — Logs & Debugging](#kubectl-logs--debugging)
9. [Kubectl — Scaling & Updates](#kubectl-scaling--updates)
10. [Kubectl — Database Access](#kubectl-database-access)
11. [Full Workflow](#full-workflow)

---

## Docker Basics

### Images

```bash
# list all local images
docker images

# search for an image on Docker Hub
docker search paulomauri

# pull image from Docker Hub
docker pull paulomauri/orderpay-webapi:latest

# remove an image
docker rmi paulomauri/orderpay-webapi:1.0.0

# remove all unused images
docker image prune

# remove all unused resources (images, containers, networks, volumes)
docker system prune
```

### Containers

```bash
# list running containers
docker ps

# list all containers including stopped
docker ps -a

# list with specific format
docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"

# start a container
docker start sqlserver2025

# stop a container
docker stop sqlserver2025

# restart a container
docker restart sqlserver2025

# remove a container
docker rm sqlserver2025

# remove all stopped containers
docker container prune
```

### Container Inspection

```bash
# check restart count
docker inspect sqlserver2025 --format='{{.RestartCount}}'

# check health status
docker inspect sqlserver2025 --format='{{.State.Health.Status}}'

# check port mappings
docker inspect sqlserver2025 --format='{{.HostConfig.PortBindings}}'

# full inspect (all details)
docker inspect sqlserver2025

# check environment variables
docker exec sqlserver2025 printenv

# check running processes inside container
docker exec -it sqlserver2025 ps aux
```

### Container Logs

```bash
# view logs
docker logs sqlserver2025

# view last 50 lines
docker logs sqlserver2025 --tail 50

# follow logs in real time
docker logs -f sqlserver2025

# view logs with timestamps
docker logs sqlserver2025 --timestamps
```

### Building Images

```bash
# build image
docker build -t paulomauri/orderpay-webapi:1.0.0 .

# build with multiple tags
docker build \
    -t paulomauri/orderpay-webapi:1.0.0 \
    -t paulomauri/orderpay-webapi:latest \
    -f src/Apps/DevIO.OrderPay.WebApi/Dockerfile \
    .

# build without cache
docker build --no-cache -t paulomauri/orderpay-webapi:latest .

# tag existing image
docker tag devioorderpaywebapi paulomauri/orderpay-webapi:1.0.0
```

### Networking

```bash
# list networks
docker network ls

# inspect network
docker network inspect devioorderpay_app_network

# get WSL2 IP (for connecting from Windows)
ip addr show eth0 | grep "inet " | awk '{print $2}' | cut -d/ -f1

# test port connectivity from WSL2
nc -zv localhost 1433

# test port connectivity from Windows PowerShell
Test-NetConnection -ComputerName localhost -Port 1433
```

### Execute Commands Inside Container

```bash
# open interactive shell
docker exec -it sqlserver2025 bash

# run single command
docker exec -it sqlserver2025 ls /var/opt/mssql

# connect to SQL Server inside container
docker exec -it sqlserver2025 /opt/mssql-tools18/bin/sqlcmd \
    -S localhost -U SA -P 'Mauri@22' -No -Q 'SELECT @@VERSION'

# backup database
docker exec sqlserver2025 /opt/mssql-tools18/bin/sqlcmd \
    -S localhost -U SA -P 'Mauri@22' -No \
    -Q "BACKUP DATABASE OrderPayDb TO DISK='/var/opt/mssql/backup/OrderPayDb.bak'"
```

---

## Docker Compose

The stack has seven services: `devio.orderpay.webapi`, `orderpay-web`, `nginx`,
`sqlserver`, `keycloak`, `keycloak-setup` (one-shot), `postgres`, and `seq`.
See [DOCKER.md](DOCKER.md) for the full service/port/URL reference.

### Start & Stop

```bash
# start all services (detached)
docker compose up -d

# start with rebuild
docker compose up --build -d

# rebuild + restart a single service after code changes
docker compose up -d --build orderpay-web
docker compose up -d --build devio.orderpay.webapi

# stop containers (keep data)
docker compose down

# stop and remove volumes (⚠️ destroys DB + Keycloak realm)
docker compose down -v

# restart all services
docker compose restart
```

### Keycloak bootstrap

The `keycloak-setup` job creates the realm clients (`orderpay-webapi`, `orderpay-swagger`,
`orderpay-web`), audience mappers, and users from `keycloak/setup.sh`. It runs once after
Keycloak is healthy.

```bash
# confirm the bootstrap finished
docker compose logs keycloak-setup        # ends with "Keycloak setup complete!"

# re-run it without wiping data
docker compose up -d --force-recreate keycloak-setup

# get a JWT for testing (swagger client = direct grant)
curl -s -X POST http://id.localhost/realms/orderpay/protocol/openid-connect/token \
    -d client_id=orderpay-swagger \
    -d username=admin@orderpay.com -d password=Mauri@22 \
    -d grant_type=password | jq -r .access_token
```

### Monitoring

```bash
# list containers
docker-compose ps

# view logs of all services
docker-compose logs

# follow logs of specific service
docker-compose logs -f devio.orderpay.webapi

# view last 30 lines
docker-compose logs --tail 30
```

### Volumes

```bash
# list volumes
docker volume ls

# inspect volume
docker volume inspect devioorderpay_sqlserver_data

# remove specific volume
docker volume rm devioorderpay_sqlserver_data

# remove all unused volumes
docker volume prune
```

---

## Docker Hub

### Authentication

```bash
# login (checks docker info for existing session)
docker login

# check if already logged in
docker info | grep Username

# logout
docker logout
```

### Push & Pull

```bash
# build + tag backend
docker build \
    -t paulomauri/orderpay-webapi:1.0.6 -t paulomauri/orderpay-webapi:latest \
    -f src/Apps/DevIO.OrderPay.WebApi/Dockerfile .

# build + tag frontend (NEXT_PUBLIC_API_URL="" → same-origin via nginx)
docker build \
    -t paulomauri/orderpay-web:1.0.2 -t paulomauri/orderpay-web:latest \
    --build-arg NEXT_PUBLIC_API_URL="" \
    -f orderpay-web/Dockerfile orderpay-web/

# push both
docker push paulomauri/orderpay-webapi:1.0.6
docker push paulomauri/orderpay-webapi:latest

docker push paulomauri/orderpay-web:1.0.2
docker push paulomauri/orderpay-web:latest

# pull image
docker pull paulomauri/orderpay-webapi:latest

# check available tags on Docker Hub
curl https://hub.docker.com/v2/repositories/paulomauri/orderpay-webapi/tags \
    | grep -o '"name":"[^"]*"'
```

---

## Minikube

### Cluster Management

```bash
# start minikube
minikube start

# start with specific resources
minikube start \
    --driver=docker \
    --memory=4096 \
    --cpus=4

# check status
minikube status

# stop cluster (keeps data)
minikube stop

# delete cluster
minikube delete

# delete and remove all config
minikube delete --purge

# reset everything
rm -rf ~/.minikube
rm -rf ~/.kube
minikube start --driver=docker --memory=4096 --cpus=4
```

### Addons

```bash
# list all addons
minikube addons list

# enable ingress
minikube addons enable ingress

# enable metrics server
minikube addons enable metrics-server

# disable addon
minikube addons disable storage-provisioner
```

### Access Services

```bash
# expose service and get URL (terminal must stay open)
minikube service orderpay-webapi -n orderpay --url

# open service in browser directly
minikube service orderpay-webapi -n orderpay

# create tunnel for LoadBalancer services (recommended)
minikube tunnel
```

### Useful Info

```bash
# get minikube IP
minikube ip

# ssh into minikube node
minikube ssh

# view minikube dashboard
minikube dashboard

# view logs
minikube logs
```

---

## Kubectl Basics

### Context & Namespace

```bash
# check current context
kubectl config current-context

# set default namespace
kubectl config set-context --current --namespace=orderpay

# list all namespaces
kubectl get namespaces

# create namespace
kubectl create namespace orderpay

# delete namespace (removes everything inside)
kubectl delete namespace orderpay
```

### Apply Manifests

```bash
# apply single file
kubectl apply -f k8s/namespace.yaml

# apply all files in directory
kubectl apply -f k8s/webapi/

# apply all k8s files recursively
kubectl apply -f k8s/

# delete resources from file
kubectl delete -f k8s/webapi/

# dry run (validate without applying)
kubectl apply -f k8s/webapi/ --dry-run=client
```

### Get Resources

```bash
# get all resources in namespace
kubectl get all -n orderpay

# get all resources in all namespaces
kubectl get pods -A

# get pods
kubectl get pods -n orderpay

# get pods with more details
kubectl get pods -n orderpay -o wide

# watch pods in real time
kubectl get pods -n orderpay -w

# get services
kubectl get svc -n orderpay

# get deployments
kubectl get deployments -n orderpay

# get persistent volume claims
kubectl get pvc -n orderpay

# get secrets
kubectl get secrets -n orderpay

# get events sorted by time
kubectl get events -n orderpay --sort-by='.lastTimestamp'
```

---

## Kubectl Deployments

### Manage Deployments

```bash
# apply deployment
kubectl apply -f k8s/webapi/deployment.yaml

# delete deployment
kubectl delete deployment orderpay-webapi -n orderpay

# describe deployment (full details)
kubectl describe deployment orderpay-webapi -n orderpay

# get deployment YAML
kubectl get deployment orderpay-webapi -n orderpay -o yaml
```

### Environment Variables

```bash
# set environment variable
kubectl set env deployment/orderpay-webapi \
    ASPNETCORE_ENVIRONMENT=Development \
    -n orderpay

# view environment variables
kubectl exec deployment/orderpay-webapi -n orderpay -- printenv
```

---

## Kubectl Services

### Manage Services

```bash
# get services with external IPs
kubectl get svc -n orderpay

# describe service
kubectl describe svc orderpay-webapi -n orderpay

# port-forward service to localhost
kubectl port-forward svc/orderpay-webapi 8090:80 -n orderpay

# port-forward SQL Server
kubectl port-forward svc/sqlserver 1433:1433 -n orderpay

# port-forward Seq
kubectl port-forward svc/seq 8082:8082 -n orderpay

# port-forward in background
kubectl port-forward svc/orderpay-webapi 8090:80 -n orderpay &
```

---

## Kubectl Logs & Debugging

### Logs

```bash
# view pod logs
kubectl logs pod/orderpay-webapi-xxx -n orderpay

# follow logs in real time
kubectl logs -f deployment/orderpay-webapi -n orderpay

# view last 50 lines
kubectl logs deployment/orderpay-webapi -n orderpay --tail 50

# view logs from previous crashed container
kubectl logs pod/orderpay-webapi-xxx -n orderpay --previous
```

### Debugging

```bash
# describe pod (events, conditions, errors)
kubectl describe pod orderpay-webapi-xxx -n orderpay

# describe pod — look for events section
kubectl describe pod orderpay-webapi-xxx -n orderpay | grep -A 10 "Events"

# exec into running pod
kubectl exec -it pod/orderpay-webapi-xxx -n orderpay -- sh

# check resource usage
kubectl top pods -n orderpay
kubectl top nodes
```

---

## Kubectl Scaling & Updates

### Scale

```bash
# scale up
kubectl scale deployment orderpay-webapi --replicas=3 -n orderpay

# scale down
kubectl scale deployment orderpay-webapi --replicas=1 -n orderpay

# check scaling
kubectl get pods -n orderpay -w
```

### Rolling Updates

```bash
# update image to new version
kubectl set image deployment/orderpay-webapi \
    orderpay-webapi=paulomauri/orderpay-webapi:1.0.2 \
    -n orderpay

# watch rollout progress
kubectl rollout status deployment/orderpay-webapi -n orderpay

# rollback to previous version
kubectl rollout undo deployment/orderpay-webapi -n orderpay

# view rollout history
kubectl rollout history deployment/orderpay-webapi -n orderpay

# restart deployment (pulls latest image)
kubectl rollout restart deployment/orderpay-webapi -n orderpay
```

---

## Kubectl Database Access

### Access SQL Server from outside cluster

The `sqlserver` service is **ClusterIP** (no external IP), so it's only reachable
inside the cluster. To connect from the host (SSMS / Azure Data Studio / sqlcmd)
you **must** port-forward — `minikube tunnel` does not expose ClusterIP services.

```bash
# terminal 1 — port-forward (keep open)
kubectl port-forward svc/sqlserver 1433:1433 -n orderpay

# …or run it detached in the background
kubectl port-forward svc/sqlserver 1433:1433 -n orderpay &

# verify the port is open on the host
Test-NetConnection -ComputerName 127.0.0.1 -Port 1433   # PowerShell
nc -vz 127.0.0.1 1433                                    # WSL / Linux

# terminal 2 — connect via sqlcmd (Docker, no local install needed)
docker run --rm -it mcr.microsoft.com/mssql-tools \
    /opt/mssql-tools/bin/sqlcmd \
    -S 127.0.0.1,1433 -U SA -P 'Mauri@22' \
    -Q 'SELECT name FROM sys.databases'
```

**Connection details**

| Field | Value |
|---|---|
| Server | `127.0.0.1,1433` |
| Auth | SQL Login |
| User / Password | `sa` / `Mauri@22` |
| Database | `OrderPayDb` |
| Trust server certificate | Yes |

### VS Code mssql connection

```json
{
    "server": "127.0.0.1,1433",
    "database": "OrderPayDb",
    "authenticationType": "SqlLogin",
    "user": "sa",
    "password": "Mauri@22",
    "encrypt": false,
    "trustServerCertificate": true,
    "profileName": "OrderPay K8s"
}
```

---

## Full Workflow

### First Time Setup

```bash
# 1. start minikube
minikube start --driver=docker --memory=4096 --cpus=4

# 2. enable addons
minikube addons enable ingress
minikube addons enable metrics-server

# 3. namespace + secrets
kubectl apply -f k8s/namespace.yaml
kubectl config set-context --current --namespace=orderpay
kubectl apply -f k8s/secrets.yaml

# 4. data + infra dependencies FIRST (webapi/keycloak depend on these)
kubectl apply -f k8s/postgres/      # Keycloak's database
kubectl apply -f k8s/sqlserver/     # WebApi's database
kubectl apply -f k8s/rabbitmq/      # WebApi's broker — MUST be up before webapi or it crash-loops
kubectl apply -f k8s/seq/           # logging sink
kubectl rollout status deploy/postgres deploy/sqlserver deploy/rabbitmq -n orderpay

# 5. Keycloak (needs postgres). The k8s/keycloak/ folder ALSO applies
#    setup-job.yaml (ConfigMap + Job) — it waits for Keycloak health, then
#    provisions the realm, roles, clients (orderpay-webapi / -swagger / -web)
#    and the seed users. No separate apply needed.
kubectl apply -f k8s/keycloak/
kubectl rollout status deploy/keycloak -n orderpay
kubectl wait --for=condition=complete job/keycloak-setup -n orderpay --timeout=180s

# 6. app tier (needs sqlserver + rabbitmq + keycloak all healthy)
kubectl apply -f k8s/webapi/
kubectl rollout status deploy/orderpay-webapi -n orderpay
kubectl apply -f k8s/frontend/
kubectl apply -f k8s/nginx/

# 7. watch pods settle
kubectl get pods -n orderpay -w

# 8. expose services (terminal 1 — keep open)
minikube tunnel

# 9. check external IPs (terminal 2)
kubectl get svc -n orderpay
```

> **Keycloak setup Job** — `k8s/keycloak/setup-job.yaml` is bundled in the
> `k8s/keycloak/` folder, so step 5 applies it automatically. The Job has
> `ttlSecondsAfterFinished: 300`, so it self-deletes ~5 min after completing
> (a later `kubectl get jobs` showing "No resources found" is normal — it ran
> and was cleaned up). To force a re-run (e.g. after editing `setup.sh`):
>
> ```bash
> kubectl delete job keycloak-setup -n orderpay --ignore-not-found
> kubectl apply -f k8s/keycloak/setup-job.yaml
> kubectl wait --for=condition=complete job/keycloak-setup -n orderpay --timeout=180s
> kubectl logs job/keycloak-setup -n orderpay        # verify clients + users created
> ```

### Daily Development Workflow

```bash
# 1. start minikube
minikube start

# 2. start tunnel (terminal 1 — keep open)
minikube tunnel

# 3. check everything is running (terminal 2)
kubectl get pods -n orderpay

# 4. make code changes locally
# 5. build new image
docker build \
    -t paulomauri/orderpay-webapi:1.0.x \
    -t paulomauri/orderpay-webapi:latest \
    -f src/Apps/DevIO.OrderPay.WebApi/Dockerfile \
    .

# 6. push to Docker Hub
docker push paulomauri/orderpay-webapi:1.0.x
docker push paulomauri/orderpay-webapi:latest

# 7. deploy to kubernetes
kubectl set image deployment/orderpay-webapi \
    orderpay-webapi=paulomauri/orderpay-webapi:1.0.x \
    -n orderpay

# 8. watch rollout
kubectl rollout status deployment/orderpay-webapi -n orderpay
```

### Shutdown

```bash
# stop minikube (keeps data)
minikube stop

# full cleanup
kubectl delete namespace orderpay
minikube stop
```

---

## Service URLs Reference

| Service | URL | Notes |
|---|---|---|
| Frontend | `http://www.localhost` | minikube tunnel required |
| WebApi Swagger | `http://api.localhost/swagger` | minikube tunnel required |
| WebApi Health | `http://api.localhost/health` | minikube tunnel required |
| WebApi (via frontend origin) | `http://www.localhost/api/v1/Customer` | same-origin API for the SPA |
| Keycloak Admin | `http://id.localhost/admin` | minikube tunnel required |
| Keycloak (direct) | `http://localhost:8085/admin` | keycloak LoadBalancer |
| Seq Dashboard | `http://127.0.0.1:8082` | minikube tunnel required |
| SQL Server | `127.0.0.1,1433` | kubectl port-forward required |

> **`*.localhost` resolves to `127.0.0.1` automatically** in Chrome/Firefox
> (RFC 6761) — no hosts-file edits needed. `minikube tunnel` binds the nginx
> LoadBalancer to `127.0.0.1:80`, and nginx routes by `Host` (www / api /
> id.localhost). `id.localhost` is the single OIDC issuer used by **both** the
> browser (via the tunnel) and the frontend pod (via `hostAliases` → nginx),
> so NextAuth's server-side token exchange and the browser share one issuer.

---

## Ports Reference

| Service | Internal Port | External Port | Access |
|---|---|---|---|
| WebApi | 8080 | 80 | LoadBalancer |
| SQL Server | 1433 | 1433 | ClusterIP (port-forward) |
| Seq UI | 80 | 8082 | LoadBalancer |
| Seq Ingest | 5341 | 5341 | LoadBalancer |

---

## Troubleshooting

### Minikube won't start

```bash
minikube delete --purge
rm -rf ~/.minikube ~/.kube
minikube start --driver=docker --memory=4096 --cpus=4
```

### Pod stuck in ContainerCreating

```bash
kubectl describe pod <pod-name> -n orderpay
kubectl get events -n orderpay --sort-by='.lastTimestamp'
```

### Pod in ErrImagePull

```bash
# check image exists on Docker Hub
curl https://hub.docker.com/v2/repositories/<user>/<image>/tags \
    | grep -o '"name":"[^"]*"'

# force re-pull
kubectl rollout restart deployment/orderpay-webapi -n orderpay
```

### Pod in CrashLoopBackOff

```bash
# check logs
kubectl logs pod/<pod-name> -n orderpay --previous

# describe for events
kubectl describe pod <pod-name> -n orderpay
```

### Service has no EXTERNAL-IP

```bash
# make sure minikube tunnel is running
minikube tunnel

# check service type is LoadBalancer
kubectl get svc -n orderpay
```

### Can't connect to SQL Server

```bash
# make sure port-forward is running
kubectl port-forward svc/sqlserver 1433:1433 -n orderpay

# test connection
Test-NetConnection -ComputerName 127.0.0.1 -Port 1433
```

---

## Key Concepts

| Concept | Description |
|---|---|
| **Pod** | Smallest deployable unit — runs one or more containers |
| **Deployment** | Manages pods — handles replicas, rolling updates, rollbacks |
| **Service** | Exposes pods to network — ClusterIP, LoadBalancer, NodePort |
| **ClusterIP** | Internal only — accessible inside cluster |
| **LoadBalancer** | External access — needs minikube tunnel on local |
| **PVC** | PersistentVolumeClaim — persistent storage for pods |
| **Secret** | Stores sensitive data — passwords, connection strings |
| **Namespace** | Logical isolation — groups related resources |
| **Rolling Update** | Zero-downtime deployment — replaces pods one by one |
| **Port-Forward** | Tunnel from localhost to a cluster service |
