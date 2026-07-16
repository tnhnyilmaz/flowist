# Flowist Cache Strategy

Bu doküman Flowist projesindeki Redis cache kullanım stratejisini açıklar.

## Amaç

Redis cache aşağıdaki amaçlarla kullanılır:

- Sık okunan verileri hızlı döndürmek
- Stateless JWT yapısında logout sonrası access token iptalini desteklemek
- Refresh token lookup işlemlerini hızlandırmak
- Notification unread count gibi sık çağrılan küçük verileri cachelemek
- SignalR connection state bilgisini servis instance'ları arasında paylaşmak

## Cache Key Formatları

| Amaç                      | Key Formatı                                 | Açıklama                                         |
| ------------------------- | ------------------------------------------- | ------------------------------------------------ |
| JWT blacklist             | `auth:blacklist:jwt:{jti}`                  | Logout/revoke edilen access token id bilgisi     |
| Refresh token cache       | `auth:refresh-token:{sha256(refreshToken)}` | Refresh token lookup cache                       |
| Notification unread count | `notification:unread-count:{userId}`        | Kullanıcının okunmamış notification sayısı       |
| SignalR connection state  | `signalr:user-connections:{userId}`         | Kullanıcının aktif SignalR connection id listesi |

## TTL Stratejisi

| Cache Türü                | TTL                      | Sebep                                                                  |
| ------------------------- | ------------------------ | ---------------------------------------------------------------------- |
| JWT blacklist             | Access token kalan ömrü  | Token süresi bitince blacklist kaydına ihtiyaç kalmaz                  |
| Refresh token cache       | Refresh token kalan ömrü | Refresh token süresi bitince cache kaydı da otomatik silinir           |
| Notification unread count | 10 dakika                | Sık okunan ama değiştiğinde yenilenebilen küçük veri                   |
| SignalR connection state  | 12 saat                  | Bağlantı kopması durumunda stale connection bilgisinin kalıcı olmaması |
| Short lived cache         | 5 dakika                 | Kısa süreli geçici cache ihtiyaçları                                   |
| Medium lived cache        | 30 dakika                | Orta süreli cache ihtiyaçları                                          |
| Long lived cache          | 12 saat                  | Daha uzun süreli ama sonsuz tutulmaması gereken cache ihtiyaçları      |

## Güvenlik Notları

Refresh token Redis içinde açık metin olarak tutulmaz.

Bunun yerine SHA-256 hash kullanılır:

```text
auth:refresh-token:{sha256(refreshToken)}
```

Bu sayede Redis dump veya log çıktılarında raw refresh token görünmez.

JWT blacklist içinde access token'ın tamamı değil, sadece `jti` claim değeri tutulur:

```text
auth:blacklist:jwt:{jti}
```

## Invalidation Kuralları

### JWT Blacklist

Access token revoke/logout edildiğinde:

```text
auth:blacklist:jwt:{jti}
```

key'i Redis'e yazılır.

TTL:

```text
access token kalan ömrü
```

### Refresh Token Cache

Login/register/refresh başarılı olduğunda refresh token cache'e yazılır.

Revoke veya token rotation sırasında eski refresh token cache'ten silinir.

### Notification Unread Count

Unread count ilk çağrıda DB'den hesaplanır ve Redis'e yazılır.

Notification okundu, tümü okundu veya silindi gibi işlemlerden sonra unread count tekrar DB'den hesaplanır ve cache güncellenir.

### SignalR Connection State

Kullanıcı SignalR hub'a bağlandığında connection id Redis set'e eklenir.

Kullanıcı ayrıldığında connection id Redis set'ten silinir.

Kullanıcının hiç connection'ı kalmazsa Redis key silinir.

## Kod Tarafındaki Varsayılanlar

Varsayılan TTL değerleri:

```text
shared/Flowist.Shared/Caching/CacheExpirationDefaults.cs
```

dosyasında tutulur.

## Cache Invalidation Stratejisi

Cache invalidation, cache'teki verinin gerçek veriyle uyumlu kalmasını sağlar.

Flowist içinde kullanılan invalidation kuralları aşağıdaki gibidir.

### Access Token Blacklist

Access token logout veya revoke edildiğinde token'ın `jti` değeri Redis'e yazılır.

Key formatı:

```text
auth:blacklist:jwt:{jti}
```

Invalidation tipi:

```text
TTL tabanlı otomatik silinme
```

Sebep:

```text
Access token süresi dolduktan sonra blacklist kaydına ihtiyaç kalmaz.
```

### Refresh Token Cache

Login veya register sonrası refresh token cache'e yazılır.

Refresh token rotation sırasında:

```text
Eski refresh token cache'ten silinir.
Yeni refresh token cache'e yazılır.
```

Revoke sırasında:

```text
İlgili refresh token cache'ten silinir.
```

Revoke all sırasında:

```text
Kullanıcının aktif tüm refresh token cache kayıtları silinir.
```

Key formatı:

```text
auth:refresh-token:{sha256(refreshToken)}
```

Invalidation tipi:

```text
Explicit delete + TTL tabanlı otomatik silinme
```

### Notification Unread Count Cache

Unread count cache'i şu endpointlerde etkilenir:

```text
GET /api/notifications/unread-count
PUT /api/notifications/{id}/read
PUT /api/notifications/read-all
DELETE /api/notifications/{id}
```

İlk okumada:

```text
Cache miss olursa DB'den sayılır ve Redis'e yazılır.
```

Notification read/delete gibi değişikliklerden sonra:

```text
Unread count DB'den tekrar hesaplanır.
Redis cache güncellenir.
SignalR ile kullanıcıya yeni sayı gönderilir.
```

Key formatı:

```text
notification:unread-count:{userId}
```

Invalidation tipi:

```text
Refresh-on-write
```

### SignalR Connection State

Kullanıcı SignalR hub'a bağlandığında:

```text
connectionId Redis set'e eklenir.
```

Kullanıcı ayrıldığında:

```text
connectionId Redis set'ten silinir.
```

Kullanıcının aktif connection'ı kalmazsa:

```text
Redis key tamamen silinir.
```

Key formatı:

```text
signalr:user-connections:{userId}
```

Invalidation tipi:

```text
Explicit remove + TTL fallback
```

TTL fallback, beklenmeyen disconnect veya servis crash durumunda stale connection bilgisinin kalıcı olmamasını sağlar.

## Cache Key Yönetimi

Cache key formatları merkezi olarak şu dosyada tanımlanır:

```text
shared/Flowist.Shared/Caching/CacheKeys.cs
```

Bu sayede key formatları servislerin içine dağılmaz ve ileride değişiklik tek nokt

```

```
