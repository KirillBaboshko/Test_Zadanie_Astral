# Cross-Cutting Concerns через декораторы

## Описание

Реализован паттерн декораторов для применения сквозных функций (Cross-Cutting Concerns) к Use Cases. Это позволяет:
- Автоматически логировать выполнение Use Cases
- Автоматически управлять транзакциями (Unit of Work)
- Держать бизнес-логику чистой от инфраструктурных деталей

## Архитектура

```
Request 
  ↓
LoggingDecorator (логирование начала, времени выполнения, ошибок)
  ↓
UnitOfWorkDecorator (управление транзакциями - SaveChanges)
  ↓
UseCase Core Logic (чистая бизнес-логика)
  ↓
Response
```

## Компоненты

### 1. Базовый интерфейс
**Файл:** `src/Server/ChatApp.Server.Application/Common/IUseCase.cs`

```csharp
public interface IUseCase<TRequest, TResponse>
{
    Task<TResponse> ExecuteAsync(TRequest request, CancellationToken cancellationToken = default);
}
```

### 2. Интерфейс декоратора
**Файл:** `src/Server/ChatApp.Server.Application/Common/CrossCutting/IUseCaseDecorator.cs`

```csharp
public interface IUseCaseDecorator<TRequest, TResponse>
{
    Task<TResponse> DecorateAsync(
        TRequest request,
        Func<TRequest, CancellationToken, Task<TResponse>> next,
        CancellationToken cancellationToken);
}
```

### 3. Базовый класс с поддержкой декораторов
**Файл:** `src/Server/ChatApp.Server.Application/Common/DecoratedUseCase.cs`

Строит цепочку декораторов и вызывает `ExecuteCoreAsync` для бизнес-логики.

### 4. Декораторы

#### LoggingDecorator
**Файл:** `src/Server/ChatApp.Server.Application/Common/CrossCutting/LoggingDecorator.cs`

- Логирует начало выполнения Use Case
- Логирует время выполнения
- Логирует ошибки при возникновении

#### UnitOfWorkDecorator
**Файл:** `src/Server/ChatApp.Server.Application/Common/CrossCutting/UnitOfWorkDecorator.cs`

- Автоматически вызывает `SaveChangesAsync` после успешного выполнения Use Case
- Гарантирует атомарность операций (например, сообщение + событие в Outbox в одной транзакции)

## Реализованные Use Cases

### SendMessageUseCase

**Файлы:**
- `src/Server/ChatApp.Server.Application/UseCases/SendMessage/SendMessageUseCase.cs`
- `src/Server/ChatApp.Server.Application/UseCases/SendMessage/SendMessageUseCaseRequest.cs`
- `src/Server/ChatApp.Server.Application/UseCases/SendMessage/SendMessageUseCaseResponse.cs`

**Используется во ВСЕХ протоколах:**
1. **REST API** - `ChatController.SendMessage`
2. **gRPC** - `CodeFirstChatService.SendMessage`
3. **Message Bus (RabbitMQ)** - `SendMessageCommandConsumer`

## Регистрация в DI

**Файл:** `src/Server/ChatApp.Server.Api/Program.cs`

```csharp
// Регистрация декораторов
builder.Services.AddScoped(typeof(LoggingDecorator<,>));
builder.Services.AddScoped(typeof(UnitOfWorkDecorator<,>));

// Регистрация Use Case с декораторами
builder.Services.AddScoped<IUseCase<SendMessageUseCaseRequest, SendMessageUseCaseResponse>>(sp =>
{
    var decorators = new List<IUseCaseDecorator<SendMessageUseCaseRequest, SendMessageUseCaseResponse>>
    {
        sp.GetRequiredService<LoggingDecorator<SendMessageUseCaseRequest, SendMessageUseCaseResponse>>(),
        sp.GetRequiredService<UnitOfWorkDecorator<SendMessageUseCaseRequest, SendMessageUseCaseResponse>>()
    };

    return new SendMessageUseCase(
        sp.GetRequiredService<IUserRepository>(),
        sp.GetRequiredService<IOutboxService>(),
        decorators);
});
```

**Важно:** Порядок декораторов имеет значение!
1. Сначала `LoggingDecorator` - для логирования
2. Затем `UnitOfWorkDecorator` - для управления транзакциями
3. Затем Core Logic - бизнес-логика

## Использование в клиентах

### REST API
```csharp
public async Task<ActionResult<ChatMessageDto>> SendMessage(
    [FromBody] SendMessageAuthRequest request,
    CancellationToken cancellationToken)
{
    var useCaseRequest = new SendMessageUseCaseRequest
    {
        UserId = userId,
        Content = request.Content
    };

    var response = await _sendMessageUseCase.ExecuteAsync(useCaseRequest, cancellationToken);
    
    if (!response.Success)
        return NotFound(new { error = "Пользователь не найден" });
    
    return CreatedAtAction(...);
}
```

### gRPC
```csharp
public async Task<MessageResponse> SendMessage(SendMessageRequest request, CallContext context)
{
    var useCaseRequest = new SendMessageUseCaseRequest
    {
        UserId = userId.Value,
        Content = request.Content
    };

    var response = await _sendMessageUseCase.ExecuteAsync(useCaseRequest, context.CancellationToken);
    
    if (!response.Success)
        throw new InvalidOperationException("User not found");
    
    return new MessageResponse { ... };
}
```

### Message Bus (RabbitMQ)
```csharp
public async Task Consume(ConsumeContext<SendMessageCommand> context)
{
    var useCaseRequest = new SendMessageUseCaseRequest
    {
        UserId = context.Message.SenderId,
        Content = context.Message.Content
    };

    var response = await _sendMessageUseCase.ExecuteAsync(useCaseRequest, context.CancellationToken);
    
    // Публикация события через Outbox происходит автоматически внутри Use Case
}
```

## Преимущества

1. **Единая реализация** - Use Case используется одинаково во всех протоколах (REST, gRPC, Message Bus)
2. **Чистая бизнес-логика** - Core Logic не содержит инфраструктурного кода (логирование, транзакции)
3. **Расширяемость** - легко добавить новые декораторы (валидация, кеширование, retry и т.д.)
4. **Тестируемость** - бизнес-логику можно тестировать отдельно от декораторов
5. **DRY принцип** - сквозные функции реализованы один раз, применяются ко всем Use Cases

## Гарантии с Outbox Pattern

Благодаря `UnitOfWorkDecorator` и `OutboxService`:
- Сообщение в БД и событие в Outbox сохраняются в **одной транзакции**
- Гарантируется **атомарность**: либо оба сохраняются, либо оба откатываются
- События публикуются **надежно** через `OutboxPublisherService` (каждые 5 сек)
- Поддерживается **retry** до 5 попыток при ошибках публикации

## Следующие шаги

Для добавления нового Use Case с декораторами:

1. Создать `Request` и `Response` классы
2. Реализовать `UseCase` унаследовав от `DecoratedUseCase<TRequest, TResponse>`
3. Переопределить метод `ExecuteCoreAsync` с бизнес-логикой
4. Зарегистрировать в DI с нужными декораторами

Пример:
```csharp
public class MyNewUseCase : DecoratedUseCase<MyRequest, MyResponse>
{
    public MyNewUseCase(
        // dependencies,
        IEnumerable<IUseCaseDecorator<MyRequest, MyResponse>> decorators)
        : base(decorators)
    {
        // ...
    }

    protected override async Task<MyResponse> ExecuteCoreAsync(
        MyRequest request,
        CancellationToken cancellationToken)
    {
        // Чистая бизнес-логика здесь
        // Логирование и транзакции применяются автоматически через декораторы
    }
}
```
