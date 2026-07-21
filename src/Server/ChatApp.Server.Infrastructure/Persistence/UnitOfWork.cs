using ChatApp.Server.Domain.Abstractions;
using ChatApp.Server.Infrastructure.Data;

namespace ChatApp.Server.Infrastructure.Persistence;


public sealed class UnitOfWork : IUnitOfWork
{
    private readonly ChatDbContext _context;

    public UnitOfWork(ChatDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Сохраняет все изменения, сделанные в контексте БД, в рамках одной транзакции
    /// </summary>
    /// <returns>Количество записей, затронутых операцией</returns>
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }
}
