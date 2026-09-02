using ChatApp.Contracts.Responses;
using MediatR;

namespace ChatApp.Server.Application.Queries.GetMessages;

/// <summary>
/// Запрос для получения сообщений пользователя по имени
/// </summary>
public sealed record GetMessagesByUsernameQuery(string Username, int Limit) : IRequest<GetMessagesResponse?>;
