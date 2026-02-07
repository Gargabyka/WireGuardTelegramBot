using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using WireGuardTelegramBot.Telegram.Common.Helper;
using WireGuardTelegramBot.Telegram.Enums;
using WireGuardTelegramBot.Telegram.Models;
using WireGuardTelegramBot.Telegram.Service.Interface;
using WireGuardTelegramBot.Telegram.Store.Interfaces;
using WireGuardTelegramBot.WireGuard.Services.Interface;

namespace WireGuardTelegramBot.Telegram.Service.TextFlows;

public sealed class AddClientNameTextFlowHandler : ITelegramTextFlowHandler
{
    private readonly ITelegramBotClient _bot;
    private readonly IAddClientStateStore _store;
    private readonly IWireGuardApiService _wireGuardApi;

    public AddClientNameTextFlowHandler(
        ITelegramBotClient bot,
        IAddClientStateStore store,
        IWireGuardApiService wireGuardApi)
    {
        _bot = bot;
        _store = store;
        _wireGuardApi = wireGuardApi;
    }

    public async Task<bool> TryHandleAsync(Update update, CancellationToken cancellationToken)
    {
        var message = update.Message;
        if (message?.Text is null)
        {
            return false;
        }

        var chatId = message.Chat.Id;
        var text = message.Text.Trim();

        // Команды не перехватываем
        if (text.StartsWith('/'))
        {
            return false;
        }

        // Этот handler активен только когда store "ждёт имя"
        if (!_store.IsWaitingName(chatId))
        {
            return false;
        }

        var clientName = text;

        if (clientName.Length < 3)
        {
            await _bot.SendMessage(
                chatId: chatId,
                text: "Имя слишком короткое. Введите другое (минимум 3 символа) или нажмите «Отмена».",
                cancellationToken: cancellationToken);
            return true;
        }

        try
        {
            var clients = await _wireGuardApi.GetWireguardClients(cancellationToken);
            if (clients is not null && clients.Any(c => string.Equals(c.Name, clientName, StringComparison.OrdinalIgnoreCase)))
            {
                await _bot.SendMessage(
                    chatId: chatId,
                    text: $"Клиент с именем '{clientName}' уже существует. Введите другое имя или нажмите «Отмена».",
                    cancellationToken: cancellationToken);
                return true;
            }

            await _wireGuardApi.AddClientAsync(clientName, cancellationToken);

            _store.Clear(chatId);

            var toList = new TelegramRequest(Guid.Empty, TelegramCallbackEvent.List, TelegramCallbackEvent.MainMenu);
            var toMenu = new TelegramRequest(Guid.Empty, TelegramCallbackEvent.MainMenu, TelegramCallbackEvent.MainMenu);

            var markup = new InlineKeyboardMarkup([
                [InlineKeyboardButton.WithCallbackData(" Список клиентов", TelegramCallbackDataCodec.Encode(toList))],
                [InlineKeyboardButton.WithCallbackData(" В главное меню", TelegramCallbackDataCodec.Encode(toMenu))]
            ]);

            await _bot.SendMessage(
                chatId: chatId,
                text: $"Клиент '{clientName}' добавлен.",
                replyMarkup: markup,
                cancellationToken: cancellationToken);

            return true;
        }
        catch (Exception ex)
        {
            _store.Clear(chatId);

            await _bot.SendMessage(
                chatId: chatId,
                text: $"Не удалось добавить клиента: {ex.Message}",
                cancellationToken: cancellationToken);

            return true;
        }
    }
}