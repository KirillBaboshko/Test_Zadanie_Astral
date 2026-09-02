using ChatApp.Contracts.Responses;
using MediatR;

namespace ChatApp.Server.Application.Queries.GetUserInfo;

/// <summary>
/// Запрос для получения детальной информации о пользователе
/// </summary>
public sealed record GetUserInfoQuery(string Username) : IRequest<UserInfoDto?>;
