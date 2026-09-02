using MediatR;

namespace ChatApp.Server.Application.Commands.Auth;

/// <summary>
/// Команда для входа в систему
/// </summary>
public sealed record LoginCommand(string Username, string Password) : IRequest<LoginResponse>;
