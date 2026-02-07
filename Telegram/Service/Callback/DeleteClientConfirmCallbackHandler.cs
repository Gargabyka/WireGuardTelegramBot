using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using WireGuardTelegramBot.Telegram.Enums;
using WireGuardTelegramBot.Telegram.Models;
using WireGuardTelegramBot.WireGuard.Services.Interface;

namespace WireGuardTelegramBot.Telegram.Service.Callback;

/// <summary>
/// Подтверждённое удаление клиента <see cref="TelegramCallbackEvent.DeleteClientConfirm"/>
/// </summary>
public class DeleteClientConfirmCallbackHandler : TelegramCallbackHandlerBase
{
    private readonly IWireGuardApiService _wireGuardApiService;
    private readonly ITelegramBotClient _telegramBot;

    public DeleteClientConfirmCallbackHandler(ITelegramBotClient telegramBot, IWireGuardApiService wireGuardApiService)
        : base(telegramBot)
    {
        _telegramBot = telegramBot;
        _wireGuardApiService = wireGuardApiService;
    }

    public override TelegramCallbackEvent CallbackEvent => TelegramCallbackEvent.DeleteClientConfirm;

    public override async Task CallbackHandlerAsync(Update update, TelegramRequest request, CancellationToken cancellationToken)
    {
        var (chatId, _, _) = GetCallbackContext(update);

        // Удаляем экран подтверждения, чтобы не плодить сообщения
        await TryDeleteOriginMessageAsync(update, cancellationToken);

        await _wireGuardApiService.DeleteClientAsync(request.Id, cancellationToken);

        await _telegramBot.SendMessage(
            chatId: chatId,
            text: "Клиент удалён.",
            cancellationToken: cancellationToken);

        // Возвращаем в список
        var backToList = BackTo(TelegramCallbackEvent.List, sourceEvent: TelegramCallbackEvent.List);

        // “Мягкий” переход: просто просим пользователя нажать кнопку
        // (либо можно сразу вызвать рендер списка вашим ListTelegramUpdateHandler — если хотите, скажите)
        await _telegramBot.SendMessage(
            chatId: chatId,
            text: "Вернуться к списку клиентов:",
            replyMarkup: new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData("⬅ К списку", Encode(backToList))),
            cancellationToken: cancellationToken);

        await AnswerAsync(update, cancellationToken);
    }
}