using ChatApp.Contracts.Responses;
using ChatApp.Server.Domain.Repositories;
using MediatR;

namespace ChatApp.Server.Application.Queries.GetUserInfo;

/// <summary>
/// Handler для получения информации о пользователе
/// </summary>
public sealed class GetUserInfoQueryHandler : IRequestHandler<GetUserInfoQuery, UserInfoDto?>
{
    private readonly IUserRepository _userRepository;

    public GetUserInfoQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<UserInfoDto?> Handle(GetUserInfoQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByUsernameWithMessagesAsync(request.Username, cancellationToken);

        if (user == null)
        {
            return null;
        }

        return new UserInfoDto
        {
            Id = user.Id,
            Username = user.Username,
            CreatedAt = user.CreatedAt,
            LastLogin = user.LastSeenAt,
            MessageCount = user.Messages.Count
        };
    }
}
