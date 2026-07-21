# Скрипт для запуска 3 серверов на разных портах
# Для демонстрации DNS round-robin балансировки

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Запуск 3 gRPC серверов" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Серверы будут запущены на портах:" -ForegroundColor Yellow
Write-Host "  Server 1: HTTP=5096, gRPC=5097" -ForegroundColor Green
Write-Host "  Server 2: HTTP=5196, gRPC=5197" -ForegroundColor Green
Write-Host "  Server 3: HTTP=5296, gRPC=5297" -ForegroundColor Green
Write-Host ""

$projectPath = "$PSScriptRoot\src\Server\ChatApp.Server.Api\ChatApp.Server.Api.csproj"

# Массив серверов
$servers = @(
    @{ Name = "Server-1"; HttpPort = 5096; GrpcPort = 5097; Color = "Green" },
    @{ Name = "Server-2"; HttpPort = 5196; GrpcPort = 5197; Color = "Cyan" },
    @{ Name = "Server-3"; HttpPort = 5296; GrpcPort = 5297; Color = "Yellow" }
)

$jobs = @()

# Запускаем каждый сервер в отдельном процессе
foreach ($server in $servers) {
    Write-Host "Запуск $($server.Name) (gRPC порт: $($server.GrpcPort))..." -ForegroundColor $server.Color
    
    $job = Start-Process powershell -ArgumentList @(
        "-NoExit",
        "-Command",
        "`$env:HTTP_PORT='$($server.HttpPort)'; `$env:GRPC_PORT='$($server.GrpcPort)'; `$host.UI.RawUI.WindowTitle='$($server.Name) - gRPC:$($server.GrpcPort)'; cd '$PSScriptRoot'; dotnet run --project '$projectPath'"
    ) -PassThru
    
    $jobs += $job
    Start-Sleep -Milliseconds 500
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Все серверы запущены!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "ВАЖНО: Для демонстрации round-robin балансировки:" -ForegroundColor Yellow
Write-Host "1. Откройте файл: C:\Windows\System32\drivers\etc\hosts" -ForegroundColor White
Write-Host "   (требуются права администратора)" -ForegroundColor Gray
Write-Host ""
Write-Host "2. Добавьте строку:" -ForegroundColor White
Write-Host "   127.0.0.1  grpc-loadbalancer" -ForegroundColor Cyan
Write-Host ""
Write-Host "3. Запустите клиент с URL: http://grpc-loadbalancer:5097" -ForegroundColor White
Write-Host "   (или 5197, или 5297 для тестирования разных серверов)" -ForegroundColor Gray
Write-Host ""
Write-Host "4. В логах серверов вы увидите, какой сервер обработал запрос" -ForegroundColor White
Write-Host "   Пример: [Server Port: localhost:5097] gRPC Register request..." -ForegroundColor Gray
Write-Host ""
Write-Host "Для остановки всех серверов нажмите Enter в этом окне" -ForegroundColor Yellow

Read-Host

Write-Host ""
Write-Host "Остановка серверов..." -ForegroundColor Red

foreach ($job in $jobs) {
    try {
        Stop-Process -Id $job.Id -Force -ErrorAction SilentlyContinue
    }
    catch {
        # Игнорируем ошибки, если процесс уже завершён
    }
}

Write-Host "Все серверы остановлены" -ForegroundColor Green
