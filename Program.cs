using WireGuardTelegramBot.Telegram;
using WireGuardTelegramBot.WireGuard;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddTelegramConfiguration(builder.Configuration);
builder.Services.AddWireguardConfiguration(builder.Configuration);

var app = builder.Build();
app.Run();