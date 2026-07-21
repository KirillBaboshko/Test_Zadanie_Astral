# 💬 ChatApp - Распределённое чат-приложение

Полнофункциональное чат-приложение на .NET 10 с архитектурой клиент-сервер, JWT аутентификацией и Docker развёртыванием.

## 📋 Описание

ChatApp - это современное чат-приложение, построенное на микросервисной архитектуре с использованием Clean Architecture и Domain-Driven Design принципов. Проект демонстрирует лучшие практики разработки enterprise-приложений на .NET.

## 🏗️ Архитектура проекта

Проект состоит из трёх основных солюшенов:

### 1. [Server](./docs/SERVER.md) - Серверное приложение
- **ChatApp.Server.Api** - ASP.NET Core Web API
- **ChatApp.Server.Application** - Бизнес-логика (Use Cases)
- **ChatApp.Server.Domain** - Доменная модель
- **ChatApp.Server.Infrastructure** - Инфраструктура (БД, безопасность)

### 2. [Client](./docs/CLIENT.md) - Клиентское приложение
- **ChatApp.Client.Console** - Консольное приложение
- **ChatApp.Client.Application** - Логика клиента
- **ChatApp.Client.Infrastructure** - HTTP клиент

### 3. [Shared](./docs/SHARED.md) - Общие компоненты
- **ChatApp.Contracts** - Контракты (DTO, запросы, ответы)

## 🚀 Быстрый старт

### Запуск через Docker (рекомендуется)

```bash
# Сборка и запуск
docker-compose up -d

# Проверка статуса
docker-compose ps

# Просмотр логов
docker-compose logs -f server
```

Сервисы:
- **API**: http://localhost:5096
- **RabbitMQ Management UI**: http://localhost:15672 (guest/guest)

Подробнее: [DOCKER.md](./DOCKER.md)

### Локальный запуск

**Требования:**
- .NET 10 SDK
- PostgreSQL 16
- RabbitMQ (опционально, для message bus)

**Запуск RabbitMQ (через Docker):**
```bash
.\start-rabbitmq.ps1
```

**Запуск сервера:**
```bash
cd src/Server/ChatApp.Server.Api
dotnet run
```

**Запуск клиента:**
```bash
cd src/Client/ChatApp.Client.Console
dotnet run
```

## 🎯 Основные возможности

### Функциональность
- ✅ Регистрация и аутентификация пользователей
- ✅ JWT токены для безопасности (RSA SHA-256)
- ✅ Отправка и получение сообщений в реальном времени
- ✅ Просмотр истории сообщений
- ✅ Фильтрация сообщений по пользователю
- ✅ Автоматическая очистка старых сообщений (MessageCleanupService)
- ✅ Множественные протоколы коммуникации:
  - **HTTP REST API** - Традиционный RESTful подход
  - **gRPC Code-first** - RPC с protobuf-net.Grpc
- ✅ Асинхронная шина сообщений (MassTransit + RabbitMQ):
  - Публикация событий (регистрация, вход, отправка сообщений)
  - Асинхронная обработка через consumers
  - Готовность к Outbox паттерну

### Технологии
- **.NET 10** - Последняя версия .NET
- **ASP.NET Core** - Web API фреймворк
- **Entity Framework Core** - ORM для работы с БД
- **PostgreSQL 16** - Реляционная база данных
- **FluentValidation** - Валидация запросов
- **JWT Bearer** - Аутентификация
- **gRPC** - Code-first подход для RPC коммуникации
- **MassTransit + RabbitMQ** - Асинхронная шина сообщений
- **Docker** - Контейнеризация

### Архитектурные паттерны
- ✅ **Clean Architecture** - Слоистая архитектура
- ✅ **Domain-Driven Design** - Aggregate Root, Value Objects
- ✅ **CQRS** - Разделение команд и запросов (Use Cases)
- ✅ **Repository Pattern** - Абстракция доступа к данным
- ✅ **Unit of Work** - Управление транзакциями
- ✅ **Dependency Injection** - Инверсия зависимостей

## 📊 Структура базы данных

```sql
users
├── id (uuid, PK)
├── username (varchar, unique)
├── password_hash (varchar)
├── created_at (timestamp)
└── last_login (timestamp)

messages
├── id (uuid, PK)
├── user_id (uuid, FK -> users)
├── content (text)
└── timestamp (timestamp)
```

## 🔐 Безопасность

### Аутентификация
- JWT токены с RSA SHA-256 подписью (2048 бит)
- Время жизни токена: 7 дней
- При перезапуске сервера старые токены становятся невалидными

### Хеширование паролей
- PBKDF2 алгоритм
- 100,000 итераций
- SHA-256 хеш-функция
- Уникальная соль для каждого пользователя

## 📡 API Endpoints

### HTTP REST API

#### Аутентификация
```
POST /api/auth/register - Регистрация нового пользователя
POST /api/auth/login    - Вход в систему
```

#### Сообщения
```
POST /api/chat/messages              - Отправка сообщения (требует JWT)
GET  /api/chat/messages              - Получение всех сообщений
GET  /api/chat/messages/user/{id}    - Сообщения конкретного пользователя
GET  /api/chat/messages-for-name     - Сообщения по имени пользователя
```

#### Пользователи
```
GET /api/chat/users           - Список всех пользователей
GET /api/chat/about-user/{id} - Информация о пользователе
```

### gRPC API (Code-first)

**Порт:** 5097

**Сервисы:**
- `IAuthService` - Регистрация и аутентификация
- `IChatService` - Отправка/получение сообщений, streaming

**DNS Round-Robin:** Настроен пулинг HTTP/2 соединений для балансировки нагрузки

### Message Bus Commands (RabbitMQ)

**Команды от клиента → серверу:**
- `RegisterUserCommand` → регистрация (Request-Response)
- `LoginUserCommand` → вход (Request-Response)
- `SendMessageCommand` → отправка сообщения (fire-and-forget)

**Ответы сервера → клиенту:**
- `RegisterUserResponse` - токен и данные пользователя
- `LoginUserResponse` - токен и данные пользователя

**Request-Response паттерн:** Клиент отправляет команду и ждёт ответ (для Auth)  
**Fire-and-forget паттерн:** Клиент отправляет команду без ожидания (для SendMessage)

### Message Bus Events (RabbitMQ)

**Публикуемые события:**
- `UserRegisteredEvent` - Пользователь зарегистрирован
- `UserLoggedInEvent` - Пользователь вошёл в систему
- `MessageSentEvent` - Сообщение отправлено

**Server Consumers (обработка событий):**
- `UserRegisteredConsumer` - Обработка регистрации (welcome действия)
- `UserLoggedInConsumer` - Логирование входов
- `MessageSentConsumer` - Аналитика сообщений

**Client Consumers (получение событий в реальном времени):**
- `MessageSentEventConsumer` - Отображение новых сообщений
- `UserRegisteredEventConsumer` - Уведомления о новых пользователях

**Архитектура Message Bus:**
```
Клиент A отправляет:
  SendMessageCommand → RabbitMQ → Server Consumer → БД → MessageSentEvent

Клиент B получает:
  MessageSentEvent → RabbitMQ → Client Consumer → отображение в консоли

Результат: Реальное время как в Telegram/Slack!
```

Подробная документация API: [Swagger UI](http://localhost:5096) (после запуска)

## 🧪 Тестирование

### Тест API через PowerShell
```bash
.\test-api.ps1
```

### Ручное тестирование

**Регистрация:**
```bash
curl -X POST http://localhost:5096/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"username":"testuser","password":"test123"}'
```

**Отправка сообщения:**
```bash
curl -X POST http://localhost:5096/api/chat/messages \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{"content":"Hello World!"}'
```

## 🛠️ Разработка

### Структура проекта
```
Test_Zadanie_Astral/
├── src/
│   ├── Server/           # Серверная часть
│   ├── Client/           # Клиентская часть
│   └── Shared/           # Общие компоненты
├── docs/                 # Документация
├── docker-compose.yml    # Docker конфигурация
├── database-setup.sql    # Инициализация БД
├── clear-data.sql        # Очистка данных
└── test-api.ps1         # Тестовый скрипт

```

### Миграции базы данных

**Создание миграции:**
```bash
cd src/Server/ChatApp.Server.Infrastructure
dotnet ef migrations add MigrationName --startup-project ../ChatApp.Server.Api
```

**Применение миграций:**
```bash
dotnet ef database update --startup-project ../ChatApp.Server.Api
```

### Фоновые сервисы

**MessageCleanupService** - Автоматическая очистка сообщений:
- Интервал проверки: каждую минуту
- Срок хранения: 1 день
- Лимит сообщений: 10,000

## 📚 Документация

- [Серверное приложение](./docs/SERVER.md)
- [Клиентское приложение](./docs/CLIENT.md)
- [Общие компоненты](./docs/SHARED.md)
- [Docker развёртывание](./DOCKER.md)

## 🐳 Docker

Проект полностью контейнеризован и готов к развёртыванию:

```bash
# Запуск всех сервисов
docker-compose up -d

# Остановка
docker-compose down

# Просмотр логов
docker-compose logs -f

# Подключение к БД
docker exec -it chatapp-postgres psql -U postgres -d chatapp
```

Подробнее: [DOCKER.md](./DOCKER.md)

## 📝 Конфигурация

### appsettings.json (Server)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=chatapp;Username=postgres;Password=postgres"
  },
  "Jwt": {
    "Issuer": "ChatApp",
    "Audience": "ChatApp.Client"
  }
}
```

### Переменные окружения (Docker)
```env
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__DefaultConnection=Host=postgres;Port=5432;Database=chatapp;...
Jwt__Issuer=ChatApp
Jwt__Audience=ChatApp.Client
```

## 🎓 Изученные концепции

Этот проект демонстрирует:
- Clean Architecture и разделение ответственности
- Domain-Driven Design с Aggregate Root
- CQRS паттерн через Use Cases
- Repository и Unit of Work паттерны
- JWT аутентификацию с RSA подписью
- PBKDF2 хеширование паролей
- Entity Framework Core с миграциями
- FluentValidation для валидации
- Background Services в ASP.NET Core
- gRPC Code-first с protobuf-net.Grpc
- DNS Round-Robin балансировка для gRPC
- MassTransit + RabbitMQ для асинхронной коммуникации
- Event-Driven Architecture с message bus
- Готовность к Transactional Outbox паттерну
- Docker контейнеризацию multi-stage builds
- Docker Compose оркестрацию

## 🔧 Требования

### Для разработки:
- .NET 10 SDK
- PostgreSQL 16
- RabbitMQ (опционально, для message bus)
- Visual Studio 2022 / Rider / VS Code
- Docker Desktop (опционально)

### Для запуска через Docker:
- Docker Desktop
- Docker Compose

## 📄 Лицензия

Этот проект создан в учебных целях.

## 👤 Автор

Тестовое задание для Astral

---

**Готово к использованию!** 🚀

Для быстрого старта: `docker-compose up -d && .\test-api.ps1`

## 📐 Архитектурная диаграмма

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                               CLIENT                                         │
│  ┌──────────────────────────────────────────────────────────────────────┐   │
│  │  ChatApp.Client.Console                                              │   │
│  │  - Выбор протокола:                                                  │   │
│  │    1. HTTP REST                                                      │   │
│  │    2. gRPC Code-first                                                │   │
│  │    3. Message Bus (Async RabbitMQ) ← НОВОЕ!                         │   │
│  └───────────────────┬──────────────────────────────────────────────────┘   │
│                      │                                                       │
│  ┌───────────────────▼──────────────────────────────────────────────────┐   │
│  │  ChatApp.Client.Infrastructure                                       │   │
│  │  ┌──────────────┐  ┌─────────────────┐  ┌─────────────────────────┐ │   │
│  │  │ HttpClient   │  │ CodeFirstGrpc   │  │ MessageBusApiClient     │ │   │
│  │  │ REST API     │  │ DNS Round-Robin │  │ Commands + Events       │ │   │
│  │  └──────────────┘  └─────────────────┘  └─────────────────────────┘ │   │
│  │                                          │ Client Consumers:        │ │   │
│  │                                          │ - MessageSentEvent       │ │   │
│  │                                          │ - UserRegisteredEvent    │ │   │
│  └──────────────────────────────────────────────────────────────────────┘   │
└──────────────┬──────────────┬───────────────────┬────────────────────────────┘
               │ HTTP :5096   │ gRPC :5097        │ RabbitMQ :5672
               │              │                   │ (Commands + Events)
┌──────────────▼──────────────▼───────────────────▼────────────────────────────┐
│                               SERVER                                          │
│  ┌────────────────────────────────────────────────────────────────────────┐  │
│  │  ChatApp.Server.Api (ASP.NET Core)                                     │  │
│  │  ┌────────────┐  ┌──────────────┐  ┌───────────────────────────────┐  │  │
│  │  │ REST       │  │ gRPC Code-   │  │ Message Bus Consumers         │  │  │
│  │  │ Controllers│  │ first        │  │ КОМАНДЫ:                      │  │  │
│  │  └────────────┘  └──────────────┘  │ - RegisterUserCommand         │  │  │
│  │                                     │ - LoginUserCommand            │  │  │
│  │                                     │ - SendMessageCommand          │  │  │
│  │                                     │ СОБЫТИЯ (для аналитики):      │  │  │
│  │                                     │ - MessageSent                 │  │  │
│  │                                     │ - UserRegistered              │  │  │
│  │                                     │ - UserLoggedIn                │  │  │
│  │                                     └───────────────────────────────┘  │  │
│  └────────┬───────────────────────────────────────────────────────────────┘  │
│           │                                                                   │
│  ┌────────▼───────────────────────────────────────────────────────────────┐  │
│  │  ChatApp.Server.Application (Use Cases)                               │  │
│  │  - RegisterUseCase    ──┐                                             │  │
│  │  - LoginUseCase       ──┼── Публикуют события                        │  │
│  │  - SendMessageUseCase ──┘   в RabbitMQ через                         │  │
│  │                             IPublishEndpoint                           │  │
│  └────────┬───────────────────────────────────────────────────────────────┘  │
│           │                                                                   │
│  ┌────────▼───────────────────────────────────────────────────────────────┐  │
│  │  ChatApp.Server.Domain                                                 │  │
│  │  - User (Aggregate Root)                                               │  │
│  │  - ChatMessage (Entity)                                                │  │
│  └────────┬───────────────────────────────────────────────────────────────┘  │
│           │                                                                   │
│  ┌────────▼───────────────────────────────────────────────────────────────┐  │
│  │  ChatApp.Server.Infrastructure                                         │  │
│  │  - Repository (EF Core)                                                │  │
│  │  - Unit of Work                                                        │  │
│  │  - JWT Token Generator (RSA SHA-256)                                   │  │
│  │  - Password Hasher (PBKDF2)                                            │  │
│  └────────┬───────────────────────────────────────────────────────────────┘  │
│           │                                                                   │
└───────────┼───────────────────────────────────────────────────────────────────┘
            │
   ┌────────┴─────────┬─────────────────────┐
   │                  │                     │
   ▼                  ▼                     ▼
┌──────────┐   ┌─────────────┐   ┌─────────────────────────┐
│PostgreSQL│   │  RabbitMQ   │   │  MassTransit            │
│  :5432   │   │   :5672     │   │                         │
│          │   │  Management │   │  SERVER Consumers:      │
│ users    │   │   UI :15672 │   │  ┌───────────────────┐  │
│ messages │   │             │   │  │ Command Handlers: │  │
│          │   │  Exchanges: │   │  │ - RegisterUser    │  │
└──────────┘   │  - Commands │   │  │ - LoginUser       │  │
               │  - Events   │   │  │ - SendMessage     │  │
               │             │   │  └───────────────────┘  │
               │  Queues:    │   │  ┌───────────────────┐  │
               │  - RegisterU│   │  │ Event Handlers:   │  │
               │  - LoginUser│   │  │ - MessageSent     │  │
               │  - SendMessa│   │  │ - UserRegistered  │  │
               │  - UserRegis│   │  │ - UserLoggedIn    │  │
               │  - MessageSe│   │  └───────────────────┘  │
               └─────────────┘   │                         │
                                 │  CLIENT Consumers:      │
                                 │  ┌───────────────────┐  │
                                 │  │ - MessageSent     │  │
                                 │  │ - UserRegistered  │  │
                                 │  └───────────────────┘  │
                                 └─────────────────────────┘

SHARED COMPONENTS:
├── ChatApp.Contracts (HTTP REST DTO)
├── ChatApp.Shared.Protos (gRPC Code-first контракты)
└── ChatApp.Shared.Messages (RabbitMQ Commands + Events) ← НОВОЕ!
    ├── Commands/ (клиент → сервер)
    │   ├── RegisterUserCommand
    │   ├── LoginUserCommand
    │   └── SendMessageCommand
    ├── Responses/ (сервер → клиент)
    │   ├── RegisterUserResponse
    │   └── LoginUserResponse
    └── Events/ (broadcast всем)
        ├── UserRegisteredEvent
        ├── UserLoggedInEvent
        └── MessageSentEvent

ПРОТОКОЛЫ КОММУНИКАЦИИ:
1. HTTP REST API (порт 5096) - Традиционный подход
2. gRPC Code-first (порт 5097) - RPC с DNS Round-Robin
3. RabbitMQ Message Bus (порт 5672) - Асинхронные команды и события
   - Commands (Request-Response): RegisterUser, LoginUser
   - Commands (Fire-and-Forget): SendMessage
   - Events (Broadcast): MessageSent, UserRegistered, UserLoggedIn

FLOW MESSAGE BUS:
Клиент A → SendMessageCommand → RabbitMQ → Server Consumer → БД
                                                ↓
                                         MessageSentEvent
                                                ↓
                      Клиенты B,C,D... ← RabbitMQ ← Publish

Результат: Все клиенты видят сообщения в реальном времени!
```

