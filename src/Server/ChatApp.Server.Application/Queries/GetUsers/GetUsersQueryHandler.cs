using ChatApp.Contracts.Responses;
using ChatApp.Server.Domain.Repositories;
using MediatR;

namespace ChatApp.Server.Application.Queries.GetUsers;

/// <summary>
/// Handler для получения списка пользователей
/// </summary>
public sealed class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, List<UserDto>>
{
    private readonly IUserRepository _userRepository;

    public GetUsersQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<List<UserDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var users = await _userRepository.GetAllAsync(cancellationToken);

        return users.Select(u => new UserDto
        {
            Id = u.Id,
            Username = u.Username
        }).ToList();
    }
}
