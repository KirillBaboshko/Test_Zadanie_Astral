using MediatR;

namespace ChatApp.Server.Application.Commands.SendMessage;

/// <summary>
/// Команда для отправки сообщения
/// </summary>
public sealed record SendMessageCommand(Guid UserId, string Content) : IRequest<SendMessageResponse>;
