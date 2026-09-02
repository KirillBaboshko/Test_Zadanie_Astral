using ChatApp.Contracts.Responses;
using MediatR;

namespace ChatApp.Server.Application.Queries.GetUsers;

/// <summary>
/// Запрос для получения списка всех пользователей
/// </summary>
public sealed record GetUsersQuery : IRequest<List<UserDto>>;
