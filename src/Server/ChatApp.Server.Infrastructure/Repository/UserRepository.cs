using ChatApp.Server.Domain.Entities;
using ChatApp.Server.Domain.Repositories;
using ChatApp.Server.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Server.Infrastructure.Repository;


public sealed class UserRepository : IUserRepository
{
    private readonly ChatDbContext _context;
    private readonly MessageRepository _messageRepository;

    public UserRepository(ChatDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _messageRepository = new MessageRepository(context);
    }

    /// <summary>
    /// Находит пользователя по имени
    /// </summary>
    public async Task<User?> GetByUsernameAsync(String username, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Username == username, cancellationToken);
    }

    /// <summary>
    /// Находит пользователя по идентификатору
    /// </summary>
    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    /// <summary>
    /// Находит пользователя по имени вместе с его сообщениями
    /// </summary>
    public async Task<User?> GetByUsernameWithMessagesAsync(String username, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Include(u => u.Messages)
            .FirstOrDefaultAsync(u => u.Username == username, cancellationToken);
    }

    /// <summary>
    /// Находит пользователя по идентификатору вместе с его сообщениями
    /// </summary>
    public async Task<User?> GetByIdWithMessagesAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Include(u => u.Messages)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    /// <summary>
    /// Добавляет нового пользователя в контекст БД
    /// </summary>
    public async Task<User> AddAsync(User user, CancellationToken cancellationToken = default)
    {
        await _context.Users.AddAsync(user, cancellationToken);
        return user;
    }

    /// <summary>
    /// Получает список всех пользователей, отсортированных по имени
    /// </summary>
    public async Task<List<User>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AsNoTracking()
            .OrderBy(u => u.Username)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Получает все сообщения всех пользователей (делегирует к внутреннему MessageRepository)
    /// </summary>
    public async Task<List<ChatMessage>> GetAllMessagesAsync(DateTime? since = null, Int32 limit = 100, CancellationToken cancellationToken = default)
    {
        return await _messageRepository.GetAsync(since, limit, cancellationToken);
    }

    /// <summary>
    /// Получает общее количество сообщений (делегирует к внутреннему MessageRepository)
    /// </summary>
    public async Task<Int32> GetTotalMessageCountAsync(CancellationToken cancellationToken = default)
    {
        return await _messageRepository.GetTotalCountAsync(cancellationToken);
    }

    /// <summary>
    /// Получает всех пользователей вместе с их сообщениями
    /// </summary>
    public async Task<List<User>> GetAllUsersWithMessagesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AsNoTracking()
            .Include(u => u.Messages)
            .OrderBy(u => u.Username)
            .ToListAsync(cancellationToken);
    }
}
