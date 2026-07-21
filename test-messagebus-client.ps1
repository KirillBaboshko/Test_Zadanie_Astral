# Скрипт для тестирования Message Bus клиента
Write-Host "=== Тестирование Message Bus Client ===" -ForegroundColor Cyan
Write-Host ""

# Запуск клиента с автоматическим вводом
$input = @"
3
localhost
1
testuser1
testpass
/exit
"@

$input | dotnet run --project src/Client/ChatApp.Client.Console/ChatApp.Client.Console.csproj
