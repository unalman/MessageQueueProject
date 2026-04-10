# .NET 10 + RabbitMQ Pub/Sub E‑Ticaret Demo

Bu repo; `OrderApi` üzerinden “satın alma” çağrısı alıp, `PaymentApi` ile basit ödeme doğrulaması yapan ve ödeme başarılıysa RabbitMQ’ya event publish ederek iki ayrı subscriber’ın (mail + stok) çalıştığı bir pub/sub örneğidir.

## Servisler

- **RabbitMQ**: Event exchange + queue’lar
  - Management UI: `http://localhost:15672` (user/pass: `guest/guest`)
- **PaymentApi**: `POST /payments/charge` (demo ödeme)
- **OrderApi**: `POST /orders/purchase` (satın alma)
- **EmailWorker**: `OrderPaid` event’ini tüketir, “mail gönderildi” diye loglar
- **StockWorker**: `OrderPaid` event’ini tüketir, in-memory stok düşürür ve loglar

## İş Akışı (Şu Anki Akış)

1. **Satın alma isteği**: Sen `OrderApi`’ye `POST /orders/purchase` çağırırsın.
2. **Ödeme çağrısı**: `OrderApi`, `PaymentApi`’ye `POST /payments/charge` isteği atar.
3. **Ödeme sonucu**:
   - `amount > 0` ise `PaymentApi` 200 OK döndürür.
   - `amount <= 0` ise `PaymentApi` 400 döndürür.
4. **Event publish** (sadece ödeme başarılıysa): `OrderApi`, RabbitMQ’ya `OrderPaid` event’ini JSON olarak publish eder.
   - Exchange: `ecommerce.events` (type: topic)
   - Routing key: `order.paid`
   - Mesaj: `Contracts/OrderPaid`
5. **Subscriber’lar**:
   - **EmailWorker**: `email.orderpaid` queue’sunu exchange’e bind eder, `OrderPaid` gelince “Mail sent …” loglar.
   - **StockWorker**: `stock.orderpaid` queue’sunu exchange’e bind eder, `OrderPaid` gelince stok düşürür (in-memory) ve loglar.

Not: Worker’lar ve OrderApi tarafında RabbitMQ bağlantısı için retry/backoff vardır; RabbitMQ hazır olmadan kapanmamalıdır. Ayrıca `docker-compose.yml` içinde RabbitMQ healthcheck vardır.

## Çalıştırma (Docker)

```bash
docker compose up --build
```

Logları izlemek için:

```bash
docker compose logs -f orderapi paymentapi emailworker stockworker
```

## Örnek İstekler

IDE HTTP dosyaları:
- `src/OrderApi/OrderApi.http` (purchase success/fail)
- `src/PaymentApi/PaymentApi.http` (charge success/fail)

Akışı görmek için en temel adım: `src/OrderApi/OrderApi.http` içindeki **Purchase (success)** request’ini gönderip worker loglarını izlemek.

