using Domain.Interfaces;
using Domain.Interfaces.Repository;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Enums;
using Telegram.Models;
using WireGuard.Services.Interface;

namespace Telegram.Service.Callback;

/// <summary>
/// Подтверждённое удаление клиента <see cref="TelegramCallbackEvent.DeleteClientConfirm"/>
/// </summary>
[UsedImplicitly]
public class DeleteClientConfirmCallbackHandler : TelegramCallbackHandlerBase
{
    private readonly IWireGuardApiService _wireGuardApiService;
    private readonly ITelegramBotClient _telegramBot;
    private readonly IServiceScopeFactory _scopeFactory;

    public DeleteClientConfirmCallbackHandler(
        ITelegramBotClient telegramBot,
        IWireGuardApiService wireGuardApiService,
        IServiceScopeFactory scopeFactory)
        : base(telegramBot)
    {
        _telegramBot = telegramBot;
        _wireGuardApiService = wireGuardApiService;
        _scopeFactory = scopeFactory;
    }

    public override TelegramCallbackEvent CallbackEvent => TelegramCallbackEvent.DeleteClientConfirm;

    public override async Task CallbackHandlerAsync(Update update, TelegramRequest request, CancellationToken cancellationToken)
    {
        var (chatId, _, _) = GetCallbackContext(update);

        // Удаляем экран подтверждения, чтобы не плодить сообщения
        await TryDeleteOriginMessageAsync(update, cancellationToken);

        // 1) Удаляем в WireGuard
        await _wireGuardApiService.DeleteClientAsync(request.Id, cancellationToken);

        // 2) Удаляем в БД (если запись есть)
        using (var scope = _scopeFactory.CreateScope())
        {
            var peerRepo = scope.ServiceProvider.GetRequiredService<IWireGuardPeerRepository>();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var peer = await peerRepo.GetByWireGuardIdAsync(request.Id, cancellationToken);
            if (peer is not null)
            {
                // Важно: связи WireGuardUsers удалятся каскадно по FK PeerId (у вас DeleteBehavior.Cascade)
                peerRepo.Remove(peer);
                await uow.SaveChangesAsync(cancellationToken);
            }
        }

        await _telegramBot.SendMessage(
            chatId: chatId,
            text: "Клиент удалён.",
            cancellationToken: cancellationToken);

        // Возвращаем туда, откуда пришли (MyConnections или List)
        var backTarget = request.SourceEvent == TelegramCallbackEvent.MyConnections
            ? TelegramCallbackEvent.MyConnections
            : TelegramCallbackEvent.List;

        var backToList = new TelegramRequest(
            Id: Guid.Empty,
            CallbackEvent: backTarget,
            SourceEvent: backTarget,
            Page: request.Page);

        await _telegramBot.SendMessage(
            chatId: chatId,
            text: "Вернуться назад:",
            replyMarkup: new InlineKeyboardMarkup(
                InlineKeyboardButton.WithCallbackData("⬅ Назад", Encode(backToList))),
            cancellationToken: cancellationToken);

        await AnswerAsync(update, cancellationToken);
    }
}