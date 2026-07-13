using System.Net;
using System.Net.Sockets;
using Test_Zadanie_Astral.Domain.Interfaces;

namespace Test_Zadanie_Astral.Infrastructure.Transport;


public sealed class UdpTransport : ITransport
{
    private readonly UdpClient _udpClient;
    private Boolean _disposed;

    public UdpTransport(Int32 port)
    {
        _udpClient = new UdpClient(port);
    }

    public async Task<(IPEndPoint RemoteEndPoint, Byte[] Data)> ReceiveAsync(CancellationToken cancellationToken)
    {
        UdpReceiveResult result = await _udpClient.ReceiveAsync(cancellationToken);
        return (result.RemoteEndPoint, result.Buffer);
    }

    public async Task SendAsync(Byte[] data, IPEndPoint endPoint, CancellationToken cancellationToken = default)
    {
        await _udpClient.SendAsync(data, endPoint, cancellationToken);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _udpClient?.Dispose();
        _disposed = true;
    }
}
