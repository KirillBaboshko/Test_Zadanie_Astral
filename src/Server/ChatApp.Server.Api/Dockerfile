FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/Server/ChatApp.Server.Api/ChatApp.Server.Api.csproj", "Server/ChatApp.Server.Api/"]
COPY ["src/Server/ChatApp.Server.Application/ChatApp.Server.Application.csproj", "Server/ChatApp.Server.Application/"]
COPY ["src/Server/ChatApp.Server.Domain/ChatApp.Server.Domain.csproj", "Server/ChatApp.Server.Domain/"]
COPY ["src/Server/ChatApp.Server.Infrastructure/ChatApp.Server.Infrastructure.csproj", "Server/ChatApp.Server.Infrastructure/"]
COPY ["src/Shared/ChatApp.Contracts/ChatApp.Contracts.csproj", "Shared/ChatApp.Contracts/"]

RUN dotnet restore "Server/ChatApp.Server.Api/ChatApp.Server.Api.csproj"

COPY src/ .

WORKDIR "/src/Server/ChatApp.Server.Api"
RUN dotnet build "ChatApp.Server.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ChatApp.Server.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "ChatApp.Server.Api.dll"]
