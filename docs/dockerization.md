# Flowist Dockerization

This document describes how Flowist backend services are containerized and how the local Docker Compose environment is started.

## Services

Flowist currently runs the following backend services in Docker:

| Service | Image | Host Port | Container Port |
| --- | --- | ---: | ---: |
| API Gateway | `flowist-apigateway:dev` | `5177` | `8080` |
| AuthService | `flowist-authservice:dev` | `5123` | `8080` |
| TaskService | `flowist-taskservice:dev` | `5147` | `8080` |
| NotificationService | `flowist-notificationservice:dev` | `5233` | `8080` |
| ActivityService | `flowist-activityservice:dev` | `5038` | `8080` |

The supporting infrastructure services are:

| Service | Image | Host Port |
| --- | --- | ---: |
| PostgreSQL | `postgres:16` | `5432` |
| RabbitMQ | `rabbitmq:3-management` | `5672`, `15672` |
| Redis | `redis:8` | `6379` |
| Seq | `datalust/seq:latest` | `5341` |
| Elasticsearch | `docker.elastic.co/elasticsearch/elasticsearch:8.19.14` | `9200` |

## Dockerfiles

Each backend service has its own Dockerfile:

```text
src/Flowist.AuthService/Dockerfile
src/Flowist.TaskService/Dockerfile
src/Flowist.NotificationService/Dockerfile
src/Flowist.ActivityService/Dockerfile
src/Flowist.ApiGateway/Dockerfile
```

Each Dockerfile uses a multi-stage build:

1. The `sdk` stage restores and publishes the service.
2. The `aspnet` runtime stage runs the published application.
3. Runtime images expose port `8080`.
4. Runtime images set `ASPNETCORE_ENVIRONMENT=Production`.
5. Services run as the non-root `app` user.
6. Each service includes a Docker healthcheck against `/health`.

Example healthcheck:

```dockerfile
HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
    CMD curl --fail http://localhost:8080/health || exit 1
```

## Docker Ignore

The repository root contains `.dockerignore` to keep Docker build contexts small.

Ignored content includes:

```text
**/bin/
**/obj/
.git/
.vs/
.vscode/
logs/
**/node_modules/
```

This prevents unnecessary files from being sent to Docker during image build.

## Build Images

Build a single service image:

```powershell
docker build -f src\Flowist.AuthService\Dockerfile -t flowist-authservice:dev .
```

Build all services through Docker Compose:

```powershell
docker compose -f infra\docker-compose.yml build
```

Start all services:

```powershell
docker compose -f infra\docker-compose.yml up -d --build
```

Stop all services:

```powershell
docker compose -f infra\docker-compose.yml down
```

## Docker Compose

The main Compose file is:

```text
infra/docker-compose.yml
```

It starts:

```text
postgres
rabbitmq
redis
seq
elasticsearch
auth-service
task-service
notification-service
activity-service
api-gateway
```

The API Gateway is exposed on:

```text
http://localhost:5177
```

Individual backend services are also exposed for local debugging:

```text
http://localhost:5123  AuthService
http://localhost:5147  TaskService
http://localhost:5233  NotificationService
http://localhost:5038  ActivityService
```

## Container Networking

Inside Docker Compose, services do not use host `localhost` to communicate with each other.

Instead, they use Compose service names:

```text
postgres
rabbitmq
redis
seq
elasticsearch
auth-service
task-service
notification-service
activity-service
```

For example, the API Gateway routes to services through internal Compose DNS:

```text
http://auth-service:8080/
http://task-service:8080/
http://notification-service:8080/
http://activity-service:8080/
```

## Environment Variables

Docker Compose overrides service configuration using ASP.NET Core environment variable syntax.

Examples:

```yaml
ConnectionStrings__DefaultConnection: Host=postgres;Port=5432;Database=flowist_auth;Username=postgres;Password=postgres
ConnectionStrings__Redis: redis:6379
RabbitMq__Host: rabbitmq
Jwt__Issuer: Flowist.AuthService
Jwt__Audience: Flowist.Client
Jwt__SecretKey: CHANGE_ME_DEVELOPMENT_SECRET_KEY_AT_LEAST_32_CHARS
```

The double underscore syntax maps to nested configuration keys.

For example:

```text
Jwt__Issuer
```

maps to:

```json
{
  "Jwt": {
    "Issuer": "Flowist.AuthService"
  }
}
```

## Database Setup

Docker Compose creates the PostgreSQL server, but application databases must exist before migrations are applied.

Required databases:

```text
flowist_auth
flowist_tasks
flowist_notifications
flowist_activity
```

Create them manually if needed:

```powershell
docker exec flowist-postgres createdb -U postgres flowist_auth
docker exec flowist-postgres createdb -U postgres flowist_tasks
docker exec flowist-postgres createdb -U postgres flowist_notifications
docker exec flowist-postgres createdb -U postgres flowist_activity
```

## Applying Migrations

For local Docker Compose, migrations can be applied by generating SQL scripts and executing them inside the PostgreSQL container.

AuthService example:

```powershell
dotnet ef migrations script `
    --project src\Flowist.AuthService\Flowist.AuthService.csproj `
    --startup-project src\Flowist.AuthService\Flowist.AuthService.csproj `
    --idempotent `
    --output auth-migrations.sql
```

```powershell
Get-Content auth-migrations.sql -Raw | docker exec -i flowist-postgres psql -U postgres -d flowist_auth
```

Repeat the same pattern for each service database.

## Health Checks

Check all containers:

```powershell
docker ps --filter "name=flowist-"
```

Expected healthy containers:

```text
flowist-apigateway
flowist-authservice
flowist-taskservice
flowist-notificationservice
flowist-activityservice
flowist-postgres
flowist-rabbitmq
flowist-redis
flowist-elasticsearch
```

Seq does not currently define a Docker healthcheck, so `Up` is expected.

Check HTTP health endpoints:

```powershell
curl.exe -i http://localhost:5177/health
curl.exe -i http://localhost:5123/health
curl.exe -i http://localhost:5147/health
curl.exe -i http://localhost:5233/health
curl.exe -i http://localhost:5038/health
```

Expected response:

```text
HTTP/1.1 200 OK

Healthy
```

## Gateway Smoke Tests

Unauthenticated requests should return `401 Unauthorized` for protected APIs:

```powershell
curl.exe -i http://localhost:5177/api/auth/me
curl.exe -i http://localhost:5177/api/workspaces
curl.exe -i http://localhost:5177/api/notifications
```

Authenticated requests should return `200 OK` after login.

Register:

```powershell
$registerBody = @{
    email = "docker-gateway-test@flowist.local"
    password = "Test123!"
    fullName = "Docker Gateway Test"
} | ConvertTo-Json

Invoke-WebRequest `
    -Uri "http://localhost:5177/api/auth/register" `
    -Method Post `
    -ContentType "application/json" `
    -Body $registerBody `
    -UseBasicParsing
```

Login:

```powershell
$loginBody = @{
    email = "docker-gateway-test@flowist.local"
    password = "Test123!"
} | ConvertTo-Json

$loginResponse = Invoke-WebRequest `
    -Uri "http://localhost:5177/api/auth/login" `
    -Method Post `
    -ContentType "application/json" `
    -Body $loginBody `
    -UseBasicParsing

$login = $loginResponse.Content | ConvertFrom-Json
$accessToken = $login.accessToken

$authHeaders = @{
    Authorization = "Bearer $accessToken"
}
```

Authenticated checks:

```powershell
Invoke-WebRequest `
    -Uri "http://localhost:5177/api/auth/me" `
    -Method Get `
    -Headers $authHeaders `
    -UseBasicParsing
```

```powershell
Invoke-WebRequest `
    -Uri "http://localhost:5177/api/workspaces" `
    -Method Get `
    -Headers $authHeaders `
    -UseBasicParsing
```

```powershell
Invoke-WebRequest `
    -Uri "http://localhost:5177/api/notifications" `
    -Method Get `
    -Headers $authHeaders `
    -UseBasicParsing
```

```powershell
$workspaceId = "11111111-1111-1111-1111-111111111111"

Invoke-WebRequest `
    -Uri "http://localhost:5177/api/workspaces/$workspaceId/activities" `
    -Method Get `
    -Headers $authHeaders `
    -UseBasicParsing
```

## Known Notes

The API Gateway and downstream services currently both emit `X-Correlation-ID`.

This can result in a combined response header like:

```text
X-Correlation-ID: gateway-id,service-id
```

This is not blocking, but can be cleaned up later by making the gateway the single response header owner or by applying a YARP response transform.
