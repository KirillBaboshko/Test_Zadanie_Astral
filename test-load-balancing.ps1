# Скрипт для тестирования DNS round-robin балансировки
# Запускает клиент несколько раз к разным серверам

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Тестирование балансировки" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$servers = @(
    @{ Name = "Server 1"; Port = 5097; Color = "Green" },
    @{ Name = "Server 2"; Port = 5197; Color = "Cyan" },
    @{ Name = "Server 3"; Port = 5297; Color = "Yellow" }
)

Write-Host "Этот скрипт поможет проверить, что запросы идут к разным серверам" -ForegroundColor White
Write-Host ""
Write-Host "Доступные серверы:" -ForegroundColor Yellow
foreach ($server in $servers) {
    Write-Host "  - $($server.Name): http://localhost:$($server.Port)" -ForegroundColor $server.Color
}
Write-Host ""
Write-Host "Инструкция:" -ForegroundColor Yellow
Write-Host "1. Убедитесь, что все 3 сервера запущены (start-3-servers.ps1)" -ForegroundColor White
Write-Host "2. Запустите клиент вручную:" -ForegroundColor White
Write-Host "   dotnet run --project src/Client/ChatApp.Client.Console" -ForegroundColor Cyan
Write-Host "3. Выберите протокол: 2 (gRPC)" -ForegroundColor White
Write-Host "4. Попробуйте разные URL серверов и смотрите логи" -ForegroundColor White
Write-Host ""
Write-Host "Примеры URL для тестирования:" -ForegroundColor Yellow
Write-Host "  http://localhost:5097  - Server 1" -ForegroundColor Green
Write-Host "  http://localhost:5197  - Server 2" -ForegroundColor Cyan
Write-Host "  http://localhost:5297  - Server 3" -ForegroundColor Yellow
Write-Host ""
Write-Host "В логах серверов вы увидите:" -ForegroundColor Yellow
Write-Host "  [Server Port: localhost:5097] gRPC Register request for username: test1" -ForegroundColor Gray
Write-Host "  [Server Port: localhost:5097] Registration successful for: test1" -ForegroundColor Gray
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Демонстрация EnableMultipleHttp2Connections:" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "С настройкой EnableMultipleHttp2Connections = true клиент может:" -ForegroundColor White
Write-Host "  - Создавать несколько параллельных HTTP/2 соединений" -ForegroundColor Gray
Write-Host "  - Распределять нагрузку между репликами при DNS round-robin" -ForegroundColor Gray
Write-Host "  - Эффективно использовать пул соединений" -ForegroundColor Gray
Write-Host ""
Write-Host "Для полноценного DNS round-robin нужен:" -ForegroundColor Yellow
Write-Host "  - Docker Swarm mode" -ForegroundColor Gray
Write-Host "  - Kubernetes Headless Service" -ForegroundColor Gray
Write-Host "  - Внешний DNS сервер с множественными A-записями" -ForegroundColor Gray
Write-Host ""

Write-Host "Нажмите Enter для продолжения..." -ForegroundColor Green
Read-Host
