using System.Collections.Concurrent;
using Test_Zadanie_Astral.Domain.Interfaces;
using Test_Zadanie_Astral.Domain.Models;

namespace Test_Zadanie_Astral.Infrastructure.Repositories;


public sealed class InMemoryUserRepository : IUserRepository
{
    private readonly ConcurrentDictionary<String, User> _usersByEndPoint = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<String, String> _endPointByName = new(StringComparer.OrdinalIgnoreCase);

    public Boolean TryAdd(User user)
    {
        if (_usersByEndPoint.TryAdd(user.EndPointKey, user))
        {
            _endPointByName[user.Name] = user.EndPointKey;
            return true;
        }

        return false;
    }

    public Boolean TryRemove(String endPointKey)
    {
        if (_usersByEndPoint.TryRemove(endPointKey, out User? user))
        {
            _endPointByName.TryRemove(user.Name, out _);
            return true;
        }

        return false;
    }

    public Boolean TryGetByEndPoint(String endPointKey, out User? user) =>
        _usersByEndPoint.TryGetValue(endPointKey, out user);

    public Boolean TryGetByName(String name, out User? user)
    {
        if (_endPointByName.TryGetValue(name, out String? endPointKey))
            return _usersByEndPoint.TryGetValue(endPointKey, out user);

        user = null;
        return false;
    }

    public Boolean IsNameTaken(String name, String? excludeEndPointKey = null)
    {
        if (!_endPointByName.TryGetValue(name, out String? existingEndPoint))
            return false;

        if (excludeEndPointKey is not null && existingEndPoint.Equals(excludeEndPointKey, StringComparison.Ordinal))
            return false;

        return true;
    }

    public IReadOnlyCollection<User> GetAll() =>
        _usersByEndPoint.Values.ToList();

    public Int32 Count => _usersByEndPoint.Count;
}
