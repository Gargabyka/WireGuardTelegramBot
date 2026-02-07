namespace Telegram.Models;

/// <summary>
/// Настройки для телеграм бота
/// </summary>
public sealed class TelegramOptions
{
    /// <summary>
    /// Токен бота
    /// </summary>
    public required string Token { get; init; }

    /// <summary>
    /// Идентификаторы админов
    /// </summary>
    public required long[] AdminIds { get; init; }
}