using ChatApp.Contracts.Messages;
using ChatApp.Contracts.Requests;
using ChatApp.Server.Domain.Abstractions;
using ChatApp.Server.Domain.Entities;
using ChatApp.Server.Domain.Repositories;

namespace ChatApp.Server.Application.UseCases.SendMessage;

public sealed class SendMessageUseCase
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SendMessageUseCase(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    /// <summary>
    /// Отправляет новое сообщение от существующего пользователя (старая версия, deprecated)
    /// </summary>
    public async Task<ChatMessageDto?> ExecuteAsync(SendMessageRequest request, CancellationToken cancellationToken = default)
    { 
        var user = await _userRepository.GetByUsernameAsync(request.SenderName, cancellationToken);
        
        if (user == null)
            return null; // Пользователь не найден

        user.UpdateLastSeen();
        await _userRepository.UpdateAsync(user, cancellationToken);

        // Добавляем сообщение через агрегат User
        var message = user.AddMessage(request.Content);
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new ChatMessageDto
        {
            Id = message.Id,
            SenderName = user.Username,
            Content = message.Content,
            Timestamp = message.Timestamp
        };
    }

    /// <summary>
    /// Отправляет новое сообщение от авторизованного пользователя (по userId из JWT)
    /// </summary>
    public async Task<ChatMessageDto?> ExecuteAuthAsync(Guid userId, SendMessageAuthRequest request, CancellationToken cancellationToken = default)
    { 
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        
        if (user == null)
            return null; // Пользователь не найден

        user.UpdateLastSeen();
        await _userRepository.UpdateAsync(user, cancellationToken);

        // Добавляем сообщение через агрегат User
        var message = user.AddMessage(request.Content);
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new ChatMessageDto
        {
            Id = message.Id,
            SenderName = user.Username,
            Content = message.Content,
            Timestamp = message.Timestamp
        };
    }
}
