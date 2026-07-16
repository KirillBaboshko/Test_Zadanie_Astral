namespace ChatApp.Client.Application.Services;


public sealed class ChatPollingService
{
    private readonly IChatApiClient _apiClient;
    private DateTime _lastMessageTime = DateTime.UtcNow;
    private readonly HashSet<Guid> _processedMessageIds = new();
    const Int32 Limit = 50;

    public ChatPollingService(IChatApiClient apiClient)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
    }

    /// <summary>
    /// Событие получения нового сообщения
    /// </summary>
    public event EventHandler<MessageReceivedEventArgs>? MessageReceived;

    /// <summary>
    /// Запускает циклический опрос сервера для получения новых сообщений
    /// </summary>
    public async Task StartPollingAsync(String currentUserName, CancellationToken cancellationToken)
    {
        const Int32 PollingInterval = 1000;// 1 секунда
        const Int32 InitialHistoryLimit = 10; // Показываем последние 10 сообщений при старте
        
        // При старте показываем несколько последних сообщений для контекста
        try
        {
            var initialResponse = await _apiClient.GetMessagesAsync(limit: InitialHistoryLimit, cancellationToken: cancellationToken);
            if (initialResponse?.Messages != null && initialResponse.Messages.Count > 0)
            {
                // Показываем историю сообщений (кроме своих)
                foreach (var message in initialResponse.Messages)
                {
                    if (!message.SenderName.Equals(currentUserName, StringComparison.OrdinalIgnoreCase))
                    {
                        MessageReceived?.Invoke(this, new MessageReceivedEventArgs(message));
                    }
                    _processedMessageIds.Add(message.Id);
                }
                
                // Устанавливаем время последнего сообщения
                _lastMessageTime = initialResponse.Messages.Max(m => m.Timestamp);
            }
        }
        catch
        {
            // Если не удалось получить историю, используем текущее время
            _lastMessageTime = DateTime.UtcNow;
        }
        
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(PollingInterval, cancellationToken);

                var response = await _apiClient.GetMessagesAsync(_lastMessageTime, Limit, cancellationToken);

                if (response?.Messages != null && response.Messages.Count > 0)
                {
                    foreach (var message in response.Messages)
                    {
                        // Пропускаем уже обработанные сообщения
                        if (_processedMessageIds.Contains(message.Id))
                            continue;
                        
                        // Не показываем свои собственные сообщения
                        if (!message.SenderName.Equals(currentUserName, StringComparison.OrdinalIgnoreCase))
                        {
                            MessageReceived?.Invoke(this, new MessageReceivedEventArgs(message));
                        }

                        _processedMessageIds.Add(message.Id);
                        
                        if (message.Timestamp > _lastMessageTime)
                            _lastMessageTime = message.Timestamp;
                    }
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception)
            {
                await Task.Delay(5000, cancellationToken);
            }
        }
    }

    /// <summary>
    /// Обновляет время последнего полученного сообщения
    /// </summary>
    public void UpdateLastMessageTime(DateTime timestamp)
    {
        if (timestamp > _lastMessageTime)
            _lastMessageTime = timestamp;
    }

    /// <summary>
    /// Получает все сообщения указанного пользователя
    /// </summary>
    public async Task GetMessagesByUserName(String targetUserName, CancellationToken cancellationToken)
    { 
        var response = await _apiClient.GetMessagesForNameAsync(Limit, targetUserName, cancellationToken);

        if (response?.Messages != null && response.Messages.Count > 0)
        {
            foreach (var message in response.Messages)
            {
                MessageReceived?.Invoke(this, new MessageReceivedEventArgs(message));
            }
        }
    }
}

/// <summary>
/// Аргументы события получения сообщения
/// </summary>
public sealed class MessageReceivedEventArgs : EventArgs
{
    public ChatApp.Contracts.Messages.ChatMessageDto Message { get; }

    public MessageReceivedEventArgs(ChatApp.Contracts.Messages.ChatMessageDto message)
    {
        Message = message;
    }
}
