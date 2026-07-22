# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Копируем Shared проекты (зависимости)
COPY src/Shared/ChatApp.Contracts/ChatApp.Contracts.csproj ./Shared/ChatApp.Contracts/
RUN dotnet restore ./Shared/ChatApp.Contracts/ChatApp.Contracts.csproj

# Копируем Blazor проект и восстанавливаем зависимости
COPY src/Client/ChatApp.Client.Blazor/ChatApp.Client.Blazor.csproj ./Client/ChatApp.Client.Blazor/
RUN dotnet restore ./Client/ChatApp.Client.Blazor/ChatApp.Client.Blazor.csproj

# Копируем все исходники
COPY src/Shared/ChatApp.Contracts/ ./Shared/ChatApp.Contracts/
COPY src/Client/ChatApp.Client.Blazor/ ./Client/ChatApp.Client.Blazor/

# Публикуем проект
WORKDIR /src/Client/ChatApp.Client.Blazor
RUN dotnet publish -c Release -o /app/publish

# Stage 2: Runtime with nginx
FROM nginx:alpine

# Копируем статические файлы из wwwroot
COPY --from=build /app/publish/wwwroot /usr/share/nginx/html

# Копируем конфигурацию nginx
COPY nginx.conf /etc/nginx/nginx.conf

# Обновляем appsettings.json для production (API через nginx proxy)
RUN echo '{"ApiBaseUrl":""}' > /usr/share/nginx/html/appsettings.json

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
