using Telegram.Bot.Types;
using WireGuardTelegramBot.Telegram.Enums;
using WireGuardTelegramBot.Telegram.Models;

namespace WireGuardTelegramBot.Telegram.Service.Interface;

/// <summary>
/// Обработчик обратных вызовов телеграм
/// </summary>
public interface ITelegramCallbackHandler
{
    /// <summary>
    /// Возвращаемое событие
    /// </summary>
    public TelegramCallbackEvent CallbackEvent { get; }

    /// <summary>
    /// Обработчик возвращаемого события
    /// </summary>
    /// <param name="update">Событие</param>
    /// <param name="request">Запрос</param>
    /// <param name="cancellationToken">Токен отмены</param>
    public Task CallbackHandlerAsync(Update update, TelegramRequest request, CancellationToken cancellationToken);
}