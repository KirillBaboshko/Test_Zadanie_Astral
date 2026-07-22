# Скрипт для запуска Blazor клиента в режиме разработки

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Blazor WebAssembly Client - Dev Mode" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Проверяем, запущен ли сервер
Write-Host "Проверка сервера API..." -ForegroundColor Yellow

try {
    $response = Invoke-WebRequest -Uri "http://localhost:5096/api/chat/messages?limit=1" -Method GET -ErrorAction SilentlyContinue
    Write-Host "✓ API сервер работает на http://localhost:5096" -ForegroundColor Green
}
catch {
    Write-Host "⚠ API сервер не найден на http://localhost:5096" -ForegroundColor Red
    Write-Host "  Запустите сервер перед запуском клиента:" -ForegroundColor Yellow
    Write-Host "  cd src/Server/ChatApp.Server.Api" -ForegroundColor Gray
    Write-Host "  dotnet run" -ForegroundColor Gray
    Write-Host ""
    $continue = Read-Host "Продолжить без сервера? (y/n)"
    if ($continue -ne 'y') {
        exit
    }
}

Write-Host ""
Write-Host "Запуск Blazor клиента..." -ForegroundColor Yellow
Write-Host ""

# Переходим в папку проекта и запускаем
Set-Location src/Client/ChatApp.Client.Blazor

Write-Host "Клиент будет доступен на:" -ForegroundColor Green
Write-Host "  https://localhost:5001" -ForegroundColor Cyan
Write-Host "  http://localhost:5000" -ForegroundColor Cyan
Write-Host ""
Write-Host "Нажмите Ctrl+C для остановки" -ForegroundColor Gray
Write-Host ""

dotnet watch
