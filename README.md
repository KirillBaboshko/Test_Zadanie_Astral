# ChatApp — распределённое чат-приложение

Чат-приложение на **.NET 10** с Clean Architecture: JWT-аутентификация, PostgreSQL, RabbitMQ, несколько протоколов коммуникации и Docker-развёртывание.

## Структура solution

```
ChatApp.slnx
├── src/Server/
│   ├── ChatApp.Server.Api              # REST + gRPC + Message Bus consumers
│   ├── ChatApp.Server.Application      # MediatR, Use Cases, Behaviors
│   ├── ChatApp.Server.Domain           # Доменная модель
│   ├── ChatApp.Server.Infrastructure   # EF Core, JWT, репозитории
│   ├── ChatApp.Server.*.Tests          # Unit-тесты (NUnit)
├── src/Client/
│   ├── ChatApp.Client.Blazor           # Web UI (Blazor WebAssembly)
│   ├── ChatApp.Client.Console          # Консольный клиент
│   ├── ChatApp.Client.Application      # Интерфейсы клиента
│   └── ChatApp.Client.Infrastructure   # HTTP / gRPC / RabbitMQ клиенты
└── src/Shared/
    ├── ChatApp.Contracts               # REST DTO
    ├── ChatApp.Shared.Protos           # gRPC code-first контракты
    └── ChatApp.Shared.Messages         # RabbitMQ Commands + Events
```

## Быстрый старт

### Вариант 1: Docker (бэкенд) + Blazor (локально) — рекомендуется

```bash
# Терминал 1: инфраструктура и API
docker compose up -d

# Терминал 2: веб-клиент
cd src/Client/ChatApp.Client.Blazor
dotnet run --launch-profile http
```

| Сервис | URL |
|--------|-----|
| Blazor UI | http://localhost:5073 |
| REST API | http://localhost:5096 |
| gRPC | localhost:5097 |
| RabbitMQ UI | http://localhost:15672 (guest/guest) |

### Вариант 2: Полностью локально

**Требования:** .NET 10 SDK, PostgreSQL 16, RabbitMQ

```bash
# Сервер
cd src/Server/ChatApp.Server.Api
dotnet run

# Консольный клиент
cd src/Client/ChatApp.Client.Console
dotnet run

# Blazor
cd src/Client/ChatApp.Client.Blazor
dotnet run --launch-profile http
```

## Docker

Проект использует Docker Compose для инфраструктуры и сервера. **Blazor запускается локально** (вне Docker).

| Сервис | Контейнер | Порт на хосте |
|--------|-----------|---------------|
| `postgres` | chatapp-postgres | 5432 |
| `rabbitmq` | chatapp-rabbitmq | 5672, 15672 |
| `server` | chatapp-server | 5096 (HTTP), 5097 (gRPC) |
| `client` | chatapp-client | профиль `client` |

```bash
docker compose up -d
docker compose down
docker compose logs -f server
docker compose up -d --build server
```

**Маппинг портов сервера:** хост `5096 → 8080` (REST), `5097 → 8081` (gRPC). В контейнере заданы `HTTP_PORT=8080`, `GRPC_PORT=8081`. Kestrel слушает `ListenAnyIP` — иначе API недоступен с хоста.

**Blazor + Docker backend:** `docker compose up -d`, затем `dotnet run --launch-profile http` в `ChatApp.Client.Blazor`. API: `http://localhost:5096` из `wwwroot/appsettings.json`. Используйте http-профиль, не https (mixed content).

**Консольный клиент в Docker:** `docker compose --profile client run --rm client`

Swagger в Docker недоступен (`Production`) — для Swagger запускайте сервер локально через `dotnet run`.

Подробнее о сервере и Docker: [SERVER.md](./docs/SERVER.md)

## Клиенты

| Клиент | Протокол | Запуск |
|--------|----------|--------|
| **Blazor** | HTTP REST | `dotnet run` (локально, порт 5073) |
| **Console** | HTTP / gRPC / RabbitMQ | выбор при старте |
| **Console (Docker)** | HTTP | `docker compose --profile client run --rm client` |

Blazor настраивается через `src/Client/ChatApp.Client.Blazor/wwwroot/appsettings.json`:

```json
{ "ApiBaseUrl": "http://localhost:5096" }
```

## Протоколы коммуникации

Все три протокола ведут к **одной бизнес-логике** (MediatR → Application):

```
Blazor / Console (HTTP) ──► REST Controllers ──┐
Console (gRPC)          ──► CodeFirst Services ├──► MediatR ──► Domain ──► PostgreSQL
Console (RabbitMQ)      ──► Command Consumers ┘
```

| Протокол | Порт | Назначение |
|----------|------|------------|
| HTTP REST | 5096 | Blazor, Swagger, универсальный API |
| gRPC (code-first) | 5097 | Альтернатива REST для консоли, server streaming |
| RabbitMQ | 5672 | Real-time события, async команды |

## API (HTTP REST)

### Аутентификация
```
POST /api/auth/register
POST /api/auth/login
```

### Сообщения
```
POST /api/chat/messages              # JWT required
GET  /api/chat/messages
GET  /api/chat/messages/user/{id}
GET  /api/chat/messages-for-name
```

### Пользователи
```
GET /api/chat/users
GET /api/chat/about-user/{id}
```

Swagger (только Development): http://localhost:5096

## Технологии

- ASP.NET Core Web API, Blazor WebAssembly
- Entity Framework Core + PostgreSQL 16
- JWT (RSA SHA-256), PBKDF2 для паролей
- MediatR, FluentValidation, CQRS
- gRPC code-first (protobuf-net.Grpc)
- MassTransit + RabbitMQ
- Docker Compose
- NUnit, NSubstitute, coverlet

## Тестирование

Unit-тесты: **NUnit**, моки **NSubstitute**, покрытие **coverlet**.

```
src/Server/
├── ChatApp.Server.Domain.Tests/
├── ChatApp.Server.Application.Tests/
└── ChatApp.Server.Infrastructure.Tests/
```

```bash
dotnet test ChatApp.slnx
dotnet test ChatApp.slnx --collect:"XPlat Code Coverage"
```

Тесты следуют паттерну **AAA** (Arrange, Act, Assert), для параметризации — `[TestCase]`. Подробнее: [SERVER.md](./docs/SERVER.md)

## Документация

- [Сервер](./docs/SERVER.md)
- [Клиент](./docs/CLIENT.md)
- [Shared](./docs/SHARED.md)
- [Декораторы](./docs/DECORATORS.md)

## Миграции БД

```bash
cd src/Server/ChatApp.Server.Infrastructure
dotnet ef migrations add MigrationName --startup-project ../ChatApp.Server.Api
dotnet ef database update --startup-project ../ChatApp.Server.Api
```

---

Тестовое задание для Astral
