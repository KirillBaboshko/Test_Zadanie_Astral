using System.Net;
using System.Net.Sockets;
using System.Text;
using Test_Zadanie_Astral.Infrastructure;
using Test_Zadanie_Astral.Models;
using static System.Console;

Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;

WriteLine("=== P2P Чат (UDP) ===");
WriteLine();
ChatUser user = new ChatUser();
if (!user.TryReadListenPort("Порт для прослушивания: "))
    return;
if (!user.TryParseRemoteAddress())
    return;
if (!user.TryReadName("Ваше имя: "))
    return;
IPEndPoint remoteEndPoint = new IPEndPoint(user.RemoteAddress, user.RemotePort);

using UdpClient udp = new UdpClient(user.ListenPort);
using CancellationTokenSource cts = new CancellationTokenSource();

await App.Run(user, remoteEndPoint, udp, cts);

