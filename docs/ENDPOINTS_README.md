# 🚀 Flowist Microservices API Endpoint Documentation

Flowist, microservice mimarisiyle tasarlanmış kurumsal düzeyde bir görev ve süreç yönetim sistemidir. Tüm dış istemci (Web, Mobile, Third-party) istekleri **Flowist API Gateway** üzerinden ilgili mikroservise yönlendirilir.

---

## 🌐 Genel Mimarisi ve Base URL

- **API Gateway Base URL**: `http://localhost:5177`
- **Swagger Documentation Aggregator**: `http://localhost:5177/swagger`
- **Kimlik Doğrulama**: `Authorization: Bearer <JWT_ACCESS_TOKEN>`
- **Ortak İletişim Header'ları**:
  - `X-Correlation-ID`: İstek takibi için (GUID)
  - `Content-Type`: `application/json`

---

## 🔐 1. Auth Service (`/api/v1/auth` veya `/api/auth`)

Kullanıcı kaydı, oturum açma, JWT token yenileme ve çıkış işlemlerini yönetir.

| Yöntem | Endpoint | Açıklama | Auth | Rol / Erişim |
| :--- | :--- | :--- | :---: | :--- |
| `POST` | `/api/auth/register` | Yeni kullanıcı kaydı oluşturur ve JWT token çifti döner | ❌ | Anonim |
| `POST` | `/api/auth/login` | Email ve şifre ile oturum açar | ❌ | Anonim |
| `POST` | `/api/auth/refresh` | Geçerli Refresh Token ile yeni Access Token & Refresh Token üretir | ❌ | Anonim |
| `POST` | `/api/auth/revoke` | Mevcut Refresh Token'ı ve aktif Access Token'ı iptal eder | ❌ | Anonim |
| `POST` | `/api/auth/revoke-all` | Kullanıcının tüm aktif oturumlarını iptal eder | 🔒 | Authenticated |
| `GET` | `/api/auth/me` | Oturum açmış kullanıcının profil bilgilerini getirir | 🔒 | Authenticated |

---

## 🏢 2. Workspace Management (`/api/workspaces`)

Çalışma alanları (Workspace) ve üyelik rollerini (Owner, Admin, Member) yönetir.

| Yöntem | Endpoint | Açıklama | Auth | Rol / Erişim |
| :--- | :--- | :--- | :---: | :--- |
| `POST` | `/api/workspaces` | Yeni çalışma alanı oluşturur (Oluşturan Owner olur) | 🔒 | Authenticated |
| `GET` | `/api/workspaces` | Kullanıcının üye olduğu tüm çalışma alanlarını listeler | 🔒 | Authenticated |
| `GET` | `/api/workspaces/{id}` | Belirtilen çalışma alanının detayını getirir | 🔒 | Owner, Admin, Member |
| `PUT` | `/api/workspaces/{id}` | Çalışma alanını günceller | 🔒 | Owner |
| `DELETE` | `/api/workspaces/{id}` | Çalışma alanını siler | 🔒 | Owner |
| `GET` | `/api/workspaces/{id}/members` | Çalışma alanı üyelerini ve rollerini listeler | 🔒 | Owner, Admin, Member |
| `POST` | `/api/workspaces/{id}/members` | Çalışma alanına yeni üye ekler | 🔒 | Owner |
| `DELETE` | `/api/workspaces/{id}/members/{userId}` | Üyeyi çalışma alanından çıkarır | 🔒 | Owner |
| `PUT` | `/api/workspaces/{id}/members/{userId}/role` | Üyenin rolünü günceller (Owner, Admin, Member) | 🔒 | Owner |

---

## 📁 3. Project Management (`/api/projects` & `/api/workspaces/{id}/projects`)

Çalışma alanı altındaki projeleri yönetir.

| Yöntem | Endpoint | Açıklama | Auth | Rol / Erişim |
| :--- | :--- | :--- | :---: | :--- |
| `POST` | `/api/workspace/{workspaceId}/projects` | Çalışma alanında yeni proje oluşturur | 🔒 | Owner, Admin |
| `GET` | `/api/workspaces/{workspaceId}/projects` | Çalışma alanındaki tüm projeleri listeler | 🔒 | Owner, Admin, Member |
| `GET` | `/api/projects/{id}` | Proje detaylarını getirir | 🔒 | Owner, Admin, Member |
| `PUT` | `/api/projects/{id}` | Projeyi günceller | 🔒 | Owner, Admin |
| `DELETE` | `/api/projects/{id}` | Projeyi ve bağlı tüm görevleri siler | 🔒 | Owner, Admin |

---

## ✅ 4. Task Item Management (`/api/tasks` & `/api/projects/{id}/tasks`)

Proje altındaki görevleri, atamaları, durumları ve filtrelemeleri yönetir.

| Yöntem | Endpoint | Açıklama | Auth | Rol / Erişim |
| :--- | :--- | :--- | :---: | :--- |
| `POST` | `/api/projects/{projectId}/tasks` | Projede yeni görev oluşturur | 🔒 | Owner, Admin, Member |
| `GET` | `/api/projects/{projectId}/tasks` | Projedeki görevleri filtreli ve sayfalı (paged) getirir | 🔒 | Owner, Admin, Member |
| `GET` | `/api/tasks/{id}` | Görev detayını getirir | 🔒 | Owner, Admin, Member |
| `PUT` | `/api/tasks/{id}` | Görevi günceller (Başlık, açıklama, son tarih vs.) | 🔒 | Owner, Admin, Member |
| `DELETE` | `/api/tasks/{id}` | Görevi siler | 🔒 | Owner, Admin, Member |
| `PUT` | `/api/tasks/{id}/assign` | Görevi bir üyeye atar veya atamayı kaldırır | 🔒 | Owner, Admin |
| `PUT` | `/api/tasks/{id}/status` | Görev durumunu günceller (Backlog, InProgress, Review, Completed) | 🔒 | Owner, Admin, Member |

---

## 🔔 5. Notification Service (`/api/notifications` & SignalR Hub)

Bildirimleri ve gerçek zamanlı SignalR akışını yönetir.

| Yöntem | Endpoint | Açıklama | Auth | Rol / Erişim |
| :--- | :--- | :--- | :---: | :--- |
| `GET` | `/api/notifications` | Kullanıcının sayfalı bildirim geçmişini getirir | 🔒 | Authenticated |
| `GET` | `/api/notifications/unread-count` | Okunmamış bildirim sayısını döner | 🔒 | Authenticated |
| `PUT` | `/api/notifications/{id}/read` | Bildirimi okundu olarak işaretler | 🔒 | Authenticated |
| `PUT` | `/api/notifications/read-all` | Tüm bildirimleri okundu olarak işaretler | 🔒 | Authenticated |
| `DELETE` | `/api/notifications/{id}` | Bildirimi siler | 🔒 | Authenticated |
| `WS` | `/hubs/notification` | Anlık bildirimler için SignalR WebSocket bağlantısı | 🔒 | Authenticated |

---

## 📊 6. Activity & Audit Service (`/api/activities`)

Çalışma alanı seviyesindeki aktivite akışını ve güvenlik denetim (audit) loglarını sunar.

| Yöntem | Endpoint | Açıklama | Auth | Rol / Erişim |
| :--- | :--- | :--- | :---: | :--- |
| `GET` | `/api/workspaces/{workspaceId}/activities` | Çalışma alanı aktivite akışını sayfalı getirir | 🔒 | Owner, Admin, Member |
| `GET` | `/api/workspaces/{workspaceId}/activities/audit` | Güvenlik ve denetim loglarını sayfalı getirir | 🔒 | Owner, Admin |

---

## ⚠️ Standart Hata Yanıt Formatı (RFC 7807 Problem Details)

Servislerden dönen tüm 4xx ve 5xx hataları **Problem Details** standardına uygundur:

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "detail": "The Email field is required.",
  "instance": "/api/auth/login",
  "errors": {
    "Email": [
      "The Email field is required."
    ]
  }
}
```
