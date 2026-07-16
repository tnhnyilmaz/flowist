# Flowist Observability

Bu doküman Flowist projesindeki local observability altyapısını açıklar.

## Bileşenler

Local geliştirme ortamında aşağıdaki altyapı servisleri Docker Compose ile çalıştırılır:

| Servis | Port | Açıklama |
| --- | ---: | --- |
| PostgreSQL | 5432 | Servis veritabanları |
| RabbitMQ | 5672 | Servisler arası event mesajlaşması |
| RabbitMQ Management UI | 15672 | RabbitMQ yönetim arayüzü |
| Seq | 5341 | Structured log görüntüleme arayüzü |
| Elasticsearch | 9200 | Log indexleme ve arama altyapısı |

## Altyapıyı Başlatma

Proje kök dizinindeyken:

```powershell
docker compose -f infra\docker-compose.yml up -d
```

## Altyapıyı Durdurma

```powershell
docker compose -f infra\docker-compose.yml down
```

## Container Durumunu Kontrol Etme

```powershell
docker ps --filter "name=flowist-"
```

Beklenen durum:

```text
flowist-postgres          healthy
flowist-rabbitmq          healthy
flowist-elasticsearch     healthy
flowist-seq               Up
```

## RabbitMQ

RabbitMQ Management UI:

```text
http://localhost:15672
```

Varsayılan kullanıcı:

```text
guest / guest
```

## Seq

Seq arayüzü:

```text
http://localhost:5341
```

Servis logları Serilog üzerinden Seq'e gönderilir.

Örnek correlation id filtresi:

```text
CorrelationId = 'seq-test-123'
```

## Elasticsearch

Elasticsearch ana endpoint:

```powershell
curl.exe http://localhost:9200
```

Cluster health kontrolü:

```powershell
curl.exe "http://localhost:9200/_cluster/health?pretty"
```

Beklenen durum:

```json
{
  "status": "green"
}
```

Servis log indexleri günlük olarak oluşturulur:

```text
flowist-authservice-yyyy.MM.dd
flowist-taskservice-yyyy.MM.dd
flowist-notificationservice-yyyy.MM.dd
flowist-activityservice-yyyy.MM.dd
```

Örnek index kontrolü:

```powershell
curl.exe "http://localhost:9200/_cat/indices/flowist-*?v"
```

Örnek log arama:

```powershell
curl.exe "http://localhost:9200/flowist-taskservice-*/_search?q=enrichment-test-123&pretty"
```

## Health Endpointleri

| Servis | URL |
| --- | --- |
| AuthService | http://localhost:5123/health |
| TaskService | http://localhost:5147/health |
| NotificationService | http://localhost:5233/health |
| ActivityService | http://localhost:5038/health |

Örnek:

```powershell
curl.exe -i http://localhost:5123/health
```

Beklenen cevap:

```text
HTTP/1.1 200 OK

Healthy
```

Eğer bağımlılıklardan biri çalışmıyorsa servis `503 Service Unavailable` dönebilir.

## Correlation ID

Servisler `X-Correlation-ID` header'ını destekler.

Örnek:

```powershell
$headers = @{
    "X-Correlation-ID" = "local-test-123"
}

Invoke-WebRequest `
    -Uri "http://localhost:5123/health" `
    -Method Get `
    -Headers $headers `
    -UseBasicParsing
```

Loglarda aynı değer görünür:

```json
"CorrelationId": "local-test-123"
```

## Workspace Log Enrichment

Servisler `X-Workspace-ID` header'ını log scope'a ekler.

Örnek:

```powershell
$headers = @{
    "X-Correlation-ID" = "enrichment-test-123"
    "X-Workspace-ID" = "11111111-1111-1111-1111-111111111111"
}

Invoke-WebRequest `
    -Uri "http://localhost:5147/health" `
    -Method Get `
    -Headers $headers `
    -UseBasicParsing
```

Elasticsearch veya Seq üzerinde loglarda şu alanlar görülebilir:

```json
"CorrelationId": "enrichment-test-123",
"WorkspaceId": "11111111-1111-1111-1111-111111111111"
```

## OpenTelemetry

Servislerde OpenTelemetry başlangıç entegrasyonu vardır.

Aktif instrumentation'lar:

- ASP.NET Core
- HTTP Client
- Entity Framework Core
- .NET Runtime metrics

Local ortamda Console Exporter kullanılır. Servis çalışırken terminalde metric ve trace çıktıları görülebilir.

Örnek metric isimleri:

```text
http.server.request.duration
dotnet.process.memory.working_set
dotnet.thread_pool.thread.count
dotnet.gc.collections
```

## Notlar

- Seq geliştirme ortamında hızlı log görüntüleme için kullanılır.
- Elasticsearch daha kapsamlı log indexleme ve arama için kullanılır.
- `CorrelationId`, bir request'in servisler arasındaki yolculuğunu takip etmek için kullanılır.
- `WorkspaceId`, workspace bazlı işlemleri loglarda ayırmak için kullanılır.