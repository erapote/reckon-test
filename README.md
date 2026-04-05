# POS Event Processing API

A minimal ASP.NET Core 10 Web API that accepts, stores, and asynchronously
processes Point-of-Sale events.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

## Run

```bash
cd src/Reckon.PointOfSale.WebApi
dotnet run
```

The API listens on `http://localhost:5238` by default.

OpenAPI schema is available at `http://localhost:5238/openapi/v1.json`
(development environment only).

## Endpoints

| Method | Path | Description |
|--------|------|-------------|
| `POST` | `/events` | Accept an event for async processing |
| `GET`  | `/events` | Return all stored events |
| `GET`  | `/events/{id}` | Return a single event by ID |

## Example: POST /events

```bash
curl -X POST http://localhost:5238/events \
  -H "Content-Type: application/json" \
  -d '{
    "eventType": "OrderPlaced",
    "timestamp": "2025-04-03T10:00:00Z",
    "payload": { "orderId": "ORD-001", "total": 49.99 }
  }'
```

```bash
curl -X POST http://localhost:5238/events \
  -H "Content-Type: application/json" \
  -d '{
    "eventType": "UserCreated",
    "timestamp": "2025-04-03T10:00:00Z",
    "payload": { "userId": "USR-42", "email": "jane@example.com" }
  }'
```

## Supported Event Types

| Event Type | Handler Behaviour |
|------------|-------------------|
| `UserCreated` | Simulates user provisioning (welcome e-mail, etc.) |
| `OrderPlaced` | Simulates fulfilment kick-off (inventory, payment) |
| _(anything else)_ | `FallbackHandler` logs a warning; event is stored |

## Architecture Overview

```
POST /events
    │
    ├─► InMemoryEventStore.Add()        (synchronous, immediate)
    │
    └─► Channel<EventRecord>.WriteAsync()
              │
              ▼
        EventProcessorService           (BackgroundService)
              │
              ├─► UserCreatedHandler
              ├─► OrderPlacedHandler
              └─► FallbackHandler       (catch-all)
```

See [write-up.md](./docs/write-up.md) for tradeoffs, assumptions, and improvement ideas.

Test the API calls using the Reckon.PointOfSale.WebApi.Http file