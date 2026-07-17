# 🔗 Shared - Общие компоненты

Shared солюшен содержит контракты и DTO, используемые как сервером, так и клиентом.

## 📦 Структура

```
Shared/
└── ChatApp.Contracts/    # Контракты, DTO, запросы и ответы
    ├── Messages/         # DTO для сообщений
    ├── Requests/         # Модели запросов
    └── Responses/        # Модели ответов
```

---

## 🎯 ChatApp.Contracts

**Назначение:** Общие контракты для коммуникации между клиентом и сервером.

### Принципы:
- ✅ Независимость от реализации
- ✅ Простые POCO объекты
- ✅ Единая точка истины для API контрактов
- ✅ Версионирование через namespace

---

## 📨 Messages (DTO)

### ChatMessageDto
```csharp
/// <summary>
/// DTO для сообщения в чате
/// </summary>
public sealed class ChatMessageDto
{
    /// <summary>
    /// Уникальный идентификатор сообщения
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// Имя отправителя
    /// </summary>
    public String SenderName { get; set; } = String.Empty;
    
    /// <summary>
    /// Содержимое сообщения
    /// </summary>
    public String Content { get; set; } = String.Empty;
    
    /// <summary>
    /// Время отправки (UTC)
    /// </summary>
    public DateTime Timestamp { get; set; }
}
```

**Использование:**
- Возвращается из API при получении сообщений
- Отображается в клиенте
- Используется в ChatPollingService для фильтрации

**Особенности:**
- SenderName вместо UserId для удобства отображения
- Timestamp в UTC для консистентности
- Sealed класс для оптимизации

### UserDto
```csharp
/// <summary>
/// DTO для пользователя
/// </summary>
public sealed class UserDto
{
    /// <summary>
    /// Уникальный идентификатор пользователя
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// Имя пользователя
    /// </summary>
    public String Username { get; set; } = String.Empty;
}
```

**Использование:**
- Получение списка пользователей
- Отображение участников чата
- Фильтрация сообщений по автору

### UserInfoDto
```csharp
/// <summary>
/// Детальная информация о пользователе
/// </summary>
public sealed class UserInfoDto
{
    /// <summary>
    /// Имя пользователя
    /// </summary>
    public String Username { get; set; } = String.Empty;
    
    /// <summary>
    /// Дата регистрации
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// Время последнего входа
    /// </summary>
    public DateTime? LastLogin { get; set; }
    
    /// <summary>
    /// Количество отправленных сообщений
    /// </summary>
    public Int32 MessageCount { get; set; }
}
```

**Использование:**
- Детальная информация о пользователе
- Статистика активности
- Профиль пользователя

---

## 📤 Requests (Модели запросов)

### RegisterRequest
```csharp
/// <summary>
/// Запрос на регистрацию нового пользователя
/// </summary>
public sealed class RegisterRequest
{
    /// <summary>
    /// Имя пользователя (3-100 символов, буквы, цифры, _, -)
    /// </summary>
    public String Username { get; set; } = String.Empty;
    
    /// <summary>
    /// Пароль (минимум 6 символов)
    /// </summary>
    public String Password { get; set; } = String.Empty;
}
```

**Валидация (на сервере):**
- Username: 3-100 символов, только буквы, цифры, `_`, `-`
- Password: минимум 6 символов
- Username должен быть уникальным

**Endpoint:**
```
POST /api/auth/register
Content-Type: application/json

{
  "username": "testuser",
  "password": "Test123!"
}
```

### LoginRequest
```csharp
/// <summary>
/// Запрос на вход в систему
/// </summary>
public sealed class LoginRequest
{
    /// <summary>
    /// Имя пользователя
    /// </summary>
    public String Username { get; set; } = String.Empty;
    
    /// <summary>
    /// Пароль
    /// </summary>
    public String Password { get; set; } = String.Empty;
}
```

**Валидация (на сервере):**
- Username: обязательное поле
- Password: обязательное поле
- Проверка существования пользователя
- Проверка правильности пароля

**Endpoint:**
```
POST /api/auth/login
Content-Type: application/json

{
  "username": "testuser",
  "password": "Test123!"
}
```

### SendMessageAuthRequest
```csharp
/// <summary>
/// Запрос на отправку сообщения от авторизованного пользователя
/// </summary>
public sealed class SendMessageAuthRequest
{
    /// <summary>
    /// Содержимое сообщения (1-5000 символов)
    /// </summary>
    public String Content { get; set; } = String.Empty;
}
```

**Валидация (на сервере):**
- Content: 1-5000 символов
- Требуется JWT токен в заголовке Authorization

**Endpoint:**
```
POST /api/chat/messages
Authorization: Bearer TOKEN
Content-Type: application/json

{
  "content": "Hello World!"
}
```

**Примечание:** UserId извлекается из JWT токена, не передаётся в теле запроса.

---

## 📥 Responses (Модели ответов)

### AuthResponse
```csharp
/// <summary>
/// Ответ на успешную аутентификацию
/// </summary>
public sealed class AuthResponse
{
    /// <summary>
    /// JWT токен для последующих запросов
    /// </summary>
    public String Token { get; set; } = String.Empty;
}
```

**Использование:**
- Возвращается при регистрации
- Возвращается при входе
- Клиент сохраняет токен для дальнейших запросов

**Пример ответа:**
```json
{
  "token": "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

### GetMessagesResponse
```csharp
/// <summary>
/// Ответ на запрос получения сообщений
/// </summary>
public sealed class GetMessagesResponse
{
    /// <summary>
    /// Список сообщений
    /// </summary>
    public List<ChatMessageDto> Messages { get; set; } = new();
    
    /// <summary>
    /// Общее количество сообщений (для пагинации)
    /// </summary>
    public Int32 TotalCount { get; set; }
}
```

**Использование:**
- Получение всех сообщений
- Получение сообщений после определённой даты
- Получение сообщений конкретного пользователя

**Пример ответа:**
```json
{
  "messages": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "senderName": "testuser",
      "content": "Hello World!",
      "timestamp": "2026-07-17T10:30:00Z"
    }
  ],
  "totalCount": 1
}
```

---

## 🔄 Маппинг между доменом и DTO

### Server → DTO (Mapping)

**ChatMessage → ChatMessageDto:**
```csharp
// В Use Case или Repository
var dto = new ChatMessageDto
{
    Id = message.Id,
    SenderName = user.Username,  // Получаем из навигации User
    Content = message.Content,
    Timestamp = message.Timestamp
};
```

**User → UserDto:**
```csharp
var dto = new UserDto
{
    Id = user.Id,
    Username = user.Username
};
```

**User → UserInfoDto:**
```csharp
var dto = new UserInfoDto
{
    Username = user.Username,
    CreatedAt = user.CreatedAt,
    LastLogin = user.LastLogin,
    MessageCount = user.Messages.Count  // Из Aggregate Root
};
```

### DTO → Domain (Mapping)

**RegisterRequest → User:**
```csharp
// В RegisterUseCase
var user = User.Create(
    request.Username,
    passwordHasher.HashPassword(request.Password)
);
```

**SendMessageAuthRequest → ChatMessage:**
```csharp
// В SendMessageUseCase
user.AddMessage(request.Content);  // Через Aggregate Root
```

---

## 📋 API Контракты

### Общая структура

**Успешный ответ:**
```json
{
  "data": { ... },
  "status": 200
}
```

**Ошибка валидации (400):**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Username": ["Username должен быть от 3 до 100 символов"],
    "Password": ["Пароль должен быть минимум 6 символов"]
  }
}
```

**Ошибка аутентификации (401):**
```json
{
  "error": "Неверное имя пользователя или пароль"
}
```

**Ошибка Not Found (404):**
```json
{
  "error": "Пользователь не найден"
}
```

---

## 🎯 Best Practices

### 1. Sealed классы
```csharp
public sealed class ChatMessageDto  // Предотвращает наследование
```

**Преимущества:**
- Оптимизация компилятора
- Понятная иерархия (нет наследования)
- Защита от неправильного использования

### 2. Иммутабельность
```csharp
// DTO с init-only свойствами (опционально)
public sealed class ChatMessageDto
{
    public Guid Id { get; init; }
    public String SenderName { get; init; } = String.Empty;
    public String Content { get; init; } = String.Empty;
    public DateTime Timestamp { get; init; }
}
```

### 3. Значения по умолчанию
```csharp
public String Username { get; set; } = String.Empty;  // Избегаем null
```

### 4. XML комментарии
```csharp
/// <summary>
/// Детальное описание для документации
/// </summary>
public String Property { get; set; }
```

**Генерация документации:**
- Swagger автоматически использует XML комментарии
- IntelliSense показывает описания
- Улучшает читаемость кода

---

## 🔐 Безопасность

### Что НЕ передаётся в DTO:

❌ **PasswordHash** - никогда не отправляется клиенту
❌ **Внутренние ID связей** - только публичные идентификаторы
❌ **Служебная информация** - версии, метаданные EF Core

### Что передаётся:

✅ **Публичные идентификаторы** (Guid)
✅ **Публичные данные пользователя** (Username, но не Email/Phone)
✅ **Контент сообщений**
✅ **Временные метки**

---

## 📊 Версионирование

### Стратегия версионирования:

**Namespace versioning:**
```csharp
namespace ChatApp.Contracts.V1.Messages;
namespace ChatApp.Contracts.V2.Messages;
```

**URL versioning:**
```
/api/v1/chat/messages
/api/v2/chat/messages
```

**Header versioning:**
```
Accept: application/vnd.chatapp.v1+json
```

**Текущая версия:** V1 (неявная, по умолчанию)

---

## 🧪 Использование

### На сервере:

**В контроллере:**
```csharp
[HttpPost("register")]
public async Task<ActionResult<AuthResponse>> Register(
    [FromBody] RegisterRequest request)
{
    var token = await _registerUseCase.ExecuteAsync(request);
    return Ok(new AuthResponse { Token = token });
}
```

**В Use Case:**
```csharp
public async Task<AuthResponse> ExecuteAsync(RegisterRequest request)
{
    // Логика регистрации
    var token = _tokenGenerator.GenerateToken(user.Id, user.Username);
    return new AuthResponse { Token = token };
}
```

### На клиенте:

**Отправка запроса:**
```csharp
var request = new RegisterRequest
{
    Username = "testuser",
    Password = "Test123!"
};
var response = await _httpClient.PostAsJsonAsync("/api/auth/register", request);
var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
```

**Получение данных:**
```csharp
var response = await _httpClient.GetAsync("/api/chat/messages");
var result = await response.Content.ReadFromJsonAsync<GetMessagesResponse>();
var messages = result?.Messages ?? new List<ChatMessageDto>();
```

---

## 📈 Производительность

### Оптимизации:

1. **Sealed классы** - оптимизация виртуальных вызовов
2. **Struct для простых значений** (опционально)
3. **Минимальный размер** - только необходимые поля
4. **Избегание вложенных объектов** - плоская структура где возможно

### Размер DTO:

**ChatMessageDto:**
- Guid (16 байт) + String (переменная) + String (переменная) + DateTime (8 байт)
- Средний размер: ~100-500 байт в зависимости от контента

**Сериализация:**
- JSON (по умолчанию)
- Компактный формат
- Сжатие на уровне HTTP (gzip)

---

## 🔄 Миграция контрактов

### При изменении контрактов:

1. **Обратная совместимость:**
   - Добавление новых полей с default значениями
   - Сохранение существующих полей
   - Маркировка устаревших как `[Obsolete]`

2. **Версионирование:**
   - Создание V2 контракта
   - Поддержка обеих версий
   - Постепенный переход клиентов

3. **Тестирование:**
   - Unit тесты для сериализации/десериализации
   - Проверка обратной совместимости
   - Тесты миграции данных

---

## 🛠️ Зависимости

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
</Project>
```

**Никаких внешних зависимостей!**

Contracts - это чистый .NET код без зависимостей от фреймворков.

---

## 📚 Документация API

Все контракты автоматически документируются в Swagger UI:

**URL:** http://localhost:5096

**Features:**
- Схемы всех DTO
- Примеры запросов и ответов
- Валидация моделей
- Try it out функциональность

---

[← Назад к главной](../README.md) | [← Клиент](./CLIENT.md) | [← Сервер](./SERVER.md)
