# Event Versioning Strategy

Flowist servisleri RabbitMQ uzerinden `Flowist.Shared.Events` altindaki integration event record'larini paylasir. Event contract'lari servisler arasi public API kabul edilir; bu nedenle entity modeli gibi serbest degistirilmez.

## Kurallar

1. Mevcut event alanlari silinmez veya yeniden adlandirilmaz.
2. Mevcut alanlarin tipi degistirilmez. Tip degisikligi gerekiyorsa yeni event tipi veya explicit version kullanilir.
3. Yeni alan eklenecekse geriye uyumlu olmali ve consumer tarafinda opsiyonel kabul edilmelidir.
4. Tum event'ler envelope alanlarini korur: `eventId`, `occurredOn`, `correlationId`.
5. Consumer eklenirken contract testine hangi event tipini tükettigi yazilir.
6. Breaking change gerekiyorsa eski event bir sure publish edilmeye devam eder, yeni event ayri consumer ile devreye alinir.

## Test Beklentisi

`Flowist.ContractTests` su iki garanti icin vardir:

- Event'ler JSON olarak serialize/deserialize oldugunda veri kaybi olmamali.
- NotificationService ve ActivityService consumer siniflari beklenen event tipleriyle eslesmeli.

Bu dosya Sprint 16 T-326 icin event versioning stratejisini tanimlar.