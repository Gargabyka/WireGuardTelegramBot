using WireGuardTelegramBot.Telegram;
using WireGuardTelegramBot.WireGuard;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddJsonFile(
        path: "appsettings.secret.json",
        optional: true,
        reloadOnChange: true);
}

builder.Services.AddTelegramConfiguration(builder.Configuration);
builder.Services.AddWireguardConfiguration(builder.Configuration);

var app = builder.Build();
app.Run();