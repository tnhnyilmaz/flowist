# Kubernetes Manifests

Flowist uses Kustomize for Kubernetes deployments.

## Structure

- `base/`: shared manifests for applications, infrastructure, networking, and monitoring.
- `dev/`: development overlay targeting the `flowist-dev` namespace.
- `staging/`: staging overlay targeting the `flowist-staging` namespace.
- `production/`: production overlay targeting the `flowist-prod` namespace.

## Validate

```powershell
kubectl kustomize k8s/dev
kubectl kustomize k8s/staging
kubectl kustomize k8s/production
```

## Deploy

```powershell
kubectl apply -k k8s/dev
kubectl apply -k k8s/staging
kubectl apply -k k8s/production
```

## Secrets

The manifests include placeholder `stringData` values for local/dev use. Replace these before deploying to a real cluster, or manage them through External Secrets Operator, Sealed Secrets, or cloud-native secret injection.

Required secret values:

- `POSTGRES_PASSWORD`
- `RABBITMQ_PASSWORD`
- `JWT_SECRET_KEY`
- `GF_SECURITY_ADMIN_PASSWORD`

## Metrics

The Kubernetes manifests prepare Prometheus scraping through pod annotations and Grafana provisioning. The application services still need a real `/metrics` endpoint before Prometheus can collect application metrics.
