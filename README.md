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

API будет доступен по адресу: **http://localhost:5096**

Подробнее: [DOCKER.md](./DOCKER.md)

### Локальный запуск

**Требования:**
- .NET 10 SDK
- PostgreSQL 16

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

### Технологии
- **.NET 10** - Последняя версия .NET
- **ASP.NET Core** - Web API фреймворк
- **Entity Framework Core** - ORM для работы с БД
- **PostgreSQL 16** - Реляционная база данных
- **FluentValidation** - Валидация запросов
- **JWT Bearer** - Аутентификация
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

### Аутентификация
```
POST /api/auth/register - Регистрация нового пользователя
POST /api/auth/login    - Вход в систему
```

### Сообщения
```
POST /api/chat/messages              - Отправка сообщения (требует JWT)
GET  /api/chat/messages              - Получение всех сообщений
GET  /api/chat/messages/user/{id}    - Сообщения конкретного пользователя
GET  /api/chat/messages-for-name     - Сообщения по имени пользователя
```

### Пользователи
```
GET /api/chat/users           - Список всех пользователей
GET /api/chat/about-user/{id} - Информация о пользователе
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
- Docker контейнеризацию multi-stage builds
- Docker Compose оркестрацию

## 🔧 Требования

### Для разработки:
- .NET 10 SDK
- PostgreSQL 16
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
