using ChatApp.Server.Domain.Entities;

namespace ChatApp.Server.Domain.Repositories;

using ChatApp.Server.Domain.Entities;

namespace ChatApp.Server.Domain.Repositories;

public interface IUserRepository
{
    Task<User?> GetByUsernameAsync(String username, CancellationToken cancellationToken = default);
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task SaveAsync(CancellationToken cancellationToken = default);
    Task<User> AddAsync(User user, CancellationToken cancellationToken = default);
    Task UpdateAsync(User user, CancellationToken cancellationToken = default);
    Task<List<User>> GetAllAsync(CancellationToken cancellationToken = default);
}
