# 🚀 Flowist — Enterprise Task & Workflow Management Platform

Flowist, kurumsal düzeyde odaklanmış "deep work" deneyimi sunan mikroservis mimarili görev ve süreç yönetim sistemidir.

## 📚 Dokümantasyon Dizini

- 📖 **[Backend Çalıştırma Rehberi](file:///c:/Users/birkil/flowist/docs/BACKEND_README.md)** — Docker Compose & .NET CLI ile backend servislerini çalıştırma adımları
- 🌐 **[API Endpoint Dokümantasyonu](file:///c:/Users/birkil/flowist/docs/ENDPOINTS_README.md)** — Tüm mikroservislere ait HTTP endpoint listesi ve parametreleri
- 🌉 **[API Gateway & YARP Mimarisi](file:///c:/Users/birkil/flowist/docs/api-gateway.md)** — Routing ve Gateway kuralları
- 🐳 **[Dockerization Dokümanı](file:///c:/Users/birkil/flowist/docs/dockerization.md)** — Konteynerleştirme ve healthcheck detayları

---

## ⚡ Hızlı Başlangıç (Backend)

```powershell
docker compose -f infra/docker-compose.yml up -d --build
```

API Gateway `http://localhost:5177` adresi üzerinden hizmet vermektedir. Swagger UI için `http://localhost:5177/swagger` adresini ziyaret edebilirsiniz.
