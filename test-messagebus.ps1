# Скрипт для тестирования Message Bus интеграции
Write-Host "=== Тестирование Message Bus (Async RabbitMQ) ===" -ForegroundColor Cyan
Write-Host ""

Write-Host "АРХИТЕКТУРА:" -ForegroundColor Yellow
Write-Host "Клиент → RabbitMQ (команды) → Server Consumer → БД → RabbitMQ (события)" -ForegroundColor Gray
Write-Host "Server → RabbitMQ (события) → Клиент Consumer → отображение в консоли" -ForegroundColor Gray
Write-Host ""

Write-Host "ТРЕБОВАНИЯ:" -ForegroundColor Yellow
Write-Host "1. Docker Desktop запущен" -ForegroundColor Gray
Write-Host "2. RabbitMQ контейнер запущен (.\start-rabbitmq.ps1)" -ForegroundColor Gray
Write-Host "3. Сервер запущен (dotnet run в src\Server\ChatApp.Server.Api)" -ForegroundColor Gray
Write-Host ""

Write-Host "КОМАНДЫ НА СЕРВЕРЕ:" -ForegroundColor Yellow
Write-Host "- RegisterUserCommand → RegisterUserCommandConsumer" -ForegroundColor Green
Write-Host "- LoginUserCommand → LoginUserCommandConsumer" -ForegroundColor Green
Write-Host "- SendMessageCommand → SendMessageCommandConsumer" -ForegroundColor Green
Write-Host ""

Write-Host "СОБЫТИЯ ДЛЯ КЛИЕНТА:" -ForegroundColor Yellow
Write-Host "- MessageSentEvent → отображается в реальном времени" -ForegroundColor Green
Write-Host "- UserRegisteredEvent → уведомление о новом пользователе" -ForegroundColor Green
Write-Host ""

Write-Host "КАК ЗАПУСТИТЬ КЛИЕНТА:" -ForegroundColor Yellow
Write-Host "cd src\Client\ChatApp.Client.Console" -ForegroundColor Cyan
Write-Host "dotnet run" -ForegroundColor Cyan
Write-Host ""
Write-Host "В меню выбрать:" -ForegroundColor Yellow
Write-Host "3. Message Bus (Async RabbitMQ)" -ForegroundColor Cyan
Write-Host ""

Write-Host "ЧТО ПРОИЗОЙДЁТ:" -ForegroundColor Yellow
Write-Host "1. Клиент подключится к RabbitMQ" -ForegroundColor Gray
Write-Host "2. При регистрации:" -ForegroundColor Gray
Write-Host "   - Клиент отправит RegisterUserCommand" -ForegroundColor Gray
Write-Host "   - Сервер обработает и вернёт токен" -ForegroundColor Gray
Write-Host "   - Сервер опубликует UserRegisteredEvent" -ForegroundColor Gray
Write-Host "   - ВСЕ клиенты получат уведомление" -ForegroundColor Gray
Write-Host ""
Write-Host "3. При отправке сообщения:" -ForegroundColor Gray
Write-Host "   - Клиент отправит SendMessageCommand (fire-and-forget)" -ForegroundColor Gray
Write-Host "   - Сервер сохранит в БД" -ForegroundColor Gray
Write-Host "   - Сервер опубликует MessageSentEvent" -ForegroundColor Gray
Write-Host "   - ВСЕ подключённые клиенты МГНОВЕННО увидят сообщение" -ForegroundColor Gray
Write-Host ""

Write-Host "ПРЕИМУЩЕСТВА:" -ForegroundColor Yellow
Write-Host "[OK] Полная асинхронность" -ForegroundColor Green
Write-Host "[OK] Реальное время (как Telegram/Slack)" -ForegroundColor Green
Write-Host "[OK] Отправка через команды" -ForegroundColor Green
Write-Host "[OK] Получение через события" -ForegroundColor Green
Write-Host "[OK] Масштабируемость (множество клиентов)" -ForegroundColor Green
Write-Host ""

Write-Host "ПРОВЕРКА СТАТУСА:" -ForegroundColor Yellow
Write-Host ""

# Проверка Docker
try {
    docker --version | Out-Null
    Write-Host "[OK] Docker установлен" -ForegroundColor Green
} catch {
    Write-Host "[X] Docker не найден" -ForegroundColor Red
    exit 1
}

# Проверка RabbitMQ
try {
    $response = Invoke-WebRequest -Uri "http://localhost:15672" -TimeoutSec 2 -UseBasicParsing -ErrorAction Stop
    Write-Host "[OK] RabbitMQ запущен (http://localhost:15672)" -ForegroundColor Green
} catch {
    Write-Host "[X] RabbitMQ не запущен. Запустите: .\start-rabbitmq.ps1" -ForegroundColor Red
}

# Проверка сервера
try {
    $response = Invoke-WebRequest -Uri "http://localhost:5096" -TimeoutSec 2 -UseBasicParsing -ErrorAction Stop
    Write-Host "[OK] Сервер запущен (http://localhost:5096)" -ForegroundColor Green
} catch {
    Write-Host "[X] Сервер не запущен" -ForegroundColor Red
}

Write-Host ""
Write-Host "Готово к тестированию!" -ForegroundColor Cyan
