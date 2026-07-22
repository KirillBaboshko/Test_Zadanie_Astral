using ChatApp.Client.Blazor.Services;
using ChatApp.Contracts.Messages;
using Microsoft.AspNetCore.Components;

namespace ChatApp.Client.Blazor.ViewModels;

/// <summary>
/// ViewModel для страницы чата
/// </summary>
public class ChatViewModel : IDisposable
{
    private readonly AuthService _authService;
    private readonly ChatService _chatService;
    private readonly NavigationManager _navigationManager;
    private System.Threading.Timer? _pollingTimer;

    public ChatViewModel(
        AuthService authService, 
        ChatService chatService, 
        NavigationManager navigationManager)
    {
        _authService = authService;
        _chatService = chatService;
        _navigationManager = navigationManager;
    }

    public List<ChatMessageDto> Messages { get; private set; } = new();
    public string MessageText { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public bool IsSending { get; private set; }
    public string Username => _authService.Username ?? "User";
    
    private DateTime? _lastMessageTime;

    public event Action? OnStateChanged;

    /// <summary>
    /// Инициализация - проверка авторизации и загрузка сообщений
    /// </summary>
    public async Task InitializeAsync()
    {
        await _authService.InitializeAsync();

        if (!_authService.IsAuthenticated)
        {
            _navigationManager.NavigateTo("/");
            return;
        }

        await LoadMessagesAsync();

        const Int32 pollingTime =2;
        _pollingTimer = new System.Threading.Timer(async _ =>
        {
            await LoadMessagesAsync();
            NotifyStateChanged();
        }, null, TimeSpan.FromSeconds(pollingTime), TimeSpan.FromSeconds(pollingTime));
    }

    /// <summary>
    /// Загрузка новых сообщений
    /// </summary>
    private async Task LoadMessagesAsync()
    {
        try
        {
            var newMessages = await _chatService.GetMessagesAsync(_lastMessageTime, 100);
            
            if (newMessages.Any())
            { 
                var existingIds = Messages.Select(m => m.Id).ToHashSet();
                
                var uniqueNewMessages = newMessages.Where(m => !existingIds.Contains(m.Id)).ToList();
                
                if (uniqueNewMessages.Any())
                {
                    Messages.AddRange(uniqueNewMessages);
                    _lastMessageTime = Messages.Max(m => m.Timestamp);
                    
                    Messages = Messages.OrderBy(m => m.Timestamp).ToList();
                }
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка загрузки сообщений: {ex.Message}";
        }
    }

    /// <summary>
    /// Отправка сообщения
    /// </summary>
    public async Task SendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(MessageText))
            return;

        IsSending = true;
        ErrorMessage = string.Empty;

        var (success, msg) = await _chatService.SendMessageAsync(MessageText);

        if (success)
        {
            MessageText = string.Empty;
           
        }
        else
        {
            ErrorMessage = msg;
        }

        IsSending = false;
        NotifyStateChanged();
    }

    /// <summary>
    /// Обработка нажатия клавиш (Enter для отправки)
    /// </summary>
    public async Task HandleKeyDownAsync(string key, bool shiftKey)
    {
        if (key == "Enter" && !shiftKey)
        {
            await SendMessageAsync();
        }
    }

    /// <summary>
    /// Выход из системы
    /// </summary>
    public async Task LogoutAsync()
    {
        await _authService.LogoutAsync();
        _navigationManager.NavigateTo("/");
    }

    /// <summary>
    /// Уведомление UI об изменении состояния
    /// </summary>
    private void NotifyStateChanged()
    {
        OnStateChanged?.Invoke();
    }

    /// <summary>
    /// Освобождение ресурсов
    /// </summary>
    public void Dispose()
    {
        _pollingTimer?.Dispose();
    }
}
