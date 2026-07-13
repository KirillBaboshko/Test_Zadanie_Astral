using System.Net;
using Test_Zadanie_Astral.Domain.Interfaces;
using Test_Zadanie_Astral.Domain.Models;
using static System.Console;

namespace Test_Zadanie_Astral.Application.Services;

public sealed class ChatServerService : IMessageHandler
{
    private readonly ITransport _transport;
    private readonly IProtocolSerializer _serializer;
    private readonly IUserRepository _userRepository;

    public ChatServerService(
        ITransport transport,
        IProtocolSerializer serializer,
        IUserRepository userRepository)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        WriteLine("Команды: list — список клиентов, exit — остановка.");
        WriteLine();
        PrintConnectedClients();

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                (IPEndPoint remoteEndPoint, Byte[] data) = await _transport.ReceiveAsync(cancellationToken);

                if (_serializer.TryDeserialize(data, out Message? message) && message is not null)
                    await HandleAsync(message, remoteEndPoint, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    public async Task HandleAsync(Message message, IPEndPoint remoteEndPoint, CancellationToken cancellationToken = default)
    {
        String endPointKey = $"{remoteEndPoint.Address}:{remoteEndPoint.Port}";

        switch (message.Type)
        {
            case MessageType.Join:
                await HandleJoinAsync(remoteEndPoint, endPointKey, message.Content, cancellationToken);
                break;

            case MessageType.Chat:
                await HandleChatMessageAsync(endPointKey, message.Content, cancellationToken);
                break;

            case MessageType.Leave:
                await HandleLeaveAsync(endPointKey, cancellationToken);
                break;
        }
    }

    private async Task HandleJoinAsync(IPEndPoint remoteEndPoint, String endPointKey, String name, CancellationToken cancellationToken)
    {
        if (String.IsNullOrWhiteSpace(name))
        {
            await SendToAsync(remoteEndPoint, Message.Error("Имя не может быть пустым."), cancellationToken);
            return;
        }

        if (_userRepository.IsNameTaken(name, endPointKey))
        {
            await SendToAsync(remoteEndPoint, Message.Error("Имя уже занято."), cancellationToken);
            return;
        }

        _userRepository.TryRemove(endPointKey);

        User user = new(name, remoteEndPoint);
        _userRepository.TryAdd(user);

        await SendToAsync(remoteEndPoint, Message.Ok("Подключено к серверу."), cancellationToken);
        await BroadcastAsync(Message.System($"{name} присоединился к чату."), cancellationToken);

        WriteLine($"[{DateTime.Now:HH:mm:ss}] {name} подключился ({remoteEndPoint}).");
        PrintConnectedClients();
    }

    private async Task HandleChatMessageAsync(String endPointKey, String content, CancellationToken cancellationToken)
    {
        if (String.IsNullOrWhiteSpace(content))
            return;

        if (!_userRepository.TryGetByEndPoint(endPointKey, out User? user) || user is null)
            return;

        Message chatMessage = Message.Chat(user.Name, content);
        await BroadcastAsync(chatMessage, cancellationToken);

        WriteLine($"[{DateTime.Now:HH:mm:ss}] {user.Name}: {content}");
    }

    private async Task HandleLeaveAsync(String endPointKey, CancellationToken cancellationToken)
    {
        if (!_userRepository.TryGetByEndPoint(endPointKey, out User? user) || user is null)
            return;

        _userRepository.TryRemove(endPointKey);
        await BroadcastAsync(Message.System($"{user.Name} покинул чат."), cancellationToken);

        WriteLine($"[{DateTime.Now:HH:mm:ss}] {user.Name} отключился.");
        PrintConnectedClients();
    }

    private async Task BroadcastAsync(Message message, CancellationToken cancellationToken)
    {
        Byte[] data = _serializer.Serialize(message);

        foreach (User user in _userRepository.GetAll())
            await _transport.SendAsync(data, user.EndPoint, cancellationToken);
    }

    private Task SendToAsync(IPEndPoint endPoint, Message message, CancellationToken cancellationToken)
    {
        Byte[] data = _serializer.Serialize(message);
        return _transport.SendAsync(data, endPoint, cancellationToken);
    }

    private void PrintConnectedClients()
    {
        if (_userRepository.Count == 0)
        {
            WriteLine("Подключённых клиентов нет.");
            return;
        }

        WriteLine($"Подключено клиентов: {_userRepository.Count}");
        Int32 index = 1;
        foreach (User user in _userRepository.GetAll().OrderBy(u => u.Name, StringComparer.OrdinalIgnoreCase))
        {
            WriteLine($"  {index}. {user.Name} ({user.EndPointKey})");
            index++;
        }
    }

    public void HandleConsoleCommand(String command)
    {
        if (command.Equals("list", StringComparison.OrdinalIgnoreCase) ||
            command.Equals("clients", StringComparison.OrdinalIgnoreCase))
        {
            PrintConnectedClients();
        }
    }
}
