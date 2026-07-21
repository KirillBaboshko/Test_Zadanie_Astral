# Скрипт для запуска RabbitMQ через Docker
Write-Host "=== Запуск RabbitMQ ===" -ForegroundColor Cyan
Write-Host ""

# Проверка наличия Docker
try {
    docker --version | Out-Null
    Write-Host "[OK] Docker найден" -ForegroundColor Green
} catch {
    Write-Host "[ОШИБКА] Docker не найден. Установите Docker Desktop." -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Запуск RabbitMQ контейнера..." -ForegroundColor Yellow

# Запуск RabbitMQ с management plugin
docker run -d `
    --name chatapp-rabbitmq `
    -p 5672:5672 `
    -p 15672:15672 `
    -e RABBITMQ_DEFAULT_USER=guest `
    -e RABBITMQ_DEFAULT_PASS=guest `
    rabbitmq:3.13-management-alpine

Write-Host ""
Write-Host "=== RabbitMQ запущен ===" -ForegroundColor Green
Write-Host ""
Write-Host "AMQP порт: 5672" -ForegroundColor Cyan
Write-Host "Management UI: http://localhost:15672" -ForegroundColor Cyan
Write-Host "Логин: guest" -ForegroundColor Cyan
Write-Host "Пароль: guest" -ForegroundColor Cyan
Write-Host ""
Write-Host "Для остановки используйте: docker stop chatapp-rabbitmq" -ForegroundColor Yellow
Write-Host "Для удаления используйте: docker rm chatapp-rabbitmq" -ForegroundColor Yellow
