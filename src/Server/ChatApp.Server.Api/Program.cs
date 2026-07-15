using ChatApp.Server.Application.UseCases.GetMessages;
using ChatApp.Server.Application.UseCases.GetUserInfo;
using ChatApp.Server.Application.UseCases.GetUsers;
using ChatApp.Server.Application.UseCases.SendMessage;
using ChatApp.Server.Domain.Repositories;
using ChatApp.Server.Infrastructure.Data;
using ChatApp.Server.Infrastructure.Repository;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "Chat API",
        Version = "v1",
        Description = "HTTP Chat API с Clean Architecture"
    });
});


builder.Services.AddDbContextPool<ChatDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IMessageRepository, MessageRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

builder.Services.AddValidatorsFromAssemblyContaining<SendMessageUseCase>();

builder.Services.AddScoped<SendMessageUseCase>();
builder.Services.AddScoped<GetMessagesUseCase>();
builder.Services.AddScoped<GetUsersUseCase>();
builder.Services.AddScoped<GetUserInfoUseCase>();

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

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthorization();
app.MapControllers();

app.Run();
