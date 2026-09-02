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


var httpPort = ResolvePort("HTTP_PORT", "ASPNETCORE_HTTP_PORTS", 5096);
var grpcPort = ResolvePort("GRPC_PORT", "ASPNETCORE_GRPC_PORTS", 5097);

builder.WebHost.ConfigureKestrel(options =>
{
    // ListenAnyIP нужен для Docker: порт-маппинг приходит не на 127.0.0.1
    options.ListenAnyIP(httpPort, listenOptions =>
    {
        listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1;
    });

    options.ListenAnyIP(grpcPort, listenOptions =>
    {
        listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http2;
    });
});

static int ResolvePort(string primaryEnvVar, string fallbackEnvVar, int defaultPort)
{
    if (int.TryParse(Environment.GetEnvironmentVariable(primaryEnvVar), out var primaryPort))
        return primaryPort;

    if (int.TryParse(Environment.GetEnvironmentVariable(fallbackEnvVar), out var fallbackPort))
        return fallbackPort;

    return defaultPort;
}

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

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<ChatApp.Server.Application.Commands.SendMessage.SendMessageCommand>();
    
    cfg.AddOpenBehavior(typeof(ChatApp.Server.Application.Behaviors.LoggingBehavior<,>));
    cfg.AddOpenBehavior(typeof(ChatApp.Server.Application.Behaviors.UnitOfWorkBehavior<,>));
});

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

app.MapGrpcService<CodeFirstAuthService>();
app.MapGrpcService<CodeFirstChatService>();

app.MapGet("/grpc", () => "gRPC endpoints: Code-first (IAuthService, IChatService)");

app.Run();
