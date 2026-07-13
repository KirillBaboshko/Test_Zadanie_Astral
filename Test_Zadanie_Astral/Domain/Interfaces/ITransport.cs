using System.Net;

namespace Test_Zadanie_Astral.Domain.Interfaces;


public interface ITransport : IDisposable
{
    Task<(IPEndPoint RemoteEndPoint, Byte[] Data)> ReceiveAsync(CancellationToken cancellationToken);
    Task SendAsync(Byte[] data, IPEndPoint endPoint, CancellationToken cancellationToken = default);
}
