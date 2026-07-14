using System.Collections.Concurrent;
using ChatApp.Server.Domain.Entities;
using ChatApp.Server.Domain.Repositories;

namespace ChatApp.Server.Infrastructure.Persistence;

public sealed class InMemoryMessageRepository : IMessageRepository
{
    private readonly ConcurrentBag<ChatMessage> _messages = new();

    public Task<ChatMessage> AddAsync(ChatMessage message, CancellationToken cancellationToken = default)
    {
        _messages.Add(message);
        return Task.FromResult(message);
    }

    public Task<List<ChatMessage>> GetAsync(DateTime? since = null, Int32 limit = 100, CancellationToken cancellationToken = default)
    {
        var query = _messages.AsEnumerable();

        if (since.HasValue)
            query = query.Where(m => m.Timestamp > since.Value);

        var result = query
            .OrderBy(m => m.Timestamp)
            .Take(limit)
            .ToList();

        return Task.FromResult(result);
    }
    public Task<List<ChatMessage>> GetForNameAsync(Int32 limit = 100, String? senderName = null, CancellationToken cancellationToken = default)
    {
        var query = _messages.AsEnumerable();
        if (!String.IsNullOrEmpty(senderName))
            query = query.Where(m => m.SenderName == senderName);
        var result = query
            .OrderBy(m => m.Timestamp)
            .Take(limit)
            .ToList();

        return Task.FromResult(result);
    }

    public Task<Int32> GetTotalCountAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_messages.Count);
    }
}
