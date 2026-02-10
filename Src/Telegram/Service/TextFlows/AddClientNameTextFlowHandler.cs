using Domain.Entities;
using Domain.Interfaces;
using Domain.Interfaces.Repository;
using Domain.Models;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Common.Helper;
using Telegram.Enums;
using Telegram.Models;
using Telegram.Service.Interface;
using Telegram.Store.Interfaces;
using WireGuard.Services.Interface;

namespace Telegram.Service.TextFlows;

public sealed class AddClientNameTextFlowHandler : ITelegramTextFlowHandler
{
    private readonly ITelegramBotClient _bot;
    private readonly IAddClientStateStore _store;
    private readonly IWireGuardApiService _wireGuardApi;
    private readonly IServiceScopeFactory _scopeFactory;

    public AddClientNameTextFlowHandler(
        ITelegramBotClient bot,
        IAddClientStateStore store,
        IWireGuardApiService wireGuardApi,
        IServiceScopeFactory scopeFactory)
    {
        _bot = bot;
        _store = store;
        _wireGuardApi = wireGuardApi;
        _scopeFactory = scopeFactory;
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
            // Проверка дубликатов в WireGuard
            var clients = await _wireGuardApi.GetWireguardClients(cancellationToken);
            if (clients is not null && clients.Any(c => string.Equals(c.Name, clientName, StringComparison.OrdinalIgnoreCase)))
            {
                await _bot.SendMessage(
                    chatId: chatId,
                    text: $"Клиент с именем '{clientName}' уже существует. Введите другое имя или нажмите «Отмена».",
                    cancellationToken: cancellationToken);
                return true;
            }

            // Создаём в WireGuard
            await _wireGuardApi.AddClientAsync(clientName, cancellationToken);

            // Сразу подтягиваем созданного клиента (чтобы получить Guid Id и остальные поля)
            var updatedClients = await _wireGuardApi.GetWireguardClients(cancellationToken) ?? [];
            var createdClient = updatedClients.FirstOrDefault(c => string.Equals(c.Name, clientName, StringComparison.OrdinalIgnoreCase));
            if (createdClient is null)
            {
                _store.Clear(chatId);

                await _bot.SendMessage(
                    chatId: chatId,
                    text: "Клиент создан, но не удалось получить его данные. Попробуйте позже или попросите администратора обновить соединения.",
                    cancellationToken: cancellationToken);

                return true;
            }

            // Сохраняем/обновляем peer в БД и создаём связь WireGuardUsers (клиент принадлежит создателю)
            using (var scope = _scopeFactory.CreateScope())
            {
                var peerRepo = scope.ServiceProvider.GetRequiredService<IWireGuardPeerRepository>();
                var wireGuardUserRepo = scope.ServiceProvider.GetRequiredService<IWireGuardUserRepository>();
                var telegramUserRepository = scope.ServiceProvider.GetRequiredService<ITelegramUserRepository>();
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                var domainClient = new WireguardClient(
                    Name: createdClient.Name,
                    Enabled: createdClient.Enabled,
                    Address: createdClient.Address,
                    PublicKey: createdClient.PublicKey,
                    CreatedAt: DateOnly.FromDateTime(createdClient.CreatedAt.UtcDateTime));

                var peerEntity = await peerRepo.GetByWireGuardIdAsync(createdClient.Id, cancellationToken);
                if (peerEntity is null)
                {
                    peerEntity = new WireGuardPeer
                    {
                        WireGuardId = createdClient.Id,
                        Name = domainClient.Name,
                        IsEnabled = domainClient.Enabled,
                        Address = domainClient.Address,
                        PublicKey = domainClient.PublicKey,
                        CreatedAt = domainClient.CreatedAt,
                    };

                    peerRepo.Add(peerEntity);
                }
                else
                {
                    // На всякий случай фиксируем WireGuardId, если запись была заведена криво
                    peerEntity.WireGuardId = createdClient.Id;
                    peerEntity.Update(domainClient);
                }

                await uow.SaveChangesAsync(cancellationToken);

                var user = await telegramUserRepository.GetUserByTelegramIdAsync(chatId, cancellationToken);
                if (user is null)
                {
	                await _bot.SendMessage(
		                chatId: chatId,
		                text: $"Не удалось найти пользователя: {chatId}",
		                cancellationToken: cancellationToken);

	                return true;
                }

                var exists = await wireGuardUserRepo.ExistsAsync(user.Id, peerEntity.Id, cancellationToken);
                if (!exists)
                {
                    wireGuardUserRepo.Add(new WireGuardUser
                    {
                        TelegramUserId = user.Id,
                        PeerId = peerEntity.Id,
                    });

                    await uow.SaveChangesAsync(cancellationToken);
                }
            }

            _store.Clear(chatId);

            var toMyConnections = new TelegramRequest(Guid.Empty, TelegramCallbackEvent.MyConnections, TelegramCallbackEvent.MainMenu);
            var toMenu = new TelegramRequest(Guid.Empty, TelegramCallbackEvent.MainMenu, TelegramCallbackEvent.MainMenu);

            var markup = new InlineKeyboardMarkup([
                [InlineKeyboardButton.WithCallbackData("🔌 Список соединений", TelegramCallbackDataCodec.Encode(toMyConnections))],
                [InlineKeyboardButton.WithCallbackData("⬅ В главное меню", TelegramCallbackDataCodec.Encode(toMenu))]
            ]);

            await _bot.SendMessage(
                chatId: chatId,
                text: $"Клиент '{clientName}' добавлен и привязан к вашему аккаунту.",
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