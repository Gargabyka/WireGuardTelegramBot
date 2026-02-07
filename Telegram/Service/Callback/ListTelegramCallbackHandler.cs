using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using WireGuardTelegramBot.Telegram.Common.Helper;
using WireGuardTelegramBot.Telegram.Enums;
using WireGuardTelegramBot.Telegram.Models;
using WireGuardTelegramBot.WireGuard.Models;
using WireGuardTelegramBot.WireGuard.Services.Interface;

namespace WireGuardTelegramBot.Telegram.Service.Callback;

/// <summary>
/// Обработчик события List <see cref="TelegramCallbackEvent.List"/>
/// </summary>
public class ListTelegramCallbackHandler : TelegramCallbackHandlerBase
{
    private const int PageSize = 9;

    private readonly IWireGuardApiService _wireGuardApiService;
    private readonly ITelegramBotClient _telegramBot;

    public ListTelegramCallbackHandler(
        ITelegramBotClient telegramBot,
        IWireGuardApiService wireGuardApiService)
        : base(telegramBot)
    {
        _telegramBot = telegramBot;
        _wireGuardApiService = wireGuardApiService;
    }

    public override TelegramCallbackEvent CallbackEvent => TelegramCallbackEvent.List;

    public override async Task CallbackHandlerAsync(Update update, TelegramRequest request, CancellationToken cancellationToken)
    {
        var (chatId, _, _) = GetCallbackContext(update);

        // Не захламляем чат: удаляем текущий экран (список/карточку/подтверждение)
        await TryDeleteOriginMessageAsync(update, cancellationToken);

        var clients = await _wireGuardApiService.GetWireguardClients(cancellationToken);
        if (clients is null or { Length: 0 })
        {
            var backToMenuEmpty = BackTo(TelegramCallbackEvent.MainMenu, sourceEvent: TelegramCallbackEvent.MainMenu);

            await _telegramBot.SendMessage(
                chatId: chatId,
                text: "Не удалось найти клиентов",
                replyMarkup: new InlineKeyboardMarkup(
                    InlineKeyboardButton.WithCallbackData("⬅ В главное меню", Encode(backToMenuEmpty))),
                cancellationToken: cancellationToken);

            await AnswerAsync(update, cancellationToken);
            return;
        }

        var total = clients.Length;
        var totalPages = (int)Math.Ceiling(total / (double)PageSize);

        var page = request.Page;
        if (page < 0)
        {
            page = 0;
        }

        if (page > totalPages - 1)
        {
            page = totalPages - 1;
        }

        var pageClients = clients
            .OrderBy(c => c.Name)
            .Skip(page * PageSize)
            .Take(PageSize)
            .ToArray();

        var rows = BuildClientRows(pageClients);

        // Навигация
        var prevPage = Math.Max(0, page - 1);
        var nextPage = Math.Min(totalPages - 1, page + 1);

        var navPrev = new TelegramRequest(
            Id: Guid.Empty,
            CallbackEvent: TelegramCallbackEvent.List,
            SourceEvent: TelegramCallbackEvent.MainMenu,
            Page: prevPage);

        var navNext = new TelegramRequest(
            Id: Guid.Empty,
            CallbackEvent: TelegramCallbackEvent.List,
            SourceEvent: TelegramCallbackEvent.MainMenu,
            Page: nextPage);

        var canPrev = page > 0;
        var canNext = page < totalPages - 1;

        rows.Add([
            InlineKeyboardButton.WithCallbackData(canPrev ? "⬅️" : " ", Encode(navPrev)),
            InlineKeyboardButton.WithCallbackData($"{page + 1}/{totalPages}", Encode(new TelegramRequest(Guid.Empty, TelegramCallbackEvent.List, TelegramCallbackEvent.MainMenu, page))),
            InlineKeyboardButton.WithCallbackData(canNext ? "➡️" : " ", Encode(navNext)),
        ]);

        // Назад в главное меню
        var backToMenu = BackTo(TelegramCallbackEvent.MainMenu, sourceEvent: TelegramCallbackEvent.MainMenu);
        rows.Add([
            InlineKeyboardButton.WithCallbackData("⬅ В главное меню", Encode(backToMenu))
        ]);

        await _telegramBot.SendMessage(
            chatId: chatId,
            text: "Выберите клиента — покажу подробную информацию:",
            replyMarkup: new InlineKeyboardMarkup(rows),
            cancellationToken: cancellationToken);

        await AnswerAsync(update, cancellationToken);
    }

    private static List<InlineKeyboardButton[]> BuildClientRows(IEnumerable<WireguardClients> clients)
    {
        var buttons = clients
            .Select(client => InlineKeyboardButton.WithCallbackData(
                text: client.Name,
                callbackData: TelegramCallbackDataCodec.Encode(
                    new TelegramRequest(
                        Id: client.Id,
                        CallbackEvent: TelegramCallbackEvent.GetFullInfo,
                        SourceEvent: TelegramCallbackEvent.List))))
            .ToList();

        var rows = new List<InlineKeyboardButton[]>();
        for (var i = 0; i < buttons.Count; i += 3)
        {
            rows.Add(buttons.Skip(i).Take(3).ToArray());
        }

        return rows;
    }
}