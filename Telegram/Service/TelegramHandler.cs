using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using WireGuardTelegramBot.Telegram.Common.Helper;
using WireGuardTelegramBot.Telegram.Models;
using WireGuardTelegramBot.Telegram.Service.Factory;
using WireGuardTelegramBot.Telegram.Service.Interface;

namespace WireGuardTelegramBot.Telegram.Service;

public sealed class TelegramHandler : ITelegramHandler
{
    private readonly ITelegramBotClient _telegramBot;
    private readonly TelegramOptions _telegramOptions;
    private readonly TelegramUpdateFactory _updateFactory;
    private readonly TelegramCallbackFactory _callbackFactory;
    private readonly IReadOnlyList<ITelegramTextFlowHandler> _textFlows;

    public TelegramHandler(
        ITelegramBotClient telegramBot,
        IOptions<TelegramOptions> telegramOptions,
        TelegramUpdateFactory updateFactory,
        TelegramCallbackFactory callbackFactory,
        IEnumerable<ITelegramTextFlowHandler> textFlows)
    {
        _telegramBot = telegramBot;
        _updateFactory = updateFactory;
        _callbackFactory = callbackFactory;
        _telegramOptions = telegramOptions.Value;
        _textFlows = textFlows.ToList();
    }

    public async Task HandleAsync(Update update, CancellationToken cancellationToken)
    {
        switch (update.Type)
        {
            case UpdateType.Message:
                await HandleUpdateAsync(update, cancellationToken);
                break;
            case UpdateType.CallbackQuery:
                await HandleCallbackQueryAsync(update, cancellationToken);
                break;
            default:
                return;
        }
    }

    private async Task HandleUpdateAsync(Update update, CancellationToken cancellationToken)
    {
        var message = update.Message;
        if (message?.Text is null)
        {
            return;
        }

        var chatId = message.Chat.Id;

        if (!_telegramOptions.AdminIds.Contains(chatId))
        {
            await _telegramBot.SendMessage(chatId, "Доступ запрещён.", cancellationToken: cancellationToken);
            return;
        }

        var text = message.Text.Trim();

        // 1) Сначала пробуем пошаговые сценарии (только для обычного текста, не команд)
        if (!text.StartsWith('/'))
        {
            foreach (var flow in _textFlows)
            {
                if (await flow.TryHandleAsync(update, cancellationToken))
                {
                    return;
                }
            }
        }

        // 2) Затем — команды как раньше
        var handler = _updateFactory.CreateHandler(text);
        await handler.UpdateHandlerAsync(update, cancellationToken);
    }

    private async Task HandleCallbackQueryAsync(Update update, CancellationToken cancellationToken)
    {
        var callbackQuery = update.CallbackQuery;

        if (callbackQuery?.Data is null)
        {
            return;
        }

        if (!TelegramCallbackDataCodec.TryDecode(callbackQuery.Data, out var request))
        {
            return;
        }

        var handler = _callbackFactory.CreateHandler(request);
        await handler.CallbackHandlerAsync(update, request, cancellationToken);
    }
}