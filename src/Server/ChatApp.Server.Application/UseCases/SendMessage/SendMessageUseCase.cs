using ChatApp.Contracts.Messages;
using ChatApp.Contracts.Requests;
using ChatApp.Server.Domain.Entities;
using ChatApp.Server.Domain.Repositories;
using ChatApp.Server.FixingChanges;



namespace ChatApp.Server.Application.UseCases.SendMessage;

/// <summary>
/// Use case для отправки нового сообщения в чат
/// </summary>
public sealed class SendMessageUseCase
{
    private readonly IMessageRepository _messageRepository;
    private readonly IUserRepository _userRepository;

    public SendMessageUseCase(
        IMessageRepository messageRepository,
        IUserRepository userRepository)
    {
        _messageRepository = messageRepository ?? throw new ArgumentNullException(nameof(messageRepository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
    }

    /// <summary>
    /// Отправляет новое сообщение. Создаёт пользователя, если он не существует, или обновляет время последней активности
    /// </summary>
    public async Task<ChatMessageDto> ExecuteAsync(SendMessageRequest request, CancellationToken cancellationToken = default)
    { 
        var user = await _userRepository.GetByUsernameAsync(request.SenderName, cancellationToken);
        
        if (user == null)
        {
            user = new User(request.SenderName);
            await _userRepository.AddAsync(user, cancellationToken);
        }
        else
        {
            user.UpdateLastSeen();
            await _userRepository.UpdateAsync(user, cancellationToken);
        }

        var message = new ChatMessage(user.Id, request.Content);
        var saved = await _messageRepository.AddAsync(message, cancellationToken);
        await FixingСhanges.FixChangesAsync(_userRepository, cancellationToken); //Немного криво сейчас реализованно

        return new ChatMessageDto
        {
            Id = saved.Id,
            SenderName = user.Username,
            Content = saved.Content,
            Timestamp = saved.Timestamp
        };
    }
}
