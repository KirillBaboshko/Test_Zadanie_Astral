using System.Net;

namespace Test_Zadanie_Astral.Domain.Models;


public sealed class User
{
    public String Name { get; }
    public IPEndPoint EndPoint { get; }
    public DateTime ConnectedAt { get; }

    public User(String name, IPEndPoint endPoint)
    {
        if (String.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Имя пользователя не может быть пустым.", nameof(name));

        Name = name.Trim();
        EndPoint = endPoint ?? throw new ArgumentNullException(nameof(endPoint));
        ConnectedAt = DateTime.Now;
    }

    public String EndPointKey => $"{EndPoint.Address}:{EndPoint.Port}";

    public override Boolean Equals(Object? obj) =>
        obj is User other && EndPointKey.Equals(other.EndPointKey, StringComparison.Ordinal);

    public override Int32 GetHashCode() => EndPointKey.GetHashCode(StringComparison.Ordinal);
}
