using System.Net;
using System.Net.Sockets;
using Test_Zadanie_Astral.Domain.Interfaces;
using Test_Zadanie_Astral.Domain.Models;
using static System.Console;

namespace Test_Zadanie_Astral.Application.Services;


public sealed class ChatClientService
{
    private readonly ITransport _transport;
    private readonly IProtocolSerializer _serializer;
    private readonly IPEndPoint _serverEndPoint;
    private readonly String _userName;

    public ChatClientService(
        ITransport transport,
        IProtocolSerializer serializer,
        IPEndPoint serverEndPoint,
        String userName)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _serverEndPoint = serverEndPoint ?? throw new ArgumentNullException(nameof(serverEndPoint));
        _userName = userName ?? throw new ArgumentNullException(nameof(userName));
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        WriteLine($"Подключение к серверу {_serverEndPoint}...");

        await SendMessageAsync(Message.Join(_userName), cancellationToken);

        if (!await WaitForJoinResponseAsync(cancellationToken))
            return;

        WriteLine("Введите сообщение. Команды: exit — выход.");
        WriteLine();

        Task receiveTask = ReceiveMessagesAsync(cancellationToken);

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                Write("> ");
                String? input = ReadLine();

                if (input is null || input.Equals("exit", StringComparison.OrdinalIgnoreCase))
                {
                    await SendMessageAsync(Message.Leave(), cancellationToken);
                    break;
                }

                if (String.IsNullOrWhiteSpace(input))
                    continue;

                Byte[] data = System.Text.Encoding.UTF8.GetBytes($"MSG|{input}");
                await _transport.SendAsync(data, _serverEndPoint, cancellationToken);
            }
        }
        finally
        {
            try
            {
                await receiveTask;
            }
            catch (OperationCanceledException)
            {
            }
        }

        WriteLine("Отключено от сервера.");
    }

    private async Task<Boolean> WaitForJoinResponseAsync(CancellationToken cancellationToken)
    {
        using CancellationTokenSource timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(5));

        try
        {
            (_, Byte[] data) = await _transport.ReceiveAsync(timeoutCts.Token);

            if (!_serializer.TryDeserialize(data, out Message? message) || message is null)
            {
                WriteLine("Некорректный ответ сервера.");
                return false;
            }

            switch (message.Type)
            {
                case MessageType.Ok:
                    WriteLine(message.Content);
                    return true;

                case MessageType.Error:
                    WriteLine($"Ошибка подключения: {message.Content}");
                    return false;

                default:
                    WriteLine("Неожиданный ответ сервера.");
                    return false;
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            WriteLine("Сервер не ответил. Убедитесь, что сервер запущен и адрес указан верно.");
            return false;
        }
    }

    private async Task ReceiveMessagesAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                (_, Byte[] data) = await _transport.ReceiveAsync(cancellationToken);

                if (!_serializer.TryDeserialize(data, out Message? message) || message is null)
                    continue;

                switch (message.Type)
                {
                    case MessageType.Chat:
                        WriteLine();
                        WriteLine($"[{DateTime.Now:HH:mm:ss}] {message.SenderName}: {message.Content}");
                        Write("> ");
                        break;

                    case MessageType.System:
                        WriteLine();
                        WriteLine($"[{DateTime.Now:HH:mm:ss}] {message.Content}");
                        Write("> ");
                        break;

                    case MessageType.Ok:
                    case MessageType.Error:
                        break;
                }
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

    private Task SendMessageAsync(Message message, CancellationToken cancellationToken)
    {
        Byte[] data = _serializer.Serialize(message);
        return _transport.SendAsync(data, _serverEndPoint, cancellationToken);
    }
}
