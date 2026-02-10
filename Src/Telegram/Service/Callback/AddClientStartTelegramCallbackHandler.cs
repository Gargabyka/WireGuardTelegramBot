using JetBrains.Annotations;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Enums;
using Telegram.Models;
using Telegram.Store.Interfaces;

namespace Telegram.Service.Callback;

/// <summary>
/// Обработчик события AddClientStart <see cref="TelegramCallbackEvent.AddClient"/>
/// </summary>
[UsedImplicitly]
public class AddClientStartTelegramCallbackHandler : TelegramCallbackHandlerBase
{
    private readonly IAddClientStateStore _addClientState;
    private readonly ITelegramBotClient _telegramBot;

    public AddClientStartTelegramCallbackHandler(
        ITelegramBotClient telegramBot,
        IAddClientStateStore addClientState)
        : base(telegramBot)
    {
        _telegramBot = telegramBot;
        _addClientState = addClientState;
    }

    public override TelegramCallbackEvent CallbackEvent => TelegramCallbackEvent.AddClient;

    public override async Task CallbackHandlerAsync(Update update, TelegramRequest request, CancellationToken cancellationToken)
    {
        var (chatId, _, _) = GetCallbackContext(update);

        await TryDeleteOriginMessageAsync(update, cancellationToken);

        _addClientState.Begin(chatId);

        var cancelRequest = new TelegramRequest(
            Id: Guid.Empty,
            CallbackEvent: TelegramCallbackEvent.AddClientCancel,
            SourceEvent: TelegramCallbackEvent.MainMenu,
            Page: 0);

        var markup = new InlineKeyboardMarkup([
            [
                InlineKeyboardButton.WithCallbackData("❌ Отмена", Encode(cancelRequest))
            ]
        ]);

        await _telegramBot.SendMessage(
            chatId: chatId,
            text: """
                  Введите имя нового клиента одним сообщением.

                  Пример: iphone_ivan
                  """,
            replyMarkup: markup,
            cancellationToken: cancellationToken);

        await AnswerAsync(update, cancellationToken);
    }
}