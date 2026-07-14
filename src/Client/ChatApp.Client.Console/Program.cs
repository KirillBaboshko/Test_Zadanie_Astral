using System.Text;
using ChatApp.Client.Application.Services;
using ChatApp.Client.Infrastructure.Http;
using ChatApp.Contracts.Requests;
using static System.Console;

OutputEncoding = Encoding.UTF8;
InputEncoding = Encoding.UTF8;

WriteLine("=== HTTP Chat Client (Clean Architecture) ===");
WriteLine();


Write("URL сервера (по умолчанию http://localhost:5096): ");
String? serverUrl = ReadLine();
if (String.IsNullOrWhiteSpace(serverUrl))
    serverUrl = "http://localhost:5096";

Write("Ваше имя: ");
String? userName = ReadLine();
if (String.IsNullOrWhiteSpace(userName))
    userName = "Anonymous";

WriteLine();
WriteLine($"Подключение к серверу {serverUrl}...");
WriteLine("Команды: /exit - выход, /about-user - сообщения по имени отправителя");
WriteLine();

using var apiClient = new HttpChatApiClient(serverUrl);
using var cts = new CancellationTokenSource();

CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

var pollingService = new ChatPollingService(apiClient);


pollingService.MessageReceived += (sender, e) =>
{
    WriteLine();
    WriteLine($"[{e.Message.Timestamp.ToLocalTime():HH:mm:ss}] {e.Message.SenderName}: {e.Message.Content}");
    Write("> ");
};

var pollingTask = Task.Run(async () =>
{
    await pollingService.StartPollingAsync(userName, cts.Token);
}, cts.Token);

try
{
    while (!cts.Token.IsCancellationRequested)
    {
        Write("> ");
        String? input = ReadLine();

        if (input == null || input.Equals("/exit", StringComparison.OrdinalIgnoreCase))
        {
            cts.Cancel();
            break;
        }
        if (input.Equals("/about-user", StringComparison.OrdinalIgnoreCase))
        {
            Write("Введите имя пользователя: ");
            String? targetUserName = ReadLine();
            if (!String.IsNullOrWhiteSpace(targetUserName))
            {
                pollingService.GetMessagesByUserName(targetUserName, cts.Token).Wait();
            }
            continue;
        }

        if (String.IsNullOrWhiteSpace(input))
            continue;

        var request = new SendMessageRequest
        {
            SenderName = userName,
            Content = input
        };

        var sentMessage = await apiClient.SendMessageAsync(request, cts.Token);

        if (sentMessage != null)
        {
            pollingService.UpdateLastMessageTime(sentMessage.Timestamp);
        }
    }
}
catch (OperationCanceledException)
{
    WriteLine("Отключение...");
}

try
{
    await pollingTask;
}
catch (OperationCanceledException)
{
}

WriteLine("Отключено от сервера.");
