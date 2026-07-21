
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/Client/ChatApp.Client.Console/ChatApp.Client.Console.csproj", "Client/ChatApp.Client.Console/"]
COPY ["src/Client/ChatApp.Client.Application/ChatApp.Client.Application.csproj", "Client/ChatApp.Client.Application/"]
COPY ["src/Client/ChatApp.Client.Infrastructure/ChatApp.Client.Infrastructure.csproj", "Client/ChatApp.Client.Infrastructure/"]
COPY ["src/Shared/ChatApp.Contracts/ChatApp.Contracts.csproj", "Shared/ChatApp.Contracts/"]

RUN dotnet restore "Client/ChatApp.Client.Console/ChatApp.Client.Console.csproj"

COPY src/ .


WORKDIR "/src/Client/ChatApp.Client.Console"
RUN dotnet build "ChatApp.Client.Console.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ChatApp.Client.Console.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app


COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "ChatApp.Client.Console.dll"]
