# Скрипт для тестирования ChatApp API в Docker

Write-Host "=== Тестирование ChatApp API ===" -ForegroundColor Cyan
Write-Host ""

$baseUrl = "http://localhost:5096"

# 1. Регистрация пользователя
Write-Host "1. Регистрация нового пользователя..." -ForegroundColor Yellow
try {
    $registerBody = '{"username":"dockeruser","password":"docker123"}'
    $registerResponse = Invoke-RestMethod -Uri "$baseUrl/api/auth/register" -Method POST -ContentType "application/json" -Body $registerBody
    Write-Host "OK - Пользователь зарегистрирован успешно!" -ForegroundColor Green
    $token = $registerResponse.token
} catch {
    Write-Host "WARN - Ошибка регистрации (возможно пользователь уже существует)" -ForegroundColor Red
    
    # Пробуем войти
    Write-Host "  Попытка входа..." -ForegroundColor Yellow
    $loginBody = '{"username":"dockeruser","password":"docker123"}'
    $loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method POST -ContentType "application/json" -Body $loginBody
    Write-Host "OK - Вход выполнен успешно!" -ForegroundColor Green
    $token = $loginResponse.token
}

Write-Host ""

# 2. Отправка сообщений
Write-Host "2. Отправка сообщений..." -ForegroundColor Yellow
$headers = @{Authorization="Bearer $token"}

$messages = @(
    "Hello from Docker!",
    "Test message 1",
    "Test message 2"
)

foreach ($msg in $messages) {
    $messageBody = "{`"content`":`"$msg`"}"
    $result = Invoke-RestMethod -Uri "$baseUrl/api/chat/messages" -Method POST -Headers $headers -ContentType "application/json; charset=utf-8" -Body ([System.Text.Encoding]::UTF8.GetBytes($messageBody))
    Write-Host "  OK - Отправлено: $msg" -ForegroundColor Green
}

Write-Host ""

# 3. Получение всех сообщений
Write-Host "3. Получение всех сообщений..." -ForegroundColor Yellow
$allMessages = Invoke-RestMethod -Uri "$baseUrl/api/chat/messages" -Method GET
Write-Host "  Всего сообщений: $($allMessages.totalCount)" -ForegroundColor Cyan

foreach ($msg in $allMessages.messages) {
    $time = ([DateTime]$msg.timestamp).ToLocalTime().ToString("HH:mm:ss")
    Write-Host "  [$time] $($msg.senderName): $($msg.content)" -ForegroundColor White
}

Write-Host ""

# 4. Получение списка пользователей
Write-Host "4. Получение списка пользователей..." -ForegroundColor Yellow
$users = Invoke-RestMethod -Uri "$baseUrl/api/chat/users" -Method GET
Write-Host "  Всего пользователей: $($users.Count)" -ForegroundColor Cyan

foreach ($user in $users) {
    Write-Host "  - $($user.username) (ID: $($user.id))" -ForegroundColor White
}

Write-Host ""
Write-Host "=== Тестирование завершено ===" -ForegroundColor Cyan
