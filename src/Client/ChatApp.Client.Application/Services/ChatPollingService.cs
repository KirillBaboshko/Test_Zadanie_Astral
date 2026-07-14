namespace ChatApp.Client.Application.Services;

public sealed class ChatPollingService
{
    private readonly IChatApiClient _apiClient;
    private DateTime _lastMessageTime = DateTime.UtcNow;
   

    public ChatPollingService(IChatApiClient apiClient)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
    }

    public event EventHandler<MessageReceivedEventArgs>? MessageReceived;

    public async Task StartPollingAsync(String currentUserName, CancellationToken cancellationToken)
    {
        const Int32 PollingInterval = 1000;// 1 секунда
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(PollingInterval, cancellationToken);

                var response = await _apiClient.GetMessagesAsync(_lastMessageTime, 50, cancellationToken);

                if (response?.Messages != null && response.Messages.Count > 0)
                {
                    foreach (var message in response.Messages)
                    {
                        // Не показываем свои собственные сообщения
                        if (!message.SenderName.Equals(currentUserName, StringComparison.OrdinalIgnoreCase))
                        {
                            MessageReceived?.Invoke(this, new MessageReceivedEventArgs(message));
                        }

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

    public void UpdateLastMessageTime(DateTime timestamp)
    {
        if (timestamp > _lastMessageTime)
            _lastMessageTime = timestamp;
    }
}

public sealed class MessageReceivedEventArgs : EventArgs
{
    public ChatApp.Contracts.Messages.ChatMessageDto Message { get; }

    public MessageReceivedEventArgs(ChatApp.Contracts.Messages.ChatMessageDto message)
    {
        Message = message;
    }
}
