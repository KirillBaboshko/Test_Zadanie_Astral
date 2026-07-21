# Скрипт для остановки RabbitMQ
Write-Host "=== Остановка RabbitMQ ===" -ForegroundColor Cyan
Write-Host ""

docker stop chatapp-rabbitmq
docker rm chatapp-rabbitmq

Write-Host ""
Write-Host "[OK] RabbitMQ остановлен и удалён" -ForegroundColor Green
