using MediatR;

namespace ChatApp.Server.Application.Commands.Auth;

/// <summary>
/// Команда для регистрации нового пользователя
/// </summary>
public sealed record RegisterCommand(string Username, string Password) : IRequest<RegisterResponse>;
