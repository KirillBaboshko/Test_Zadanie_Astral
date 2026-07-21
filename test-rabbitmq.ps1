# Скрипт для тестирования интеграции с RabbitMQ
Write-Host "=== Тестирование MassTransit + RabbitMQ ===" -ForegroundColor Cyan
Write-Host ""

# Проверка что RabbitMQ запущен
Write-Host "Проверка RabbitMQ..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "http://localhost:15672" -TimeoutSec 5 -UseBasicParsing
    Write-Host "[OK] RabbitMQ запущен" -ForegroundColor Green
} catch {
    Write-Host "[ОШИБКА] RabbitMQ не запущен. Запустите через: .\start-rabbitmq.ps1" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "=== Инструкция по тестированию ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "1. Запустите сервер:" -ForegroundColor Yellow
Write-Host "   cd src\Server\ChatApp.Server.Api" -ForegroundColor Gray
Write-Host "   dotnet run" -ForegroundColor Gray
Write-Host ""
Write-Host "2. В логах сервера вы должны увидеть:" -ForegroundColor Yellow
Write-Host "   - [MassTransit] Configured with RabbitMQ" -ForegroundColor Gray
Write-Host "   - Подключение к RabbitMQ" -ForegroundColor Gray
Write-Host ""
Write-Host "3. Зарегистрируйте пользователя:" -ForegroundColor Yellow
Write-Host '   Invoke-RestMethod -Method POST -Uri "http://localhost:5096/api/auth/register" -ContentType "application/json" -Body ''{"username":"testuser","password":"test123"}''' -ForegroundColor Gray
Write-Host ""
Write-Host "4. В логах сервера должно появиться:" -ForegroundColor Yellow
Write-Host "   [RabbitMQ Consumer] Новый пользователь зарегистрирован: testuser" -ForegroundColor Gray
Write-Host ""
Write-Host "5. Войдите в систему и отправьте сообщение (используя полученный token)" -ForegroundColor Yellow
Write-Host ""
Write-Host "6. Проверьте RabbitMQ Management UI:" -ForegroundColor Yellow
Write-Host "   http://localhost:15672 (guest/guest)" -ForegroundColor Cyan
Write-Host "   - Перейдите в Queues" -ForegroundColor Gray
Write-Host "   - Вы увидите очереди для consumers" -ForegroundColor Gray
Write-Host "   - Проверьте статистику сообщений" -ForegroundColor Gray
Write-Host ""
Write-Host "=== События, которые публикуются ===" -ForegroundColor Cyan
Write-Host "- UserRegisteredEvent (при регистрации)" -ForegroundColor Green
Write-Host "- UserLoggedInEvent (при входе)" -ForegroundColor Green
Write-Host "- MessageSentEvent (при отправке сообщения)" -ForegroundColor Green
Write-Host ""
