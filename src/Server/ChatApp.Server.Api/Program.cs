using ChatApp.Server.Api.BackgroundServices;
using ChatApp.Server.Api.GrpcServices;
using ChatApp.Server.Api.MessageBus.Consumers;
using ChatApp.Server.Application.UseCases.Auth;
using ChatApp.Server.Application.UseCases.GetMessages;
using ChatApp.Server.Application.UseCases.GetUserInfo;
using ChatApp.Server.Application.UseCases.GetUsers;
using ChatApp.Server.Application.UseCases.SendMessage;
using ChatApp.Server.Infrastructure;
using ChatApp.Server.Infrastructure.Data;
using FluentValidation;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ProtoBuf.Grpc.Server;
using System.Security.Cryptography;

AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

var rsa = RSA.Create(2048);
var rsaKey = new RsaSecurityKey(rsa);

var builder = WebApplication.CreateBuilder(args);


var httpPort = int.TryParse(Environment.GetEnvironmentVariable("HTTP_PORT"), out var hPort) ? hPort : 5096;
var grpcPort = int.TryParse(Environment.GetEnvironmentVariable("GRPC_PORT"), out var gPort) ? gPort : 5097;

builder.WebHost.ConfigureKestrel(options =>
{
    // HTTP/1.1 для REST API
    options.ListenLocalhost(httpPort, listenOptions =>
    {
        listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1;
    });
    
    // HTTP/2 для gRPC без TLS
    options.ListenLocalhost(grpcPort, listenOptions =>
    {
        listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http2;
    });
});

Console.WriteLine($"Server configured: HTTP port {httpPort}, gRPC port {grpcPort}");

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCodeFirstGrpc();

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddSingleton(rsaKey);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = rsaKey,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddValidatorsFromAssemblyContaining<SendMessageUseCase>();

builder.Services.AddScoped<RegisterUseCase>();
builder.Services.AddScoped<LoginUseCase>();
builder.Services.AddScoped<SendMessageUseCase>();
builder.Services.AddScoped<GetMessagesUseCase>();
builder.Services.AddScoped<GetUsersUseCase>();
builder.Services.AddScoped<GetUserInfoUseCase>();

builder.Services.AddScoped<ChatApp.Server.Application.Services.IOutboxService, ChatApp.Server.Infrastructure.Services.OutboxService>();


builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<MessageSentConsumer>();
    x.AddConsumer<UserRegisteredConsumer>();
    x.AddConsumer<UserLoggedInConsumer>();
    
    x.AddConsumer<RegisterUserCommandConsumer>();
    x.AddConsumer<LoginUserCommandConsumer>();
    x.AddConsumer<SendMessageCommandConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        var rabbitHost = builder.Configuration.GetValue<string>("RabbitMQ:Host") ?? "localhost";
        var rabbitUser = builder.Configuration.GetValue<string>("RabbitMQ:Username") ?? "guest";
        var rabbitPass = builder.Configuration.GetValue<string>("RabbitMQ:Password") ?? "guest";

        cfg.Host(rabbitHost, h =>
        {
            h.Username(rabbitUser);
            h.Password(rabbitPass);
        });

        // Автоматическая настройка endpoints для consumers
        cfg.ConfigureEndpoints(context);
    });
});

Console.WriteLine($"[MassTransit] Configured with RabbitMQ (Events + Commands)");


builder.Services.AddHostedService<MessageCleanupService>();
builder.Services.AddHostedService<OutboxPublisherService>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();
        
        if (pendingMigrations.Any())
        {
            logger.LogInformation("Применение миграций БД: {Count} миграций", pendingMigrations.Count());
            await dbContext.Database.MigrateAsync();
            logger.LogInformation("Миграции успешно применены");
        }
        else
        {
            logger.LogInformation("База данных актуальна");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Ошибка при применении миграций базы данных");
        throw;
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Chat API v1");
        options.RoutePrefix = string.Empty;
    });
}
else
{
    app.UseHttpsRedirection();
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Code-first gRPC сервисы (новый подход)
app.MapGrpcService<CodeFirstAuthService>();
app.MapGrpcService<CodeFirstChatService>();

app.MapGet("/grpc", () => "gRPC endpoints: Code-first (IAuthService, IChatService)");

app.Run();
