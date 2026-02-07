using Telegram.Bot;
using Telegram.Bot.Types;
using WireGuardTelegramBot.Telegram.Enums;
using WireGuardTelegramBot.Telegram.Models;
using WireGuardTelegramBot.WireGuard.Services.Interface;

namespace WireGuardTelegramBot.Telegram.Service.Callback;

/// <summary>
/// Обработчик для скачивания конфигурации
/// </summary>
public class DownloadConfigTelegramCallbackHandler : TelegramCallbackHandlerBase
{
    private readonly IWireGuardApiService _wireGuardApiService;
    private readonly ITelegramBotClient _telegramBot;

    public DownloadConfigTelegramCallbackHandler(IWireGuardApiService wireGuardApiService, ITelegramBotClient telegramBot)
        : base(telegramBot)
    {
        _wireGuardApiService = wireGuardApiService;
        _telegramBot = telegramBot;
    }

    public override TelegramCallbackEvent CallbackEvent => TelegramCallbackEvent.DownloadConfig;

    public override async Task CallbackHandlerAsync(Update update, TelegramRequest request, CancellationToken cancellationToken)
    {
        var (chatId, _, _) = GetCallbackContext(update);

        // Тут специально НЕ удаляем исходное сообщение (QR-экран) — пусть остаётся,
        // а конфиг просто прилетит отдельным документом.
        var document = await _wireGuardApiService.DownloadConfigAsync(request.Id, cancellationToken);
        await using var ms = new MemoryStream(document, writable: false);
        ms.Position = 0;

        var inputFile = InputFile.FromStream(ms, fileName: "vpn.config");

        await _telegramBot.SendDocument(
            chatId: chatId,
            document: inputFile,
            caption: "Конфиг для WireGuard",
            cancellationToken: cancellationToken);

        await AnswerAsync(update, cancellationToken);
    }
}