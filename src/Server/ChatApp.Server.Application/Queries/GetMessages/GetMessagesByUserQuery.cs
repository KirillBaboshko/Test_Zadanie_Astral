using ChatApp.Contracts.Responses;
using MediatR;

namespace ChatApp.Server.Application.Queries.GetMessages;

/// <summary>
/// Запрос для получения сообщений конкретного пользователя по ID
/// </summary>
public sealed record GetMessagesByUserQuery(Guid UserId, int Limit) : IRequest<GetMessagesResponse>;
