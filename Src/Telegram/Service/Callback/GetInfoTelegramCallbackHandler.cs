using System.Text;
using JetBrains.Annotations;
using QRCoder;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Enums;
using Telegram.Models;
using WireGuard.Services.Interface;

namespace Telegram.Service.Callback;

/// <summary>
/// Обработчик события GetInfo
/// </summary>
[UsedImplicitly]
public class GetInfoTelegramCallbackHandler : TelegramCallbackHandlerBase
{
    private readonly IWireGuardApiService _wireGuardApiService;
    private readonly ITelegramBotClient _telegramBot;

    public GetInfoTelegramCallbackHandler(IWireGuardApiService wireGuardApiService, ITelegramBotClient telegramBot)
        : base(telegramBot)
    {
        _wireGuardApiService = wireGuardApiService;
        _telegramBot = telegramBot;
    }

    public override TelegramCallbackEvent CallbackEvent => TelegramCallbackEvent.GetFullInfo;

    public override async Task CallbackHandlerAsync(
	    Update update,
	    TelegramRequest request,
	    CancellationToken cancellationToken)
    {
	    var (chatId, _, _) = GetCallbackContext(update);

	    var clients = await _wireGuardApiService.GetWireguardClients(cancellationToken);
	    if (clients is null or { Length: 0 })
	    {
		    await _telegramBot.SendMessage(
			    chatId: chatId,
			    text: "Не удалось найти клиента",
			    cancellationToken: cancellationToken);
		    return;
	    }

	    var client = clients.SingleOrDefault(x => x.Id == request.Id);
	    if (client is null)
	    {
		    await _telegramBot.SendMessage(
			    chatId: chatId,
			    text: "Не удалось найти клиента",
			    cancellationToken: cancellationToken);
		    return;
	    }

	    await TryDeleteOriginMessageAsync(update, cancellationToken);

	    var configBytes = await _wireGuardApiService.DownloadConfigAsync(request.Id, cancellationToken);
	    var configText = Encoding.UTF8.GetString(configBytes)
		    .Replace("\r\n", "\n")
		    .Replace("\r", "\n");

	    var qrPng = BuildQrPng(configText);

	    var caption = $"""
	                   {BuildClientInfoText(client)}

	                   Выберите действие:
	                   """;

	    // Назад возвращает туда, откуда пришли (MyConnections или List)
	    var backTarget = request.SourceEvent == TelegramCallbackEvent.MyConnections
		    ? TelegramCallbackEvent.MyConnections
		    : TelegramCallbackEvent.List;

	    var backRequest = new TelegramRequest(
		    Id: Guid.Empty,
		    CallbackEvent: backTarget,
		    SourceEvent: backTarget,
		    Page: request.Page);

	    var downloadRequest = WithEvent(
		    request,
		    TelegramCallbackEvent.DownloadConfig,
		    sourceEvent: TelegramCallbackEvent.GetFullInfo);

	    var deleteAskRequest = WithEvent(
		    request,
		    TelegramCallbackEvent.DeleteClientAskConfirm,
		    sourceEvent: request.SourceEvent);

	    var markup = new InlineKeyboardMarkup([
		    [
			    InlineKeyboardButton.WithCallbackData("⬇ Скачать конфиг", Encode(downloadRequest))
		    ],
		    [
			    InlineKeyboardButton.WithCallbackData("🗑 Удалить клиента", Encode(deleteAskRequest))
		    ],
		    [
			    InlineKeyboardButton.WithCallbackData("⬅ Назад", Encode(backRequest))
		    ]
	    ]);

	    await using var ms = new MemoryStream(qrPng, writable: false);
	    ms.Position = 0;

	    var inputFile = InputFile.FromStream(ms, fileName: "vpn.png");

	    await _telegramBot.SendPhoto(
		    chatId: chatId,
		    photo: inputFile,
		    caption: caption,
		    parseMode: ParseMode.None,
		    replyMarkup: markup,
		    cancellationToken: cancellationToken);

	    await AnswerAsync(update, cancellationToken);
    }

    private static string BuildClientInfoText(dynamic client)
    {
        return $"""
                Имя клиента: {client.Name}
                Адрес: {client.Address}
                Клиент включен: {(client.Enabled ? "да" : "нет")}
                Публичный ключ: {client.PublicKey}
                Дата создания клиента: {client.CreatedAt:MM.dd.yyyy}
                """;
    }

    private static byte[] BuildQrPng(string payload)
    {
        using var gen = new QRCodeGenerator();
        using var data = gen.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
        using var qr = new PngByteQRCode(data);
        return qr.GetGraphic(pixelsPerModule: 8);
    }
}