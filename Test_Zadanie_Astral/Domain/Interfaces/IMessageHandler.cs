using System.Net;
using Test_Zadanie_Astral.Domain.Models;

namespace Test_Zadanie_Astral.Domain.Interfaces;


public interface IMessageHandler
{
    Task HandleAsync(Message message, IPEndPoint remoteEndPoint, CancellationToken cancellationToken = default);
}
