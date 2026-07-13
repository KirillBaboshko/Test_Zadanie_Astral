using System.Net;
using Test_Zadanie_Astral.Domain.Models;

namespace Test_Zadanie_Astral.Domain.Interfaces;

public interface IUserRepository
{
    Boolean TryAdd(User user);
    Boolean TryRemove(String endPointKey);
    Boolean TryGetByEndPoint(String endPointKey, out User? user);
    Boolean TryGetByName(String name, out User? user);
    Boolean IsNameTaken(String name, String? excludeEndPointKey = null);
    IReadOnlyCollection<User> GetAll();
    Int32 Count { get; }
}
