using ChatApp.Contracts.Responses;
using MediatR;

namespace ChatApp.Server.Application.Queries.GetMessages;

/// <summary>
/// Запрос для получения сообщений с фильтрацией
/// </summary>
public sealed record GetMessagesQuery(DateTime? Since, int Limit) : IRequest<GetMessagesResponse>;
