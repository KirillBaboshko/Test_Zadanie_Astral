using System.Net;
using System.Net.Sockets;
using Test_Zadanie_Astral.Application.Services;
using Test_Zadanie_Astral.Domain.Interfaces;
using Test_Zadanie_Astral.Infrastructure.Protocol;
using Test_Zadanie_Astral.Infrastructure.Repositories;
using Test_Zadanie_Astral.Infrastructure.Transport;
using Test_Zadanie_Astral.Presentation;
using static System.Console;

namespace Test_Zadanie_Astral.Application;

public static class Application
{
    public static async Task RunAsync(CancellationTokenSource cts)
    {
        ApplicationMode? mode = ConsoleUI.SelectMode();
        if (!mode.HasValue)
        {
            WriteLine("Некорректный режим. Введите 1 или 2.");
            return;
        }

        try
        {
            switch (mode.Value)
            {
                case ApplicationMode.Server:
                    await RunServerAsync(cts);
                    break;

                case ApplicationMode.Client:
                    await RunClientAsync(cts);
                    break;
            }
        }
        catch (OperationCanceledException)
        {
            WriteLine("Операция отменена.");
        }
    }

    private static async Task RunServerAsync(CancellationTokenSource cts)
    {
        Int32? port = ConsoleUI.ReadPort("Порт сервера: ");
        if (!port.HasValue)
            return;

        ITransport transport;
        try
        {
            transport = new UdpTransport(port.Value);
        }
        catch (SocketException)
        {
            WriteLine($"Не удалось запустить сервер на порту {port}.");
            WriteLine("Порт уже занят — возможно, сервер уже запущен.");
            return;
        }

        using (transport)
        {
            WriteLine();
            WriteLine($"Сервер запущен на UDP-порту {port}.");

            IProtocolSerializer serializer = new ProtocolSerializer();
            IUserRepository userRepository = new InMemoryUserRepository();
            ChatServerService server = new(transport, serializer, userRepository);

            _ = Task.Run(() =>
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    String? command = ReadLine()?.Trim();
                    if (String.IsNullOrWhiteSpace(command))
                        continue;

                    if (command.Equals("exit", StringComparison.OrdinalIgnoreCase))
                    {
                        cts.Cancel();
                        break;
                    }

                    server.HandleConsoleCommand(command);
                }
            });

            await server.RunAsync(cts.Token);
            WriteLine("Сервер остановлен.");
        }
    }

    private static async Task RunClientAsync(CancellationTokenSource cts)
    {
        (IPAddress? address, Int32 port) = ConsoleUI.ReadServerAddress();
        if (address is null || port == 0)
            return;

        String userName = ConsoleUI.ReadName("Ваше имя: ");

        using ITransport transport = new UdpTransport(0);
        IProtocolSerializer serializer = new ProtocolSerializer();
        IPEndPoint serverEndPoint = new(address, port);

        ChatClientService client = new(transport, serializer, serverEndPoint, userName);
        await client.RunAsync(cts.Token);
    }
}
