# 🐳 Docker deployment для ChatApp

Этот проект содержит полную Docker инфраструктуру для развёртывания чат-приложения.

## ✅ Статус запуска

**Проект успешно запущен в Docker!**

- ✅ PostgreSQL 16 работает на порту 5432
- ✅ Server API работает на порту 5096 (http://localhost:5096)
- ✅ Миграции БД применены автоматически
- ✅ JWT аутентификация функционирует
- ✅ MessageCleanupService запущен и работает
- ✅ Все API endpoints протестированы и работают

**Примеры успешных операций:**
```
✓ Регистрация: POST /api/auth/register
✓ Вход: POST /api/auth/login  
✓ Отправка сообщения: POST /api/chat/messages (с JWT)
✓ Получение сообщений: GET /api/chat/messages
```

Для быстрого тестирования используйте: `.\test-api.ps1`

## 📦 Состав

### Контейнеры:
1. **postgres** - PostgreSQL 16 база данных
2. **server** - ASP.NET Core Web API (порт 5096)
3. **client** - .NET Console приложение (опционально)

### Docker образы:
- `chatapp-server` - собирается из `src/Server/ChatApp.Server.Api/Dockerfile`
- `chatapp-client` - собирается из `src/Client/ChatApp.Client.Console/Dockerfile`

## 🚀 Быстрый старт

### 1. Запуск сервера и базы данных

```bash
# Сборка и запуск
docker-compose up -d

# Просмотр логов
docker-compose logs -f server

# Проверка статуса
docker-compose ps
```

API будет доступен по адресу: **http://localhost:5096**

### 2. Запуск с клиентом

```bash
# Запуск всех сервисов включая клиент
docker-compose --profile client up -d

# Подключение к консоли клиента
docker attach chatapp-client
```

## 📋 Команды управления

### Запуск

```bash
# Запуск в фоне
docker-compose up -d

# Запуск с просмотром логов
docker-compose up

# Запуск только определённых сервисов
docker-compose up postgres server
```

### Остановка

```bash
# Остановка всех контейнеров
docker-compose down

# Остановка с удалением volumes (очистка БД)
docker-compose down -v

# Остановка с удалением образов
docker-compose down --rmi all
```

### Пересборка

```bash
# Пересборка образов
docker-compose build

# Пересборка без кеша
docker-compose build --no-cache

# Пересборка и запуск
docker-compose up --build
```

### Логи

```bash
# Все логи
docker-compose logs

# Логи с follow (live)
docker-compose logs -f

# Логи конкретного сервиса
docker-compose logs -f server
docker-compose logs -f postgres

# Последние 100 строк
docker-compose logs --tail=100 server
```

## 🔧 Конфигурация

### Переменные окружения

Можно переопределить через `.env` файл или в `docker-compose.yml`:

**Server:**
```env
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__DefaultConnection=Host=postgres;Port=5432;Database=chatapp;Username=postgres;Password=postgres
Jwt__Issuer=ChatApp
Jwt__Audience=ChatApp.Client
```

**PostgreSQL:**
```env
POSTGRES_USER=postgres
POSTGRES_PASSWORD=postgres
POSTGRES_DB=chatapp
```

### Порты

| Сервис | Контейнер | Хост | Описание |
|--------|-----------|------|----------|
| server | 8080 | 5096 | HTTP API |
| postgres | 5432 | 5432 | PostgreSQL |

Изменить можно в `docker-compose.yml`:
```yaml
ports:
  - "5096:8080"  # хост:контейнер
```

## 🗄️ База данных

### Инициализация

База данных автоматически инициализируется при первом запуске из файла `database-setup.sql`.

### Очистка данных

```bash
# Выполнить SQL скрипт в контейнере
docker exec -i chatapp-postgres psql -U postgres -d chatapp < clear-data.sql
```

### Подключение к БД

```bash
# Через psql в контейнере
docker exec -it chatapp-postgres psql -U postgres -d chatapp

# Или с хоста (если установлен psql)
psql -h localhost -p 5432 -U postgres -d chatapp
```

### Бэкап и восстановление

```bash
# Создать дамп
docker exec chatapp-postgres pg_dump -U postgres chatapp > backup.sql

# Восстановить из дампа
docker exec -i chatapp-postgres psql -U postgres chatapp < backup.sql
```

## 🐞 Отладка

### Проверка здоровья контейнеров

```bash
# Статус всех сервисов
docker-compose ps

# Проверка healthcheck
docker inspect chatapp-postgres --format='{{.State.Health.Status}}'
```

### Вход в контейнер

```bash
# Shell в server контейнере
docker exec -it chatapp-server /bin/bash

# Shell в postgres контейнере
docker exec -it chatapp-postgres /bin/bash

# Интерактивная консоль клиента
docker attach chatapp-client
```

### Просмотр ресурсов

```bash
# Использование ресурсов
docker stats chatapp-server chatapp-postgres

# Размер образов
docker images | grep chatapp
```

## 🧪 Тестирование API

### Через curl

```bash
# Health check
curl http://localhost:5096/api/chat/messages

# Регистрация
curl -X POST http://localhost:5096/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"username":"testuser","password":"Test123!"}'

# Логин
curl -X POST http://localhost:5096/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"testuser","password":"Test123!"}'

# Отправка сообщения (замените TOKEN)
curl -X POST http://localhost:5096/api/chat/messages \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer TOKEN" \
  -d '{"content":"Hello from Docker!"}'
```

### Swagger UI

Откройте в браузере: **http://localhost:5096**

## 🔐 Production deployment

### Рекомендации для Production:

1. **Использовать secrets для паролей:**
```yaml
secrets:
  db_password:
    file: ./secrets/db_password.txt
```

2. **Настроить HTTPS:**
```yaml
environment:
  - ASPNETCORE_URLS=https://+:443;http://+:80
  - ASPNETCORE_Kestrel__Certificates__Default__Path=/app/cert.pfx
  - ASPNETCORE_Kestrel__Certificates__Default__Password=${CERT_PASSWORD}
volumes:
  - ./certs:/app/certs:ro
```

3. **Ограничить ресурсы:**
```yaml
deploy:
  resources:
    limits:
      cpus: '1.0'
      memory: 512M
    reservations:
      cpus: '0.5'
      memory: 256M
```

4. **Добавить мониторинг:**
```yaml
  prometheus:
    image: prom/prometheus
  grafana:
    image: grafana/grafana
```

5. **Использовать reverse proxy (nginx/traefik)**

## 📊 Мониторинг

### Логи приложения

```bash
# Все логи сервера
docker-compose logs server

# Фильтр по уровню (в логах приложения)
docker-compose logs server | grep "error"

# Реал-тайм логи
docker-compose logs -f --tail=50 server
```

### MessageCleanupService

Проверка работы фонового сервиса очистки сообщений:

```bash
docker-compose logs server | grep "MessageCleanupService"
```

Ожидаемый вывод:
```
server  | info: ChatApp.Server.Api.BackgroundServices.MessageCleanupService[0]
server  |       MessageCleanupService запущен. Интервал проверки: 1 мин...
```

## 🛠️ Troubleshooting

### Проблема: Порт уже занят

```bash
# Найти процесс на порту
netstat -ano | findstr :5096  # Windows
lsof -i :5096                 # Linux/Mac

# Изменить порт в docker-compose.yml
ports:
  - "5097:8080"
```

### Проблема: База не инициализируется

```bash
# Удалить volume и пересоздать
docker-compose down -v
docker-compose up -d
```

### Проблема: Сервер не подключается к БД

```bash
# Проверить сеть
docker network inspect chatapp-network

# Проверить DNS
docker exec chatapp-server ping postgres
```

### Проблема: Старый образ

```bash
# Полная очистка и пересборка
docker-compose down --rmi all -v
docker-compose build --no-cache
docker-compose up -d
```

## 📦 Volumes

### Данные PostgreSQL

Данные хранятся в named volume `chatapp-postgres-data`:

```bash
# Просмотр volumes
docker volume ls | grep chatapp

# Информация о volume
docker volume inspect chatapp-postgres-data

# Удаление volume (потеря всех данных!)
docker volume rm chatapp-postgres-data
```

## 🌐 Сетевое взаимодействие

Контейнеры общаются через bridge network `chatapp-network`:

- `postgres` доступен по имени хоста `postgres` внутри сети
- `server` доступен по имени хоста `server` внутри сети
- Внешний доступ через проброшенные порты

## 📝 Примеры использования

### Scenario 1: Разработка локально

```bash
# Запустить только БД
docker-compose up -d postgres

# Разрабатывать и запускать API локально
dotnet run --project src/Server/ChatApp.Server.Api
```

### Scenario 2: Полное развёртывание

```bash
# Всё в Docker
docker-compose up -d

# Проверить что всё работает
curl http://localhost:5096/api/chat/messages
```

### Scenario 3: С клиентом

```bash
# Запуск с клиентом
docker-compose --profile client up

# В другом терминале подключиться к клиенту
docker attach chatapp-client
```

## 🔄 CI/CD Integration

### GitHub Actions пример

```yaml
- name: Build and push Docker images
  run: |
    docker build -t myregistry/chatapp-server:${{ github.sha }} -f src/Server/ChatApp.Server.Api/Dockerfile .
    docker push myregistry/chatapp-server:${{ github.sha }}
```

### Deploy команды

```bash
# Pull и restart на production сервере
docker-compose pull
docker-compose up -d --no-deps --build server
```

---

**Готово!** Теперь ваше приложение полностью контейнеризовано и готово к развёртыванию. 🚀
