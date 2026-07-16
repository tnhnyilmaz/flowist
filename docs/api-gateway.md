# Flowist API Gateway

Flowist API Gateway, istemciler için tek giriş noktasıdır.

Gateway YARP Reverse Proxy kullanır.

## Local Gateway URL

```text
http://localhost:5177
```

## Route Mapping

| Gateway Route | Downstream Service | Downstream URL |
| --- | --- | --- |
| `/api/auth/**` | AuthService | `http://localhost:5123` |
| `/api/workspaces/**` | TaskService | `http://localhost:5147` |
| `/api/projects/**` | TaskService | `http://localhost:5147` |
| `/api/tasks/**` | TaskService | `http://localhost:5147` |
| `/api/notifications/**` | NotificationService | `http://localhost:5233` |
| `/api/workspaces/{workspaceId}/activities` | ActivityService | `http://localhost:5038` |
| `/api/workspaces/{workspaceId}/activities/**` | ActivityService | `http://localhost:5038` |
| `/api/activities/**` | ActivityService | `http://localhost:5038` |
| `/hubs/notification/**` | NotificationService | `http://localhost:5233` |

## Service Discovery Strategy

Local development ortamında service discovery statik YARP config ile yapılır.

Config dosyası:

```text
src/Flowist.ApiGateway/appsettings.json
```

Local destination adresleri:

```text
AuthService: http://localhost:5123
TaskService: http://localhost:5147
NotificationService: http://localhost:5233
ActivityService: http://localhost:5038
```

Docker Compose veya Kubernetes ortamında bu adresler servis DNS isimleriyle değiştirilebilir.

Örnek Docker/Kubernetes destination adresleri:

```text
http://flowist-authservice:8080
http://flowist-taskservice:8080
http://flowist-notificationservice:8080
http://flowist-activityservice:8080
```

## Authentication Forwarding

Gateway JWT token'ı kendisi doğrulamak zorunda değildir.

YARP varsayılan olarak `Authorization` header'ını downstream servislere forward eder.

Downstream servisler kendi JWT validation middleware'leri ile token doğrulaması yapar.

Doğrulanan davranış:

```text
Authorization header yok -> 401
Authorization header var -> downstream servis 200 döner
```

## CORS

Gateway seviyesinde CORS policy vardır.

İzin verilen local frontend origin'leri:

```text
http://localhost:3000
https://localhost:3000
http://localhost:5173
https://localhost:5173
```

## Rate Limiting

Gateway seviyesinde IP bazlı fixed window rate limit uygulanır.

Varsayılan:

```text
100 request / 1 dakika / IP
```

Limit aşılırsa:

```text
429 Too Many Requests
```

## Load Balancing

YARP cluster'larında RoundRobin load balancing policy tanımlıdır.

Local ortamda her cluster tek destination içerir.

Production ortamında aynı cluster altına birden fazla destination eklenirse YARP istekleri round-robin dağıtır.

## Swagger Aggregation

Gateway Swagger UI:

```text
http://localhost:5177/swagger
```

Gateway UI altında aşağıdaki servis swagger dokümanları listelenir:

```text
AuthService
TaskService
NotificationService
ActivityService
```