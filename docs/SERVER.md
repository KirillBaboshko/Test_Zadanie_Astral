# 🖥️ Server - Серверное приложение

Серверная часть ChatApp построена на ASP.NET Core Web API с использованием Clean Architecture.

## 📦 Структура

```
Server/
├── ChatApp.Server.Api/              # Web API слой
├── ChatApp.Server.Application/      # Бизнес-логика
├── ChatApp.Server.Domain/           # Доменная модель
└── ChatApp.Server.Infrastructure/   # Инфраструктура
```

---

## 🎯 ChatApp.Server.Api

**Назначение:** Presentation слой - HTTP API endpoints, контроллеры, middleware.

### Основные компоненты:

#### Controllers
- **AuthController** - Аутентификация и регистрация
  - `POST /api/auth/register` - Регистрация пользователя
  - `POST /api/auth/login` - Вход в систему
  
- **ChatController** - Операции с сообщениями
  - `POST /api/chat/messages` - Отправка сообщения (требует JWT)
  - `GET /api/chat/messages` - Получение всех сообщений
  - `GET /api/chat/messages/user/{userId}` - Сообщения пользователя по ID
  - `GET /api/chat/messages-for-name` - Сообщения по имени пользователя
  - `GET /api/chat/users` - Список пользователей
  - `GET /api/chat/about-user/{username}` - Информация о пользователе

#### Program.cs
Конфигурация приложения:
- **Kestrel** — два порта: HTTP/1.1 (REST) и HTTP/2 (gRPC)
- Регистрация сервисов (DI), JWT, Swagger, CORS
- MediatR + MassTransit (RabbitMQ)
- Миграции БД при старте
- Background services (`MessageCleanupService`, `OutboxPublisherService`)

**Порты (локально):**

| Переменная | По умолчанию | Протокол |
|------------|--------------|----------|
| `HTTP_PORT` | 5096 | HTTP/1.1 — REST API |
| `GRPC_PORT` | 5097 | HTTP/2 — gRPC |

**Порты (Docker):**

| Хост | Контейнер | Переменная |
|------|-----------|------------|
| 5096 | 8080 | `HTTP_PORT=8080` |
| 5097 | 8081 | `GRPC_PORT=8081` |

Kestrel использует `ListenAnyIP` (не `ListenLocalhost`) — обязательно для работы Docker port mapping.

#### gRPC Services (Code-first)
- **CodeFirstAuthService** — `IAuthService` (Register, Login)
- **CodeFirstChatService** — `IChatService` (SendMessage, GetMessages, StreamMessages, …)

gRPC-сервисы вызывают те же MediatR-команды, что и REST-контроллеры.

#### Message Bus Consumers
- **RegisterUserCommandConsumer**, **LoginUserCommandConsumer**, **SendMessageCommandConsumer**
- **UserRegisteredConsumer**, **UserLoggedInConsumer**, **MessageSentConsumer**

#### Background Services
- **MessageCleanupService** - Фоновая очистка сообщений
  - Интервал проверки: каждую минуту
  - Удаление сообщений старше 1 дня
  - Ограничение: максимум 10,000 сообщений
  - Использует `ExecuteDeleteAsync` для эффективности

### Зависимости:
```xml
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" />
<PackageReference Include="Swashbuckle.AspNetCore" />
<PackageReference Include="Grpc.AspNetCore" />
<PackageReference Include="protobuf-net.Grpc.AspNetCore" />
<PackageReference Include="MassTransit.RabbitMQ" />
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" />
```

---

## 💼 ChatApp.Server.Application

**Назначение:** Application слой - бизнес-логика, Commands, Queries, Handlers.

### Архитектура MediatR

Приложение использует паттерн медиатор через библиотеку MediatR:

```
Request → IMediator → Pipeline Behaviors → Handler → Response
```

**Подробнее:** См. [MEDIATR.md](MEDIATR.md)

### Commands (Команды для изменения состояния)

#### SendMessageCommand
- **Handler:** `SendMessageCommandHandler`
- **Что делает:**
  - Находит пользователя по ID
  - Добавляет сообщение через доменную модель
  - Публикует событие в Outbox (MessageSentEvent)
  - Возвращает данные сообщения
- **Behaviors:** Logging → UnitOfWork → Handler

### Use Cases (Legacy, постепенно мигрируют на MediatR)

#### Auth (Аутентификация)
- **RegisterUseCase** - Регистрация нового пользователя
  - Проверка уникальности username
  - Хеширование пароля (PBKDF2)
  - Создание пользователя
  - Генерация JWT токена
  
- **LoginUseCase** - Вход в систему
  - Поиск пользователя по username
  - Проверка пароля
  - Обновление `last_login`
  - Генерация JWT токена

#### Chat (Queries для чтения данных)
- **GetMessagesUseCase** - Получение сообщений
  - `ExecuteAsync` - все сообщения с фильтром по дате
  - `ExecuteForUserIdAsync` - сообщения конкретного пользователя (по ID)
  - `ExecuteForUsernameAsync` - сообщения по имени пользователя

- **GetUsersUseCase** - Получение списка всех пользователей

- **GetUserInfoUseCase** - Детальная информация о пользователе
  - Username, дата регистрации, последний вход
  - Количество отправленных сообщений

### Pipeline Behaviors (Cross-Cutting Concerns)

**Расположение:** `Application/Behaviors/`

- **LoggingBehavior** - Автоматическое логирование
  - Логирует начало выполнения запроса с параметрами
  - Измеряет время выполнения
  - Логирует успешное завершение или ошибку

- **UnitOfWorkBehavior** - Управление транзакциями
  - Автоматически сохраняет изменения в БД после Handler
  - Гарантирует транзакционность операций
  - Работает с Outbox Pattern для надежной доставки событий

### Services

- **IOutboxService / OutboxService** - Outbox Pattern
  - Сохранение событий в outbox_messages в той же транзакции
  - Гарантия надежной доставки событий в RabbitMQ

### Validation (FluentValidation):
- **RegisterRequestValidator** - Валидация регистрации
  - Username: 3-100 символов, только буквы, цифры, `_`, `-`
  - Password: минимум 6 символов
  
- **LoginRequestValidator** - Валидация входа
  - Обязательные поля username и password
  
- **SendMessageAuthRequestValidator** - Валидация сообщения
  - Content: 1-5000 символов

### Зависимости:
```xml
<PackageReference Include="FluentValidation" />
<PackageReference Include="FluentValidation.DependencyInjectionExtensions" />
<PackageReference Include="MediatR" Version="14.2.0" />
```

---

## 🏛️ ChatApp.Server.Domain

**Назначение:** Domain слой - доменная модель, бизнес-правила, интерфейсы репозиториев.

### Entities:

#### User (Aggregate Root)
```csharp
public class User
{
    public Guid Id { get; private set; }
    public String Username { get; private set; }
    public String PasswordHash { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastLogin { get; private set; }
    
    // Aggregate для сообщений (Owned Entity)
    private readonly List<ChatMessage> _messages = new();
    public IReadOnlyCollection<ChatMessage> Messages => _messages.AsReadOnly();
    
    // Методы для управления сообщениями
    public void AddMessage(String content);
    public void UpdateLastLogin();
}
```

**Особенности:**
- Aggregate Root для `ChatMessage`
- Инкапсуляция коллекции сообщений (backing field)
- Доменные методы для изменения состояния

#### ChatMessage (Owned Entity)
```csharp
public class ChatMessage
{
    public Guid Id { get; private set; }
    public String Content { get; private set; }
    public DateTime Timestamp { get; private set; }
    
    // Навигация к User управляется через Aggregate Root
}
```

**Особенности:**
- Owned Entity (не имеет собственного репозитория)
- Всегда создаётся через User.AddMessage()
- Не имеет navigation property к User

### Repositories (Интерфейсы):

#### IUserRepository
```csharp
Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
Task<User?> GetByUsernameAsync(String username, CancellationToken cancellationToken);
Task<List<User>> GetAllAsync(CancellationToken cancellationToken);
Task AddAsync(User user, CancellationToken cancellationToken);
Task<Boolean> ExistsAsync(String username, CancellationToken cancellationToken);
```

#### IMessageRepository (internal)
```csharp
Task<List<ChatMessage>> GetMessagesAsync(...);
Task<List<ChatMessage>> GetMessagesByUserIdAsync(...);
Task<List<ChatMessage>> GetMessagesByUsernameAsync(...);
Task<Int32> GetMessageCountAsync(...);
```

**Примечание:** `MessageRepository` - internal, используется только внутри Infrastructure слоя.

### Abstractions:

#### IUnitOfWork
```csharp
IUserRepository Users { get; }
Task<Int32> SaveChangesAsync(CancellationToken cancellationToken = default);
```

**Особенности:**
- Управление транзакциями
- Единая точка сохранения изменений
- EF Core ChangeTracker автоматически отслеживает изменения

### Зависимости:
Нет внешних зависимостей - чистая доменная модель.

---

## 🔧 ChatApp.Server.Infrastructure

**Назначение:** Infrastructure слой - реализация репозиториев, доступ к БД, внешние сервисы.

### Data (Entity Framework Core):

#### ApplicationDbContext
```csharp
public class ApplicationDbContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<ChatMessage> Messages { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
```

#### Entity Configurations:

**UserConfiguration:**
```csharp
- Users таблица
- Username: уникальный индекс, max length 100
- PasswordHash: max length 500
- Owned Entity: ChatMessage
  - Table: messages
  - Навигация: user_id (FK)
```

**ChatMessageConfiguration:**
```csharp
- Messages таблица (если доступна как DbSet)
- Content: max length 5000
- Индекс по Timestamp для быстрой сортировки
```

### Repository (Реализации):

#### UserRepository
```csharp
public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;
    
    // Реализация всех методов IUserRepository
    // Использует EF Core для доступа к БД
    // Include(_messages) для загрузки Owned Entity
}
```

#### MessageRepository (internal sealed)
```csharp
internal sealed class MessageRepository : IMessageRepository
{
    // Используется только внутри Infrastructure
    // Не регистрируется в DI
    // Прямой доступ к DbSet<ChatMessage>
}
```

### Persistence:

#### UnitOfWork
```csharp
public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    
    public IUserRepository Users { get; }
    
    public async Task<Int32> SaveChangesAsync(...)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }
}
```

**Особенности:**
- Единая транзакция для всех операций
- Автоматическое управление изменениями через EF ChangeTracker
- Не нужны отдельные методы `Update` в репозиториях

### Security (Безопасность):

#### PasswordHasher : IPasswordHasher
```csharp
public String HashPassword(String password)
{
    // PBKDF2 с 100,000 итерациями
    // SHA-256 хеш-функция
    // Генерация уникальной соли
    // Формат: salt:hash (Base64)
}

public Boolean VerifyPassword(String password, String hash)
{
    // Извлечение соли из хеша
    // Повторное хеширование
    // Сравнение результатов
}
```

#### JwtTokenGenerator : IJwtTokenGenerator
```csharp
public String GenerateToken(Guid userId, String username)
{
    // RSA SHA-256 подпись (2048 бит)
    // Claims: sub, unique_name, jti, nameid, name
    // Время жизни: 7 дней
    // Issuer и Audience из конфигурации
}
```

**Важно:** 
- RSA ключ создаётся при запуске (Singleton)
- При перезапуске все старые токены становятся невалидными

### Migrations:

**20260716091311_InitialWithAuth:**
- Создание таблиц users и messages
- Настройка связей и индексов
- Constraints и уникальные ключи

### Зависимости:
```xml
<PackageReference Include="Microsoft.EntityFrameworkCore" />
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" />
<PackageReference Include="Microsoft.IdentityModel.Tokens" />
<PackageReference Include="System.IdentityModel.Tokens.Jwt" />
```

---

## 🔐 Безопасность

### JWT Аутентификация
```csharp
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = configuration["Jwt:Audience"],
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = rsaSecurityKey
        };
    });
```

### Хеширование паролей
- Алгоритм: PBKDF2 (Password-Based Key Derivation Function 2)
- Итерации: 100,000
- Хеш-функция: SHA-256
- Длина ключа: 256 бит (32 байта)
- Уникальная соль для каждого пароля

---

## 📊 База данных

### Схема:

```sql
-- Users таблица
CREATE TABLE users (
    id UUID PRIMARY KEY,
    username VARCHAR(100) UNIQUE NOT NULL,
    password_hash VARCHAR(500) NOT NULL,
    created_at TIMESTAMP NOT NULL,
    last_login TIMESTAMP
);

-- Messages таблица (Owned Entity)
CREATE TABLE messages (
    id UUID PRIMARY KEY,
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    content TEXT NOT NULL,
    timestamp TIMESTAMP NOT NULL
);

-- Индексы
CREATE INDEX idx_username ON users(username);
CREATE INDEX idx_messages_timestamp ON messages(timestamp);
CREATE INDEX idx_messages_user_id ON messages(user_id);
```

### Миграции:

**Создание:**
```bash
cd src/Server/ChatApp.Server.Infrastructure
dotnet ef migrations add MigrationName --startup-project ../ChatApp.Server.Api
```

**Применение:**
```bash
dotnet ef database update --startup-project ../ChatApp.Server.Api
```

**Откат:**
```bash
dotnet ef migrations remove --startup-project ../ChatApp.Server.Api
```

---

## 🚀 Запуск

### Локально:

1. **Настроить PostgreSQL:**
```bash
# Создать базу данных
psql -U postgres
CREATE DATABASE chatapp;
```

2. **Настроить appsettings.json:**
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

3. **Запустить:**
```bash
cd src/Server/ChatApp.Server.Api
dotnet run
```

API будет доступен на: http://localhost:5096

### Через Docker:

```bash
docker compose up -d
```

API: http://localhost:5096 (REST), localhost:5097 (gRPC)

**Сервисы:** `postgres`, `rabbitmq`, `server`. Blazor (`blazor-client`) в compose закомментирован.

**Порты:** хост `5096 → 8080` (REST), `5097 → 8081` (gRPC). Переменные `HTTP_PORT=8080`, `GRPC_PORT=8081`.

**Полезные команды:**
```bash
docker compose down
docker compose logs -f server
docker compose up -d --build server
docker exec -it chatapp-postgres psql -U postgres -d chatapp
```

---

## 📚 Архитектурные принципы

### Clean Architecture
- ✅ Независимость от фреймворков
- ✅ Тестируемость
- ✅ Независимость от UI
- ✅ Независимость от БД
- ✅ Независимость от внешних агентств

### Domain-Driven Design
- ✅ **Aggregate Root** (User)
- ✅ **Owned Entity** (ChatMessage)
- ✅ Инкапсуляция бизнес-логики в доменных объектах
- ✅ Доменные методы вместо публичных setters

### SOLID принципы
- ✅ **S** - Single Responsibility (каждый Use Case - одна ответственность)
- ✅ **O** - Open/Closed (расширение через новые Use Cases)
- ✅ **L** - Liskov Substitution (интерфейсы репозиториев)
- ✅ **I** - Interface Segregation (узкие интерфейсы)
- ✅ **D** - Dependency Inversion (зависимость от абстракций)

### Паттерны
- ✅ **Repository** - абстракция доступа к данным
- ✅ **Unit of Work** - управление транзакциями
- ✅ **CQRS** - разделение команд и запросов через Use Cases
- ✅ **Dependency Injection** - инверсия зависимостей

---

## 🧪 Тестирование

### Unit-тесты

Проекты: `ChatApp.Server.Domain.Tests`, `ChatApp.Server.Application.Tests`, `ChatApp.Server.Infrastructure.Tests`.

**Стек:** NUnit, NUnit3TestAdapter, NSubstitute, coverlet.collector, Microsoft.NET.Test.Sdk.

```bash
dotnet test ChatApp.slnx
dotnet test src/Server/ChatApp.Server.Application.Tests
dotnet test ChatApp.slnx --collect:"XPlat Code Coverage" --results-directory ./TestResults
```

Тесты используют паттерн **AAA** (Arrange, Act, Assert) и `[TestCase]` для параметризации.

### API тесты (ручные):

```bash
# Регистрация
curl -X POST http://localhost:5096/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"username":"testuser","password":"test123"}'

# Логин
curl -X POST http://localhost:5096/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"testuser","password":"test123"}'

# Отправка сообщения
curl -X POST http://localhost:5096/api/chat/messages \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer TOKEN" \
  -d '{"content":"Hello World!"}'
```

### Swagger UI:
http://localhost:5096

---

## 📈 Производительность

### MessageCleanupService
- Использует `ExecuteDeleteAsync` для массового удаления
- Не загружает сущности в память
- Эффективные SQL запросы напрямую в БД

### Repository
- `AsNoTracking()` для read-only операций
- Индексы на часто используемых полях
- Пагинация для больших выборок

### EF Core
- Compiled Queries для повторяющихся запросов
- Connection pooling
- Оптимизация через Include для Owned Entities

---

[← Назад к главной](../README.md) | [Клиент →](./CLIENT.md)
