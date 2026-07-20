using System.Text;
using ChatApp.Client.Application.Services;
using ChatApp.Client.Infrastructure.Grpc;
using ChatApp.Client.Infrastructure.Http;
using ChatApp.Contracts.Requests;
using ChatApp.Contracts.Responses;
using static System.Console;

// Включаем HTTP/2 без TLS для gRPC (необходимо для Windows в dev режиме)
AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

OutputEncoding = Encoding.UTF8;
InputEncoding = Encoding.UTF8;

WriteLine("=== Chat Client с JWT аутентификацией ===");
WriteLine();

// Выбор протокола
WriteLine("Выберите протокол:");
WriteLine("1. HTTP (REST API)");
WriteLine("2. gRPC");
Write("Выбор (по умолчанию 1): ");
String? protocolChoice = ReadLine();
bool useGrpc = protocolChoice == "2";

WriteLine();
Write($"URL сервера (по умолчанию {(useGrpc ? "http://localhost:5097" : "http://localhost:5096")}): ");
String? serverUrl = ReadLine();
if (String.IsNullOrWhiteSpace(serverUrl))
    serverUrl = useGrpc ? "http://localhost:5097" : "http://localhost:5096";

WriteLine();
WriteLine($"Подключение к серверу {serverUrl} через {(useGrpc ? "gRPC" : "HTTP")}...");
WriteLine();

// Создаём клиент в зависимости от выбранного протокола
IChatApiClient apiClient = useGrpc 
    ? new GrpcChatApiClient(serverUrl) 
    : new HttpChatApiClient(serverUrl);

using var cts = new CancellationTokenSource();

CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

AuthResponse? authResponse = null;
String? currentUsername = null;

// Меню авторизации
while (authResponse == null && !cts.Token.IsCancellationRequested)
{
    WriteLine("================================");
    WriteLine("   МЕНЮ АУТЕНТИФИКАЦИИ");
    WriteLine("================================");
    WriteLine(" 1. Регистрация");
    WriteLine(" 2. Вход");
    WriteLine(" 0. Выход");
    WriteLine("================================");
    Write("Выберите действие: ");
    
    String? choice = ReadLine();
    WriteLine();

    switch (choice)
    {
        case "1": // Регистрация
            Write("Введите имя пользователя (3-100 символов, только буквы, цифры, _ и -): ");
            String? regUsername = ReadLine();
            
            Write("Введите пароль (минимум 6 символов): ");
            String? regPassword = ReadPasswordMasked();
            WriteLine();
            
            if (String.IsNullOrWhiteSpace(regUsername) || String.IsNullOrWhiteSpace(regPassword))
            {
                WriteLine("[ОШИБКА] Имя пользователя и пароль не могут быть пустыми!");
                WriteLine();
                break;
            }

            WriteLine("Регистрация...");
            var registerRequest = new RegisterRequest
            {
                Username = regUsername,
                Password = regPassword
            };

            authResponse = await apiClient.RegisterAsync(registerRequest, cts.Token);
            
            if (authResponse != null)
            {
                currentUsername = authResponse.Username;
                WriteLine($"[OK] Регистрация успешна! Добро пожаловать, {currentUsername}!");
                WriteLine($"Токен действителен до: {authResponse.ExpiresAt.ToLocalTime():dd.MM.yyyy HH:mm:ss}");
            }
            else
            {
                WriteLine("[ОШИБКА] Ошибка регистрации. Возможно, пользователь уже существует.");
            }
            WriteLine();
            break;

        case "2": // Вход
            Write("Введите имя пользователя: ");
            String? loginUsername = ReadLine();
            
            Write("Введите пароль: ");
            String? loginPassword = ReadPasswordMasked();
            WriteLine();
            
            if (String.IsNullOrWhiteSpace(loginUsername) || String.IsNullOrWhiteSpace(loginPassword))
            {
                WriteLine("[ОШИБКА] Имя пользователя и пароль не могут быть пустыми!");
                WriteLine();
                break;
            }

            WriteLine("Авторизация...");
            var loginRequest = new LoginRequest
            {
                Username = loginUsername,
                Password = loginPassword
            };

            authResponse = await apiClient.LoginAsync(loginRequest, cts.Token);
            
            if (authResponse != null)
            {
                currentUsername = authResponse.Username;
                WriteLine($"[OK] Вход выполнен! Добро пожаловать, {currentUsername}!");
                WriteLine($"Токен действителен до: {authResponse.ExpiresAt.ToLocalTime():dd.MM.yyyy HH:mm:ss}");
            }
            else
            {
                WriteLine("[ОШИБКА] Неверное имя пользователя или пароль.");
            }
            WriteLine();
            break;

        case "0": // Выход
            WriteLine("Выход из приложения...");
            cts.Cancel();
            return 0;

        default:
            WriteLine("[ОШИБКА] Неверный выбор. Попробуйте снова.");
            WriteLine();
            break;
    }
}

if (authResponse == null || currentUsername == null)
{
    WriteLine("Не удалось авторизоваться. Завершение работы.");
    return 1;
}

// Главное меню чата
WriteLine();
WriteLine("================================");
WriteLine("       ЧАТ ПРИЛОЖЕНИЕ");
WriteLine("================================");
WriteLine(" Команды:");
WriteLine(" /help     - помощь");
WriteLine(" /messages - все сообщения");
WriteLine(" /user     - сообщения юзера");
WriteLine(" /exit     - выход");
WriteLine("================================");
WriteLine();

var pollingService = new ChatPollingService(apiClient);

pollingService.MessageReceived += (sender, e) =>
{
    WriteLine();
    WriteLine($"[{e.Message.Timestamp.ToLocalTime():HH:mm:ss}] {e.Message.SenderName}: {e.Message.Content}");
    Write("> ");
};

var pollingTask = Task.Run(async () =>
{
    await pollingService.StartPollingAsync(currentUsername, cts.Token);
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

        if (input.Equals("/help", StringComparison.OrdinalIgnoreCase))
        {
            WriteLine();
            WriteLine("--- Доступные команды ---");
            WriteLine("/help     - показать эту справку");
            WriteLine("/messages - показать все сообщения");
            WriteLine("/user     - показать сообщения конкретного пользователя");
            WriteLine("/exit     - выйти из чата");
            WriteLine("Любой другой текст будет отправлен как сообщение");
            WriteLine();
            continue;
        }

        if (input.Equals("/messages", StringComparison.OrdinalIgnoreCase))
        {
            WriteLine("Загрузка всех сообщений...");
            var messagesResponse = await apiClient.GetMessagesAsync(limit: 50, cancellationToken: cts.Token);
            
            if (messagesResponse != null && messagesResponse.Messages.Count > 0)
            {
                WriteLine();
                WriteLine($"--- Последние {messagesResponse.Messages.Count} сообщений (всего: {messagesResponse.TotalCount}) ---");
                foreach (var msg in messagesResponse.Messages)
                {
                    WriteLine($"[{msg.Timestamp.ToLocalTime():HH:mm:ss}] {msg.SenderName}: {msg.Content}");
                }
                WriteLine();
            }
            else
            {
                WriteLine("Сообщений пока нет.");
            }
            continue;
        }

        if (input.Equals("/user", StringComparison.OrdinalIgnoreCase))
        {
            Write("Введите имя пользователя: ");
            String? targetUserName = ReadLine();
            
            if (!String.IsNullOrWhiteSpace(targetUserName))
            {
                await pollingService.GetMessagesByUserName(targetUserName, cts.Token);
            }
            continue;
        }

        if (String.IsNullOrWhiteSpace(input))
            continue;

        // Отправка сообщения от авторизованного пользователя
        var request = new SendMessageAuthRequest
        {
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

WriteLine("Отключено от сервера. До свидания!");

return 0;

// Вспомогательная функция для ввода пароля с маскировкой
static String ReadPasswordMasked()
{
    var password = new StringBuilder();
    ConsoleKeyInfo key;

    do
    {
        key = ReadKey(true);

        if (key.Key == ConsoleKey.Backspace && password.Length > 0)
        {
            password.Length--;
            Write("\b \b");
        }
        else if (key.Key != ConsoleKey.Enter && !Char.IsControl(key.KeyChar))
        {
            password.Append(key.KeyChar);
            Write("*");
        }
    } while (key.Key != ConsoleKey.Enter);

    return password.ToString();
}
