using ChatApp.Client.Application.Services;
using ChatApp.Contracts.Messages;
using ChatApp.Contracts.Requests;
using ChatApp.Contracts.Responses;
using ChatApp.Shared.Messages.Commands;
using ChatApp.Shared.Messages.Responses;
using MassTransit;

namespace ChatApp.Client.Infrastructure.MessageBus;

/// <summary>
/// Клиент для работы с Chat API через RabbitMQ Message Bus
/// Отправляет команды и получает ответы через Request-Response паттерн
/// </summary>
public class MessageBusApiClient : IChatApiClient
{
    private readonly IBus _bus;
    private readonly IRequestClient<RegisterUserCommand> _registerClient;
    private readonly IRequestClient<LoginUserCommand> _loginClient;
    private Guid _currentUserId;
    private string _currentUsername = string.Empty;
    private string _currentToken = string.Empty;

    public MessageBusApiClient(
        IBus bus,
        IRequestClient<RegisterUserCommand> registerClient,
        IRequestClient<LoginUserCommand> loginClient)
    {
        _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        _registerClient = registerClient ?? throw new ArgumentNullException(nameof(registerClient));
        _loginClient = loginClient ?? throw new ArgumentNullException(nameof(loginClient));
    }

    public async Task<AuthResponse?> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var command = new RegisterUserCommand
            {
                Username = request.Username,
                Password = request.Password
            };

            var response = await _registerClient.GetResponse<RegisterUserResponse>(command, cancellationToken);
            var result = response.Message;

            if (result.Success)
            {
                _currentUserId = result.UserId;
                _currentUsername = result.Username;
                _currentToken = result.Token;
                
                
                CurrentUserContext.Instance.SetCurrentUser(result.UserId, result.Username);
                
                return new AuthResponse
                {
                    Token = result.Token,
                    UserId = result.UserId,
                    Username = result.Username,
                    ExpiresAt = DateTime.UtcNow.AddHours(24)
                };
            }

            return null;
        }
        catch (RequestTimeoutException)
        {
            return null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var command = new LoginUserCommand
            {
                Username = request.Username,
                Password = request.Password
            };

            var response = await _loginClient.GetResponse<LoginUserResponse>(command, cancellationToken);
            var result = response.Message;

            if (result.Success)
            {
                _currentUserId = result.UserId;
                _currentUsername = result.Username;
                _currentToken = result.Token;
                
               
                CurrentUserContext.Instance.SetCurrentUser(result.UserId, result.Username);
                
                return new AuthResponse
                {
                    Token = result.Token,
                    UserId = result.UserId,
                    Username = result.Username,
                    ExpiresAt = DateTime.UtcNow.AddHours(24)
                };
            }

            return null;
        }
        catch (RequestTimeoutException)
        {
            return null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public void SetAuthToken(string token)
    {
        _currentToken = token;
    }

    public void ClearAuthToken()
    {
        _currentToken = string.Empty;
        _currentUserId = Guid.Empty;
        _currentUsername = string.Empty;
        
        // Очищаем контекст пользователя
        CurrentUserContext.Instance.Clear();
    }

    public async Task<ChatMessageDto?> SendMessageAsync(SendMessageAuthRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            
            var command = new SendMessageCommand
            {
                UserId = _currentUserId,
                Username = _currentUsername,
                Content = request.Content
            };

            await _bus.Publish(command, cancellationToken);
            
            return new ChatMessageDto
            {
                Id = Guid.NewGuid(),
                SenderName = _currentUsername,
                Content = request.Content,
                Timestamp = DateTime.UtcNow
            };
        }
        catch (Exception)
        {
            return null;
        }
    }

    public Task<GetMessagesResponse?> GetMessagesAsync(DateTime? since = null, int limit = 100, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<GetMessagesResponse?>(new GetMessagesResponse
        {
            Messages = new List<ChatMessageDto>(),
            TotalCount = 0
        });
    }

    public Task<GetMessagesResponse?> GetMessagesForNameAsync(int limit = 100, string? senderName = null, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<GetMessagesResponse?>(new GetMessagesResponse
        {
            Messages = new List<ChatMessageDto>(),
            TotalCount = 0
        });
    }
}
