using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ChatApp.Client.Blazor;
using ChatApp.Client.Blazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Настройка HttpClient для подключения к API серверу
// В production это будет настроено через nginx proxy
var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5096";
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(apiBaseUrl) });

// Регистрация сервисов
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ChatService>();

// Регистрация ViewModels
builder.Services.AddTransient<ChatApp.Client.Blazor.ViewModels.AuthViewModel>();
builder.Services.AddTransient<ChatApp.Client.Blazor.ViewModels.ChatViewModel>();

await builder.Build().RunAsync();
