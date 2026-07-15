using ChatApp.Server.Domain.Entities;

namespace ChatApp.Server.Domain.Repositories;


public interface IMessageRepository
{
    Task<ChatMessage> AddAsync(ChatMessage message, CancellationToken cancellationToken = default);
    Task<List<ChatMessage>> GetAsync(DateTime? since = null, Int32 limit = 100, CancellationToken cancellationToken = default);
    Task<List<ChatMessage>> GetForUserIdAsync(Guid userId, Int32 limit = 100, CancellationToken cancellationToken = default);
    Task<Int32> GetTotalCountAsync(CancellationToken cancellationToken = default);
    Task<Int32> GetCountForUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
