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
