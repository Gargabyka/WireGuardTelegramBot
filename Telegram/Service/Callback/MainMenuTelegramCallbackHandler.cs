using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using WireGuardTelegramBot.Telegram.Enums;
using WireGuardTelegramBot.Telegram.Models;

namespace WireGuardTelegramBot.Telegram.Service.Callback;

/// <summary>
/// Обработчик для главного меню <see cref="TelegramCallbackEvent.MainMenu"/>
/// </summary>
public class MainMenuTelegramCallbackHandler : TelegramCallbackHandlerBase
{
    private readonly ITelegramBotClient _telegramBot;

    public MainMenuTelegramCallbackHandler(ITelegramBotClient telegramBot)
        : base(telegramBot)
    {
        _telegramBot = telegramBot;
    }

    public override TelegramCallbackEvent CallbackEvent => TelegramCallbackEvent.MainMenu;

    public override async Task CallbackHandlerAsync(Update update, TelegramRequest request, CancellationToken cancellationToken)
    {
        var (chatId, _, _) = GetCallbackContext(update);

        await TryDeleteOriginMessageAsync(update, cancellationToken);

        var listRequest = new TelegramRequest(
            Id: Guid.Empty,
            CallbackEvent: TelegramCallbackEvent.List,
            SourceEvent: TelegramCallbackEvent.MainMenu);

        var addRequest = new TelegramRequest(
            Id: Guid.Empty,
            CallbackEvent: TelegramCallbackEvent.AddClient,
            SourceEvent: TelegramCallbackEvent.MainMenu);

        var markup = new InlineKeyboardMarkup([
            [
                InlineKeyboardButton.WithCallbackData("📋 Список клиентов", Encode(listRequest))
            ],
            [
                InlineKeyboardButton.WithCallbackData("➕ Добавить клиента", Encode(addRequest))
            ]
        ]);

        await _telegramBot.SendMessage(
            chatId: chatId,
            text: "Главное меню:",
            replyMarkup: markup,
            cancellationToken: cancellationToken);

        await AnswerAsync(update, cancellationToken);
    }
}