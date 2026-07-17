using ChatApp.Contracts.Messages;
using ChatApp.Contracts.Requests;
using ChatApp.Server.Application.Common;
using ChatApp.Server.Domain.Abstractions;
using ChatApp.Server.Domain.Entities;
using ChatApp.Server.Domain.Repositories;

namespace ChatApp.Server.Application.UseCases.SendMessage;

/// <summary>
/// Use case для отправки сообщений
/// </summary>
public sealed class SendMessageUseCase : UseCaseBase
{
    private readonly IUserRepository _userRepository;

    public SendMessageUseCase(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork) : base(unitOfWork)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
    }

    /// <summary>
    /// Отправляет новое сообщение от авторизованного пользователя (по userId из JWT)
    /// </summary>
    public async Task<ChatMessageDto?> ExecuteAuthAsync(Guid userId, SendMessageAuthRequest request, CancellationToken cancellationToken = default)
    {
        return await ExecuteWithUnitOfWorkAsync(async ct =>
        {
            var user = await _userRepository.GetByIdAsync(userId, ct);
            
            if (user == null)
                return null;

            user.UpdateLastSeen();
            var message = user.AddMessage(request.Content);

            return new ChatMessageDto
            {
                Id = message.Id,
                SenderName = user.Username,
                Content = message.Content,
                Timestamp = message.Timestamp
            };
        }, cancellationToken);
    }
}
