using ChatApp.Server.Domain.Entities;

namespace ChatApp.Server.Domain.Repositories;


public interface IUserRepository
{
    Task<User?> GetByUsernameAsync(String username, CancellationToken cancellationToken = default);
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<User?> GetByUsernameWithMessagesAsync(String username, CancellationToken cancellationToken = default);
    Task<User?> GetByIdWithMessagesAsync(Guid id, CancellationToken cancellationToken = default);
    Task<User> AddAsync(User user, CancellationToken cancellationToken = default);
    Task<List<User>> GetAllAsync(CancellationToken cancellationToken = default);
    
    Task<List<ChatMessage>> GetAllMessagesAsync(DateTime? since = null, Int32 limit = 100, CancellationToken cancellationToken = default);
    Task<Int32> GetTotalMessageCountAsync(CancellationToken cancellationToken = default);
    Task<List<User>> GetAllUsersWithMessagesAsync(CancellationToken cancellationToken = default);
}
