using WireGuardTelegramBot.Telegram.Common.Attributes;

namespace WireGuardTelegramBot.Telegram.Enums;

/// <summary>
/// События, возникающие при callback от Telegram
/// </summary>
public enum TelegramCallbackEvent
{
    /// <summary>
    /// Получить информацию о клиенте
    /// </summary>
    [CallbackCode("gi")]
    GetFullInfo = 0,

    /// <summary>
    /// Получить список клиентов
    /// </summary>
    [CallbackCode("ls")]
    List = 1,

    /// <summary>
    /// Скачать конфиг=
    /// </summary>
    [CallbackCode("dc")]
    DownloadConfig = 2,

    /// <summary>
    /// Показать подтверждение удаления клиента
    /// </summary>
    [CallbackCode("dx")]
    DeleteClientAskConfirm = 3,

    /// <summary>
    /// Подтвердить удаление клиента
    /// </summary>
    [CallbackCode("dd")]
    DeleteClientConfirm = 4,

    /// <summary>
    /// Главное меню
    /// </summary>
    [CallbackCode("mm")]
    MainMenu = 10,

    /// <summary>
    /// Добавить клиента
    /// </summary>
    [CallbackCode("ac")]
    AddClient = 11,

    /// <summary>
    /// Отмена добавления клиента
    /// </summary>
    [CallbackCode("ax")]
    AddClientCancel = 12,
}