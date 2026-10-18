using Domain.Interfaces;
using Domain.Interfaces.Repository;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Enums;
using Telegram.Models;
using WireGuard.Services.Interface;

namespace Telegram.Service.Callback;

/// <summary>
/// Включает/выключает peer WireGuard. Обычному пользователю доступно только для своих peer, админу — для любых.
/// </summary>
[UsedImplicitly]
public sealed class TogglePeerEnabledTelegramCallbackHandler : TelegramCallbackHandlerBase
{
    private readonly ITelegramBotClient _bot;
    private readonly IWireGuardApiService _wireGuardApiService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TelegramOptions _telegramOptions;
    private readonly ILogger<TogglePeerEnabledTelegramCallbackHandler> _logger;

    public TogglePeerEnabledTelegramCallbackHandler(
        ITelegramBotClient telegramBot,
        IWireGuardApiService wireGuardApiService,
        IOptions<TelegramOptions> telegramOptions,
        IServiceScopeFactory scopeFactory,
        ILogger<TogglePeerEnabledTelegramCallbackHandler> logger)
        : base(telegramBot)
    {
        _bot = telegramBot;
        _wireGuardApiService = wireGuardApiService;
        _telegramOptions = telegramOptions.Value;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public override TelegramCallbackEvent CallbackEvent => TelegramCallbackEvent.TogglePeerEnabled;

    public override async Task CallbackHandlerAsync(Update update, TelegramRequest request, CancellationToken cancellationToken)
    {
        var (chatId, _, _) = GetCallbackContext(update);

        await TryDeleteOriginMessageAsync(update, cancellationToken);

        var backRequest = new TelegramRequest(
            Id: request.Id,
            CallbackEvent: TelegramCallbackEvent.GetFullInfo,
            SourceEvent: request.SourceEvent,
            Page: request.Page);

        using var scope = _scopeFactory.CreateScope();

        if (!_telegramOptions.AdminIds.Contains(chatId))
        {
            var telegramUserRepo = scope.ServiceProvider.GetRequiredService<ITelegramUserRepository>();
            var wgUserRepo = scope.ServiceProvider.GetRequiredService<IWireGuardUserRepository>();

            var owner = await telegramUserRepo.GetUserByTelegramIdAsync(chatId, cancellationToken);
            var ownsThisPeer = owner is not null
                && (await wgUserRepo.GetConnectionsByTelegramIdAsync(owner.Id, cancellationToken))
                    .Any(c => c.Peer.WireGuardId == request.Id);

            if (!ownsThisPeer)
            {
                await _bot.SendMessage(
                    chatId: chatId,
                    text: "Это соединение вам не принадлежит.",
                    cancellationToken: cancellationToken);

                await AnswerAsync(update, cancellationToken);
                return;
            }
        }

        // 1) Переключение через WireGuard API — если тут упадёт, значит реально ничего не поменялось,
        // и пользователю можно честно сказать "не удалось".
        bool wasEnabled;
        try
        {
            var clients = await _wireGuardApiService.GetWireguardClients(cancellationToken);
            var client = clients?.SingleOrDefault(x => x.Id == request.Id);
            if (client is null)
            {
                await _bot.SendMessage(
                    chatId: chatId,
                    text: "Не удалось найти клиента.",
                    replyMarkup: new InlineKeyboardMarkup(
                        InlineKeyboardButton.WithCallbackData("⬅ Назад", Encode(backRequest))),
                    cancellationToken: cancellationToken);

                await AnswerAsync(update, cancellationToken);
                return;
            }

            wasEnabled = client.Enabled;

            if (wasEnabled)
            {
                await _wireGuardApiService.DisableClientAsync(request.Id, cancellationToken);
            }
            else
            {
                await _wireGuardApiService.EnableClientAsync(request.Id, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Не удалось переключить состояние peer {WireGuardId} через WireGuard API", request.Id);

            await _bot.SendMessage(
                chatId: chatId,
                text: "Не удалось изменить состояние соединения. Попробуйте позже.",
                replyMarkup: new InlineKeyboardMarkup(
                    InlineKeyboardButton.WithCallbackData("⬅ Назад", Encode(backRequest))),
                cancellationToken: cancellationToken);

            await AnswerAsync(update, cancellationToken);
            return;
        }

        // 2) Локальная копия статуса — best-effort. WireGuard API уже применил изменение,
        // поэтому сбой здесь не должен заставлять пользователя думать, что переключение не сработало
        // (иначе повторный тап отменит уже применённое изменение).
        try
        {
            var peerRepo = scope.ServiceProvider.GetRequiredService<IWireGuardPeerRepository>();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var localPeer = await peerRepo.GetByWireGuardIdAsync(request.Id, cancellationToken);
            if (localPeer is not null)
            {
                localPeer.IsEnabled = !wasEnabled;
                await uow.SaveChangesAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Peer {WireGuardId} переключён в WireGuard, но не удалось обновить локальную копию", request.Id);
        }

        var statusText = wasEnabled
            ? "Соединение отключено."
            : "Соединение включено.";

        await _bot.SendMessage(
            chatId: chatId,
            text: statusText,
            replyMarkup: new InlineKeyboardMarkup(
                InlineKeyboardButton.WithCallbackData("⬅ Назад", Encode(backRequest))),
            cancellationToken: cancellationToken);

        await AnswerAsync(update, cancellationToken);
    }
}
