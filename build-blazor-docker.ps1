# Скрипт для сборки и запуска Blazor клиента в Docker

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Blazor Client - Docker Build & Run" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Останавливаем старый контейнер если есть
Write-Host "Остановка старого контейнера..." -ForegroundColor Yellow
docker stop chatapp-blazor-client 2>$null
docker rm chatapp-blazor-client 2>$null

# Собираем образ
Write-Host ""
Write-Host "Сборка Docker образа..." -ForegroundColor Yellow
docker build -t chatapp-blazor-client -f src/Client/ChatApp.Client.Blazor/Dockerfile .

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "✗ Ошибка сборки образа" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "✓ Образ успешно собран" -ForegroundColor Green

# Запускаем контейнер
Write-Host ""
Write-Host "Запуск контейнера..." -ForegroundColor Yellow

docker run -d `
    --name chatapp-blazor-client `
    --network chatapp-network `
    -p 8080:80 `
    chatapp-blazor-client

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "✗ Ошибка запуска контейнера" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "✓ Контейнер успешно запущен" -ForegroundColor Green
Write-Host ""
Write-Host "Blazor клиент доступен на:" -ForegroundColor Cyan
Write-Host "  http://localhost:8080" -ForegroundColor White
Write-Host ""
Write-Host "Логи контейнера:" -ForegroundColor Yellow
Write-Host "  docker logs -f chatapp-blazor-client" -ForegroundColor Gray
Write-Host ""
Write-Host "Остановка контейнера:" -ForegroundColor Yellow
Write-Host "  docker stop chatapp-blazor-client" -ForegroundColor Gray
Write-Host ""
