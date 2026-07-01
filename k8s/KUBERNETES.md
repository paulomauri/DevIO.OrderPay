# DevIO.OrderPay — Kubernetes Deployment Guide

## Prerequisites

- Docker Desktop with Minikube installed
- kubectl CLI
- Images pushed to Docker Hub
- WSL2 (Ubuntu 24.04)

---

## Project Structure

```
k8s/
├── namespace.yaml
├── secrets.yaml
├── postgres/
│   ├── deployment.yaml      ← Postgres (Keycloak database)
│   └── service.yaml
├── keycloak/
│   ├── deployment.yaml
│   ├── service.yaml
│   └── setup-job.yaml       ← ConfigMap + Job (realm, clients, users)
├── sqlserver/
│   ├── deployment.yaml
│   └── service.yaml
├── rabbitmq/
│   ├── deployment.yaml      ← RabbitMQ broker (Outbox → MassTransit) + PVC
│   └── service.yaml         ← LoadBalancer 5672 (AMQP) / 15672 (management UI)
├── webapi/
│   ├── deployment.yaml
│   └── service.yaml         ← ClusterIP (nginx handles external access)
├── frontend/
│   ├── deployment.yaml      ← orderpay-web Next.js (ClusterIP)
│   └── service.yaml
├── nginx/
│   ├── configmap.yaml       ← nginx.conf mounted as ConfigMap
│   ├── deployment.yaml
│   └── service.yaml         ← LoadBalancer port 80 (single entry point)
└── seq/
    ├── deployment.yaml
    └── service.yaml
```

---

## Phase 1 — Docker Hub

### 1. Login to Docker Hub

```bash
docker login
# Username: paulomauri
# Password: ********
```

### 2. Build and tag WebApi image

```bash
cd /home/paulomauri/projects/DevIO.OrderPay

docker build \
    -t paulomauri/orderpay-webapi:1.0.0 \
    -t paulomauri/orderpay-webapi:latest \
    -f src/Apps/DevIO.OrderPay.WebApi/Dockerfile \
    .
```

### 3. Or retag existing image

```bash
docker tag devioorderpaywebapi paulomauri/orderpay-webapi:1.0.0
docker tag devioorderpaywebapi paulomauri/orderpay-webapi:latest
```

### 4. Push WebApi to Docker Hub

```bash
docker push paulomauri/orderpay-webapi:1.0.0
docker push paulomauri/orderpay-webapi:latest
```

### 5. Build and push Frontend image

```bash
docker build \
    -t paulomauri/orderpay-web:latest \
    --build-arg NEXT_PUBLIC_API_URL="" \
    -f orderpay-web/Dockerfile \
    orderpay-web/

docker push paulomauri/orderpay-web:latest
```

### 6. Verify on Docker Hub

```bash
docker search paulomauri
```

Or visit: `https://hub.docker.com/u/paulomauri`

---

## Phase 2 — Kubernetes Setup

### 1. Start Minikube

```bash
minikube start
minikube status
```

Expected output:
```
minikube
type: Control Plane
host: Running
kubelet: Running
apiserver: Running
kubeconfig: Configured
```

### 2. Enable Minikube addons

```bash
minikube addons enable ingress
minikube addons enable metrics-server
```

### 3. Create namespace

```bash
kubectl apply -f k8s/namespace.yaml
kubectl config set-context --current --namespace=orderpay
```

### 4. Apply secrets

```bash
kubectl apply -f k8s/secrets.yaml
```

Verify:
```bash
kubectl get secrets -n orderpay
```

---

## Phase 3 — Deploy Services

Deploy in dependency order: Postgres first (Keycloak needs it), then Keycloak, then everything else.

### 1. Deploy Postgres

```bash
kubectl apply -f k8s/postgres/
```

### 2. Deploy Keycloak + setup Job

```bash
kubectl apply -f k8s/keycloak/
```

The setup Job waits for Keycloak's `/health/ready` internally before configuring the realm, clients, roles, and users. Watch it:
```bash
kubectl logs -f job/keycloak-setup -n orderpay
```

### 3. Deploy SQL Server

```bash
kubectl apply -f k8s/sqlserver/
```

### 4. Deploy RabbitMQ

```bash
kubectl apply -f k8s/rabbitmq/
```

### 5. Deploy Seq

```bash
kubectl apply -f k8s/seq/
```

### 6. Deploy WebApi

```bash
kubectl apply -f k8s/webapi/
```

### 7. Deploy Frontend

```bash
kubectl apply -f k8s/frontend/
```

### 8. Deploy Nginx (reverse proxy — single entry point)

```bash
kubectl apply -f k8s/nginx/
```

### 9. Verify everything is running

```bash
kubectl get all -n orderpay
```

Expected output:
```
NAME                                   READY   STATUS      RESTARTS
pod/keycloak-xxx                       1/1     Running     0
pod/keycloak-setup-xxx                 0/1     Completed   0
pod/nginx-xxx                          1/1     Running     0
pod/orderpay-web-xxx                   1/1     Running     0
pod/orderpay-webapi-xxx                1/1     Running     0
pod/orderpay-webapi-yyy                1/1     Running     0
pod/postgres-xxx                       1/1     Running     0
pod/seq-xxx                            1/1     Running     0
pod/sqlserver-xxx                      1/1     Running     0

NAME                      TYPE           CLUSTER-IP     EXTERNAL-IP
service/keycloak          LoadBalancer   10.96.x.x      127.0.0.1   ← port 8085 (admin)
service/nginx             LoadBalancer   10.96.x.x      127.0.0.1   ← port 80 (main entry)
service/orderpay-web      ClusterIP      10.96.x.x      <none>
service/orderpay-webapi   ClusterIP      10.96.x.x      <none>
service/postgres          ClusterIP      10.96.x.x      <none>
service/seq               LoadBalancer   10.96.x.x      127.0.0.1   ← port 8082 (logs)
service/sqlserver         ClusterIP      10.96.x.x      <none>
```

---

## Phase 4 — Access Services

### 1. Expose WebApi via Minikube

```bash
minikube service orderpay-webapi -n orderpay --url
```

Or use tunnel (keeps LoadBalancer running):
```bash
minikube tunnel
```

Then access:
```
http://127.0.0.1:80/swagger
```

### 2. Expose Seq UI

```bash
minikube service seq -n orderpay --url
```

Then access:
```
http://127.0.0.1:8082    ← Seq UI
```

### 3. Port-forward (alternative)

```bash
# WebApi
kubectl port-forward svc/orderpay-webapi 8080:80 -n orderpay

# Keycloak Admin Console
kubectl port-forward svc/keycloak 8085:8085 -n orderpay

# Seq UI
kubectl port-forward svc/seq 8082:8082 -n orderpay

# SQL Server
kubectl port-forward svc/sqlserver 1433:1433 -n orderpay
```

---

## Useful kubectl Commands

### Check pod logs

```bash
# webapi logs
kubectl logs -f deployment/orderpay-webapi -n orderpay

# sqlserver logs
kubectl logs -f deployment/sqlserver -n orderpay

# seq logs
kubectl logs -f deployment/seq -n orderpay
```

### Describe pod (debug issues)

```bash
kubectl describe pod <pod-name> -n orderpay
```

### Scale WebApi replicas

```bash
# scale up
kubectl scale deployment orderpay-webapi --replicas=3 -n orderpay

# scale down
kubectl scale deployment orderpay-webapi --replicas=1 -n orderpay
```

### Update image (rolling update)

```bash
# build and push new version
docker build -t paulomauri/orderpay-webapi:1.0.1 -f src/Apps/DevIO.OrderPay.WebApi/Dockerfile .
docker push paulomauri/orderpay-webapi:1.0.1

# update deployment
kubectl set image deployment/orderpay-webapi \
    orderpay-webapi=paulomauri/orderpay-webapi:1.0.1 \
    -n orderpay

# watch rollout
kubectl rollout status deployment/orderpay-webapi -n orderpay
```

### Rollback deployment

```bash
kubectl rollout undo deployment/orderpay-webapi -n orderpay
```

### Get events (debug crashes)

```bash
kubectl get events -n orderpay --sort-by='.lastTimestamp'
```

---

## Teardown

### Stop without deleting

```bash
minikube stop
```

### Delete all resources

```bash
kubectl delete namespace orderpay
```

### Delete Minikube cluster

```bash
minikube delete
```

---

## Architecture Overview

```
Browser (minikube tunnel → 127.0.0.1)
    ↓
Service: nginx  LoadBalancer :80
    ↓
Pod: nginx (reverse proxy)
    ├── /             → Service: orderpay-web  ClusterIP :3000
    │                       └── Pod: orderpay-web (Next.js)
    ├── /api/         → Service: orderpay-webapi  ClusterIP :8080
    │   /swagger/          └── Pod: orderpay-webapi (x2)
    │                               ├── → Service: sqlserver ClusterIP :1433
    │                               └── → Service: seq       ClusterIP :5341
    └── /realms/      → Service: keycloak  LoadBalancer :8085
        /admin/              └── Pod: keycloak
                                     └── → Service: postgres  ClusterIP :5432

Service: seq  LoadBalancer :8082  ← direct log viewer access
```

---

## Important Notes

| Topic | Detail |
|---|---|
| SQL Server image | Uses official `mcr.microsoft.com/mssql/server:2022-latest` |
| WebApi image | `paulomauri/orderpay-webapi:latest` from Docker Hub |
| Secrets | Never commit `secrets.yaml` with real passwords to git |
| Replicas | WebApi runs 2 replicas for high availability |
| Storage | SQL Server and Seq use PersistentVolumeClaims |
| Production | Use managed DB (Azure SQL / AWS RDS) instead of SQL Server in k8s |
