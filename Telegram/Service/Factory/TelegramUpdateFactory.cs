using WireGuardTelegramBot.Telegram.Service.Interface;
using WireGuardTelegramBot.Telegram.Service.Updates;

namespace WireGuardTelegramBot.Telegram.Service.Factory;

/// <summary>
/// Класс создания фабрики по работе с обновлениями
/// </summary>
public class TelegramUpdateFactory
{
    private readonly IServiceProvider _serviceProvider;

    public TelegramUpdateFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public ITelegramUpdateHandler CreateHandler(string command)
    {
        return command switch
        {
            "/start" => _serviceProvider.GetRequiredService<StartTelegramUpdateHandler>(),
            _ => throw new ArgumentException()
        };
    }
}