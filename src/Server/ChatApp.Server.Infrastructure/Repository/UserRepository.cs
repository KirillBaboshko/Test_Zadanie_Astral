using ChatApp.Server.Domain.Entities;
using ChatApp.Server.Domain.Repositories;
using ChatApp.Server.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Server.Infrastructure.Repository;

/// <summary>
/// Репозиторий для работы с пользователями в базе данных
/// </summary>
public sealed class UserRepository : IUserRepository
{
    private readonly ChatDbContext _context;

    public UserRepository(ChatDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
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
    /// Добавляет нового пользователя в контекст БД
    /// </summary>
    public async Task<User> AddAsync(User user, CancellationToken cancellationToken = default)
    {
        await _context.Users.AddAsync(user, cancellationToken);
        return user;
    }

    /// <summary>
    /// Сохраняет изменения в базе данных
    /// </summary>
    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Обновляет данные пользователя в контексте БД
    /// </summary>
    public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
       _context.Users.Update(user);
    }

    /// <summary>
    /// Получает список всех пользователей, отсортированных по имени
    /// </summary>
    public async Task<List<User>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .OrderBy(u => u.Username)
            .ToListAsync(cancellationToken);
    }
}
