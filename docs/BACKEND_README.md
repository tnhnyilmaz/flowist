# 🚀 Flowist Backend Çalıştırma Rehberi

Bu rehber, Flowist mikroservis backend mimarisinin (API Gateway, Auth, Task, Notification, Activity servisleri ve altyapı konteynerleri) yerel geliştirme ortamında nasıl çalıştırılacağını adım adım açıklamaktadır.

---

## 📋 Servis Portları ve Altyapı Tablosu

| Servis / Altyapı | Teknoloji | Host Portu | Kapsam / Swagger |
| :--- | :--- | :---: | :--- |
| **API Gateway** | YARP Reverse Proxy (.NET) | `5177` | `http://localhost:5177/swagger` |
| **AuthService** | ASP.NET Core Web API | `5123` | `http://localhost:5123/swagger` |
| **TaskService** | ASP.NET Core Web API | `5147` | `http://localhost:5147/swagger` |
| **NotificationService** | ASP.NET Core Web API + SignalR | `5233` | `http://localhost:5233/swagger` |
| **ActivityService** | ASP.NET Core Web API | `5038` | `http://localhost:5038/swagger` |
| **PostgreSQL** | Veritabanı | `5432` | Veritabanları: `flowist_auth`, `flowist_tasks`, `flowist_notifications`, `flowist_activity` |
| **Redis** | Caching & Rate Limiting | `6379` | Key-Value Cache |
| **RabbitMQ** | Event Bus (MassTransit) | `5672` / `15672` | Management Console: `http://localhost:15672` (`guest` / `guest`) |
| **Seq** | Merkezi Loglama | `5341` | UI: `http://localhost:5341` |
| **Elasticsearch** | Arama & Indexing | `9200` | Analytics Engine |

---

## 🛠 Ön Gereksinimler

- **Docker Desktop** (Docker Compose v2+)
- **.NET 9 SDK** (Konteyner olmadan çalıştırmak için)
- **PowerShell** veya **Bash**

---

## 📦 Yöntem 1: Docker Compose ile Tüm Sistemi Çalıştırma (Tavsiye Edilen)

Tüm altyapı servislerini (PostgreSQL, Redis, RabbitMQ, Seq, Elasticsearch) ve 5 mikroservisi tek komutla ayağa kaldırabilirsiniz.

### 1. Servisleri Başlatma

Proje kök dizininde şu komutu çalıştırın:

```powershell
docker compose -f infra/docker-compose.yml up -d --build
```

### 2. Veritabanlarını Oluşturma (İlk Kurulumda)

PostgreSQL container'ı ayağa kalktıktan sonra veritabanlarını oluşturun:

```powershell
docker exec flowist-postgres createdb -U postgres flowist_auth
docker exec flowist-postgres createdb -U postgres flowist_tasks
docker exec flowist-postgres createdb -U postgres flowist_notifications
docker exec flowist-postgres createdb -U postgres flowist_activity
```

### 3. Migrasyon SQL Scriptlerini Uygulama

Proje kökündeki SQL dosyalarını veritabanlarına uygulayın:

```powershell
Get-Content auth-migrations.sql -Raw | docker exec -i flowist-postgres psql -U postgres -d flowist_auth
Get-Content task-migrations.sql -Raw | docker exec -i flowist-postgres psql -U postgres -d flowist_tasks
Get-Content notification-migrations.sql -Raw | docker exec -i flowist-postgres psql -U postgres -d flowist_notifications
Get-Content activity-migrations.sql -Raw | docker exec -i flowist-postgres psql -U postgres -d flowist_activity
```

### 4. Sağlık Kontrolü (Health Checks)

Servislerin sağlık durumunu sorgulayın:

```powershell
curl.exe -i http://localhost:5177/health
curl.exe -i http://localhost:5123/health
curl.exe -i http://localhost:5147/health
curl.exe -i http://localhost:5233/health
curl.exe -i http://localhost:5038/health
```

### 5. Konteynerleri Durdurma

```powershell
docker compose -f infra/docker-compose.yml down
```

---

## 💻 Yöntem 2: Altyapı Docker + Servisleri .NET CLI ile Çalıştırma

Geliştirme yaparken kodları canlı debug etmek için veritabanı/mesajlaşma konteynerlerini Docker ile başlatıp, C# servislerini `dotnet run` ile çalıştırabilirsiniz.

### 1. Sadece Altyapı Servislerini Başlatma

```powershell
docker compose -f infra/docker-compose.yml up -d postgres redis rabbitmq seq
```

### 2. EF Core Migrasyonlarını Güncelleme

```powershell
dotnet ef database update --project src/Flowist.AuthService/Flowist.AuthService.csproj
dotnet ef database update --project src/Flowist.TaskService/Flowist.TaskService.csproj
dotnet ef database update --project src/Flowist.NotificationService/Flowist.NotificationService.csproj
dotnet ef database update --project src/Flowist.ActivityService/Flowist.ActivityService.csproj
```

### 3. Servisleri Sırayla Çalıştırma

Farklı terminal pencerelerinde veya IDE'nizde (Visual Studio / Rider multi-startup project) şu komutları çalıştırın:

```powershell
# Terminal 1: Auth Service
dotnet run --project src/Flowist.AuthService/Flowist.AuthService.csproj

# Terminal 2: Task Service
dotnet run --project src/Flowist.TaskService/Flowist.TaskService.csproj

# Terminal 3: Notification Service
dotnet run --project src/Flowist.NotificationService/Flowist.NotificationService.csproj

# Terminal 4: Activity Service
dotnet run --project src/Flowist.ActivityService/Flowist.ActivityService.csproj

# Terminal 5: API Gateway
dotnet run --project src/Flowist.ApiGateway/Flowist.ApiGateway.csproj
```

---

## 🧪 Quick Test & Smoke Test (PowerShell)

Sistemin çalıştığını doğrulamak için hızlı bir kayıt ve giriş testi:

```powershell
# 1. Kayıt
$registerBody = @{
    email = "test@flowist.local"
    password = "Password123!"
    fullName = "Flowist Test User"
} | ConvertTo-Json

Invoke-WebRequest -Uri "http://localhost:5177/api/auth/register" -Method Post -ContentType "application/json" -Body $registerBody

# 2. Giriş yapma & Token Alma
$loginBody = @{
    email = "test@flowist.local"
    password = "Password123!"
} | ConvertTo-Json

$res = Invoke-WebRequest -Uri "http://localhost:5177/api/auth/login" -Method Post -ContentType "application/json" -Body $loginBody
$token = ($res.Content | ConvertFrom-Json).accessToken

# 3. Korumalı Endpoint İsteği
Invoke-WebRequest -Uri "http://localhost:5177/api/auth/me" -Headers @{ Authorization = "Bearer $token" }
```
