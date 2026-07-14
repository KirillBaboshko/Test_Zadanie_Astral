using ChatApp.Server.Application.UseCases.GetMessages;
using ChatApp.Server.Application.UseCases.SendMessage;
using ChatApp.Server.Domain.Repositories;
using ChatApp.Server.Infrastructure.Persistence;

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

builder.Services.AddSingleton<IMessageRepository, InMemoryMessageRepository>();

builder.Services.AddScoped<SendMessageUseCase>();
builder.Services.AddScoped<GetMessagesUseCase>();

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
