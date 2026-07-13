using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Test_Zadanie_Astral.Models;
using static System.Console;

namespace Test_Zadanie_Astral.Infrastructure
{
    public class App
    {
        static private async Task ReceiveMessagesAsync(UdpClient udp, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    UdpReceiveResult result = await udp.ReceiveAsync(cancellationToken);
                    String text = Encoding.UTF8.GetString(result.Buffer);
                    WriteLine();
                    WriteLine($"[{DateTime.Now:HH:mm:ss}] {text}");
                    Write("> ");
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
            }
        }
        static public async Task Run(ChatUser user, IPEndPoint remoteEndPoint, UdpClient udp, CancellationTokenSource cts)
        {
            CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
            };

            WriteLine();
            WriteLine($"Слушаю UDP-порт {user.ListenPort}.");
            WriteLine($"Отправка сообщений на {remoteEndPoint}.");
            WriteLine("Введите сообщение. Команды: exit — выход.");
            WriteLine();

            var receiveTask = ReceiveMessagesAsync(udp, cts.Token);

            try
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    Write("> ");
                    String? message = ReadLine();

                    if (message is null || message.Equals("exit", StringComparison.OrdinalIgnoreCase))
                        break;

                    if (String.IsNullOrWhiteSpace(message))
                        continue;

                    byte[] payload = Encoding.UTF8.GetBytes($"{user.Name}: {message}");
                    await udp.SendAsync(payload, remoteEndPoint);
                }
            }
            finally
            {
                cts.Cancel();
                try
                {
                    await receiveTask;
                }
                catch (OperationCanceledException)
                {
                }
            }

            WriteLine("Завершение работы.");
        }
    }
}
