using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using WireGuardTelegramBot.Telegram.Common.Helper;
using WireGuardTelegramBot.Telegram.Enums;
using WireGuardTelegramBot.Telegram.Models;
using WireGuardTelegramBot.Telegram.Service.Interface;

namespace WireGuardTelegramBot.Telegram.Service.Updates;

/// <summary>
/// Обработчик обновления команды /start
/// </summary>
public class StartTelegramUpdateHandler : ITelegramUpdateHandler
{
    private readonly ITelegramBotClient _telegramBot;

    public StartTelegramUpdateHandler(ITelegramBotClient telegramBot)
    {
        _telegramBot = telegramBot;
    }

    public async Task UpdateHandlerAsync(Update update, CancellationToken cancellationToken)
    {
        var chatId = update.Message?.Chat.Id;
        if (chatId is null)
        {
            return;
        }

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
                InlineKeyboardButton.WithCallbackData("📋 Список клиентов", TelegramCallbackDataCodec.Encode(listRequest))
            ],
            [
                InlineKeyboardButton.WithCallbackData("➕ Добавить клиента", TelegramCallbackDataCodec.Encode(addRequest))
            ]
        ]);

        await _telegramBot.SendMessage(
            chatId: chatId.Value,
            text: "Главное меню:",
            replyMarkup: markup,
            cancellationToken: cancellationToken);
    }
}