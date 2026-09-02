# 💻 Client - Клиентские приложения

Клиентская часть ChatApp включает **Blazor WebAssembly** (веб-UI) и **консольное приложение** с выбором протокола (HTTP / gRPC / RabbitMQ).

## 📦 Структура

```
Client/
├── ChatApp.Client.Blazor/          # Blazor WebAssembly (веб-клиент)
├── ChatApp.Client.Console/         # Консольное приложение
├── ChatApp.Client.Application/     # Интерфейсы и абстракции
└── ChatApp.Client.Infrastructure/  # HTTP, gRPC, Message Bus реализации
```

---

## 🌐 ChatApp.Client.Blazor

**Назначение:** веб-интерфейс чата (Blazor WebAssembly). Запускается **локально**, бэкенд — в Docker или локально.

### Запуск

```bash
cd src/Client/ChatApp.Client.Blazor
dotnet run --launch-profile http
```

- UI: http://localhost:5073
- API: настраивается в `wwwroot/appsettings.json`

```json
{
  "ApiBaseUrl": "http://localhost:5096"
}
```

### Типичный сценарий (Docker backend + Blazor local)

```bash
# Терминал 1
docker compose up -d

# Терминал 2
cd src/Client/ChatApp.Client.Blazor
dotnet run --launch-profile http
```

> Используйте **http** профиль (`5073`), не https — иначе браузер блокирует запросы к `http://localhost:5096`.

### Архитектура

```
Browser (Blazor WASM)
    │
    ├── AuthService / ChatService (HttpClient)
    │
    └── HTTP REST → localhost:5096/api/...
```

### Компоненты

| Папка | Назначение |
|-------|------------|
| `Pages/` | `Auth.razor`, `Chat.razor` |
| `Services/` | `AuthService`, `ChatService` |
| `ViewModels/` | `AuthViewModel`, `ChatViewModel` |

### Docker

Сервис `blazor-client` в `docker-compose.yml` **закомментирован**. Blazor запускается через `dotnet run`, не через Docker.

---

## 🎯 ChatApp.Client.Console

**Назначение:** Presentation слой - пользовательский интерфейс, консольное взаимодействие.

### Основные возможности:

#### Меню аутентификации
```
╔════════════════════════════════╗
║   МЕНЮ АУТЕНТИФИКАЦИИ          ║
╠════════════════════════════════╣
║ 1. Регистрация                 ║
║ 2. Вход                        ║
║ 0. Выход                       ║
╚════════════════════════════════╝
```

#### Чат интерфейс
```
╔════════════════════════════════╗
║       ЧАТ ПРИЛОЖЕНИЕ           ║
╠════════════════════════════════╣
║ Команды:                       ║
║ /help     - помощь             ║
║ /messages - все сообщения      ║
║ /user     - сообщения юзера    ║
║ /exit     - выход              ║
╚════════════════════════════════╝
```

### Program.cs

**Выбор протокола при старте:**

```
1. HTTP (REST API)      → http://localhost:5096
2. gRPC (Code-first)    → http://localhost:5097
3. Message Bus (RabbitMQ) → localhost:5672
```

**Основной поток (HTTP/gRPC):**

1. **Подключение к серверу**
   - Запрос URL сервера (по умолчанию: http://localhost:5096)
   - Проверка доступности API

2. **Аутентификация**
   - Регистрация нового пользователя
   - Или вход существующего
   - Получение и сохранение JWT токена

3. **Чат режим**
   - Запуск фонового опроса сообщений
   - Отображение последних 10 сообщений
   - Обработка команд и отправка сообщений

4. **Завершение**
   - Остановка фонового сервиса
   - Корректное завершение соединения

### Особенности UI:

- **Цветной вывод:**
  - Cyan - заголовки и меню
  - Green - успешные операции
  - Red - ошибки
  - Yellow - предупреждения
  - White - обычный текст

- **Скрытый ввод пароля:**
```csharp
String password = "";
while (true)
{
    var key = Console.ReadKey(intercept: true);
    if (key.Key == ConsoleKey.Enter) break;
    if (key.Key == ConsoleKey.Backspace && password.Length > 0)
    {
        password = password[0..^1];
        Console.Write("\b \b");
    }
    else if (!Char.IsControl(key.KeyChar))
    {
        password += key.KeyChar;
        Console.Write("*");
    }
}
```

- **Форматирование времени:**
  - Локальное время для отображения
  - Формат: `[HH:mm:ss] username: message`

### Зависимости:
```xml
<PackageReference Include="Microsoft.Extensions.DependencyInjection" />
<PackageReference Include="Microsoft.Extensions.Hosting" />
```

---

## 💼 ChatApp.Client.Application

**Назначение:** Application слой - бизнес-логика клиента, сервисы.

### Services:

#### IChatApiClient
Интерфейс для взаимодействия с API:

**Методы аутентификации:**
```csharp
Task<String?> RegisterAsync(String username, String password);
Task<String?> LoginAsync(String username, String password);
```

**Методы работы с сообщениями:**
```csharp
Task<List<ChatMessageDto>> GetMessagesAsync(DateTime? since = null, Int32 limit = 100);
Task<List<ChatMessageDto>> GetMessagesByUserNameAsync(String username, Int32 limit = 100);
Task<ChatMessageDto?> SendMessageAsync(String token, String content);
```

**Методы работы с пользователями:**
```csharp
Task<List<UserDto>> GetUsersAsync();
Task<UserInfoDto?> GetUserInfoAsync(String username);
```

#### ChatPollingService
Фоновый сервис для опроса новых сообщений:

**Особенности:**
```csharp
public class ChatPollingService
{
    private Timer? _timer;
    private DateTime _lastCheckTime;
    private readonly HashSet<Guid> _seenMessageIds = new();
    
    public void Start(String token, Action<ChatMessageDto> onNewMessage)
    {
        // Запуск таймера с интервалом 2 секунды
        // Опрос API на наличие новых сообщений
        // Дедупликация через HashSet
    }
    
    public void Stop()
    {
        // Остановка таймера
        // Освобождение ресурсов
    }
}
```

**Механизм работы:**
1. Каждые 2 секунды опрашивает API
2. Получает сообщения после `_lastCheckTime`
3. Фильтрует уже показанные через `_seenMessageIds`
4. Вызывает callback для новых сообщений
5. Обновляет `_lastCheckTime`

**Преимущества:**
- ✅ Дедупликация сообщений (не показывает дважды)
- ✅ Эффективный опрос (только новые сообщения)
- ✅ Асинхронная обработка (не блокирует UI)
- ✅ Автоматическое управление ресурсами

### Зависимости:
```xml
<PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" />
```

---

## 🔧 ChatApp.Client.Infrastructure

**Назначение:** Infrastructure слой - реализация HTTP клиента, внешние зависимости.

### HTTP:

### Реализации IChatApiClient

| Класс | Протокол | Порт |
|-------|----------|------|
| `HttpChatApiClient` | REST JSON | 5096 |
| `CodeFirstGrpcChatApiClient` | gRPC code-first | 5097 |
| `MessageBusApiClient` | RabbitMQ | 5672 |

#### HttpChatApiClient : IChatApiClient
Реализация HTTP клиента для взаимодействия с API:

**Конфигурация:**
```csharp
public class HttpChatApiClient : IChatApiClient
{
    private readonly HttpClient _httpClient;
    private readonly String _baseUrl;
    
    public HttpChatApiClient(HttpClient httpClient, String baseUrl)
    {
        _httpClient = httpClient;
        _baseUrl = baseUrl.TrimEnd('/');
    }
}
```

**Методы:**

**RegisterAsync:**
```csharp
var request = new { username, password };
var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/api/auth/register", request);
if (response.IsSuccessStatusCode)
{
    var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
    return result?.Token;
}
```

**SendMessageAsync:**
```csharp
var request = new { content };
_httpClient.DefaultRequestHeaders.Authorization = 
    new AuthenticationHeaderValue("Bearer", token);
var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/api/chat/messages", request);
```

**GetMessagesAsync:**
```csharp
var query = new StringBuilder($"{_baseUrl}/api/chat/messages?limit={limit}");
if (since.HasValue)
{
    query.Append($"&since={since.Value:O}");
}
var response = await _httpClient.GetAsync(query.ToString());
var result = await response.Content.ReadFromJsonAsync<GetMessagesResponse>();
return result?.Messages ?? new List<ChatMessageDto>();
```

**Обработка ошибок:**
```csharp
catch (HttpRequestException ex)
{
    Console.WriteLine($"Ошибка HTTP: {ex.Message}");
    return null;
}
catch (Exception ex)
{
    Console.WriteLine($"Ошибка: {ex.Message}");
    return null;
}
```

### Зависимости:
```xml
<PackageReference Include="System.Net.Http.Json" />
<PackageReference Include="Grpc.Net.Client" />
<PackageReference Include="protobuf-net.Grpc" />
<PackageReference Include="MassTransit.RabbitMQ" />
```

---

## 🎮 Пользовательский сценарий

### 1. Запуск приложения

```
=== HTTP Chat Client с JWT аутентификацией ===
URL сервера (по умолчанию http://localhost:5096):
> [Enter для default]

Подключение к серверу http://localhost:5096...
```

### 2. Аутентификация

**Регистрация:**
```
Выберите действие: 1
Введите имя пользователя (3-100 символов): testuser
Введите пароль (минимум 6 символов): ******

⏳ Регистрация...
✅ Регистрация успешна! Добро пожаловать, testuser!
🔑 Токен действителен до: 24.07.2026 13:56:38
```

**Вход:**
```
Выберите действие: 2
Введите имя пользователя: testuser
Введите пароль: ******

⏳ Авторизация...
✅ Вход выполнен! Добро пожаловать, testuser!
🔑 Токен действителен до: 24.07.2026 13:58:15
```

### 3. Чат режим

**Отображение истории:**
```
========== ПОСЛЕДНИЕ СООБЩЕНИЯ ==========
[13:45:23] user1: Привет!
[13:46:15] user2: Как дела?
[13:47:02] user1: Отлично!
==========================================
```

**Отправка сообщений:**
```
> Привет всем!
✅ Сообщение отправлено

> Как погода?
✅ Сообщение отправлено
```

**Real-time получение:**
```
[13:50:33] other_user: Привет, testuser!
```

**Команды:**
```
> /help
Доступные команды:
  /help     - показать эту справку
  /messages - показать все сообщения
  /user     - показать сообщения конкретного пользователя
  /exit     - выход из приложения

> /messages
[показывает все сообщения]

> /user
Введите имя пользователя: user1
[показывает сообщения user1]

> /exit
Завершение работы...
До свидания!
```

---

## 🔐 Безопасность

### JWT токены
- Токены сохраняются в памяти приложения
- Автоматическая передача в заголовке `Authorization: Bearer TOKEN`
- Время жизни: 7 дней
- При истечении токена требуется повторный вход

### Защита паролей
- Пароли не сохраняются на клиенте
- Скрытый ввод (замена символов на `*`)
- Передача только по HTTPS (в продакшене)

---

## 🚀 Запуск

### Локально:

```bash
cd src/Client/ChatApp.Client.Console
dotnet run
```

### Через Docker:

```bash
# Собрать образ
docker-compose build client

# Запустить интерактивно
docker-compose run --rm client
```

**Примечание:** Клиент в профиле `client` и не запускается по умолчанию.

---

## 📊 Архитектура взаимодействия

```
┌─────────────────────┐
│  Console UI         │
│  (Program.cs)       │
└──────────┬──────────┘
           │
           ▼
┌─────────────────────┐
│ ChatPollingService  │  ◄─── Фоновый опрос (каждые 2 сек)
└──────────┬──────────┘
           │
           ▼
┌─────────────────────┐
│  IChatApiClient     │
│  (Interface)        │
└──────────┬──────────┘
           │
           ▼
┌─────────────────────┐
│ HttpChatApiClient   │  ◄─── HTTP запросы к API
└──────────┬──────────┘
           │
           ▼
┌─────────────────────┐
│  Server API         │
│  (HTTP REST)        │
└─────────────────────┘
```

---

## 🧪 Тестирование

### Ручное тестирование:

1. Запустить сервер
2. Запустить клиент
3. Зарегистрироваться
4. Отправить сообщение
5. Открыть второй клиент
6. Войти под другим пользователем
7. Проверить получение сообщений в реальном времени

### Сценарии:

**Сценарий 1: Регистрация и отправка**
- Регистрация нового пользователя
- Отправка нескольких сообщений
- Проверка отображения в истории

**Сценарий 2: Множественные клиенты**
- Запуск 2-3 клиентов
- Вход под разными пользователями
- Обмен сообщениями
- Проверка real-time обновлений

**Сценарий 3: Команды**
- Использование `/help`
- Просмотр всех сообщений `/messages`
- Фильтрация по пользователю `/user`
- Выход `/exit`

---

## 📈 Производительность

### ChatPollingService
- Интервал опроса: 2 секунды
- Получение только новых сообщений (since параметр)
- Дедупликация через HashSet (O(1) проверка)
- Асинхронная обработка (не блокирует UI)

### HttpClient
- Переиспользование одного HttpClient
- Connection pooling
- Автоматическое управление соединениями

### Оптимизации
- Ограничение истории (последние 10 сообщений)
- Limit параметр для пагинации
- Минимальный трафик (только новые данные)

---

## 🔄 Жизненный цикл

```
[Запуск]
   │
   ├─► [Подключение к серверу]
   │
   ├─► [Аутентификация]
   │      ├─► Регистрация
   │      └─► Вход
   │
   ├─► [Инициализация чата]
   │      ├─► Получение последних 10 сообщений
   │      └─► Запуск ChatPollingService
   │
   ├─► [Чат режим] ◄──┐
   │      ├─► Отправка сообщений
   │      ├─► Получение новых (фон)
   │      ├─► Команды
   │      └─► [продолжение] ──┘
   │
   └─► [Завершение]
          ├─► Остановка ChatPollingService
          └─► Выход
```

---

## 🛠️ Расширения

### Возможные улучшения:

1. **SignalR для real-time:**
   - Замена polling на WebSocket
   - Мгновенная доставка сообщений
   - Уведомления о статусе пользователей

2. **Локальное кеширование:**
   - SQLite для оффлайн доступа
   - Синхронизация при подключении

3. **GUI версия:**
   - WPF / Avalonia UI
   - Кроссплатформенность

4. **Уведомления:**
   - Звуковые сигналы
   - Desktop notifications
   - Badges для новых сообщений

5. **Группы и каналы:**
   - Создание приватных чатов
   - Групповые беседы
   - Управление доступом

---

[← Назад к главной](../README.md) | [← Сервер](./SERVER.md) | [Shared →](./SHARED.md)
