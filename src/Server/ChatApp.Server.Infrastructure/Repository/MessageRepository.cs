using ChatApp.Server.Domain.Entities;
using ChatApp.Server.Domain.Repositories;
using ChatApp.Server.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Server.Infrastructure.Repository;

internal sealed class MessageRepository : IMessageRepository
{
    private readonly ChatDbContext _context;

    public MessageRepository(ChatDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Добавляет новое сообщение в контекст БД
    /// </summary>
    public async Task<ChatMessage> AddAsync(ChatMessage message, CancellationToken cancellationToken = default)
    {
        await _context.Messages.AddAsync(message, cancellationToken);
        return message;
    }

    /// <summary>
    /// Получает список сообщений с возможностью фильтрации по времени
    /// </summary>
    public async Task<List<ChatMessage>> GetAsync(DateTime? since = null, Int32 limit = 100, CancellationToken cancellationToken = default)
    {
        var query = _context.Messages
            .AsNoTracking()
            .AsQueryable();

        if (since.HasValue)
            query = query.Where(m => m.Timestamp > since.Value);

        return await query
            .OrderBy(m => m.Timestamp)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Получает список сообщений конкретного пользователя
    /// </summary>
    public async Task<List<ChatMessage>> GetForUserIdAsync(Guid userId, Int32 limit = 100, CancellationToken cancellationToken = default)
    {
        return await _context.Messages
            .AsNoTracking()
            .Where(m => m.UserId == userId)
            .OrderBy(m => m.Timestamp)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Возвращает общее количество сообщений в базе данных
    /// </summary>
    public async Task<Int32> GetTotalCountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Messages.CountAsync(cancellationToken);
    }
   
    /// <summary>
    /// Возвращает количество сообщений конкретного пользователя
    /// </summary>
    public async Task<Int32> GetCountForUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Messages
            .Where(m => m.UserId == userId)
            .CountAsync(cancellationToken);
    }
}
