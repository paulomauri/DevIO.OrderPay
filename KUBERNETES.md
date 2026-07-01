# DevIO.OrderPay — Kubernetes

The canonical Kubernetes deployment guide lives next to the manifests:

➡️ **[k8s/KUBERNETES.md](k8s/KUBERNETES.md)**

It covers the full stack on Minikube — Postgres, Keycloak (+ setup Job), SQL Server, RabbitMQ,
Seq, WebApi, the Next.js frontend, and the nginx ingress — in dependency order, plus access via
`minikube tunnel`.

For day-to-day `kubectl` / `minikube` commands, see [DEVOPS_COMMANDS.md](DEVOPS_COMMANDS.md).

> This file used to hold an older 3-service guide (SQL Server + WebApi + Seq only). It was
> superseded by `k8s/KUBERNETES.md` once Keycloak, the frontend, and nginx were added.
