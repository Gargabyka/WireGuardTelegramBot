using Application.Services.Interfaces;
using Domain.Models;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Service.Interface;
using Telegram.Store.Interfaces;

namespace Telegram.Service.TextFlows;

public sealed class AddUserByContactTextFlowHandler : ITelegramTextFlowHandler
{
    private readonly ITelegramBotClient _bot;
    private readonly IServiceScopeFactory _scopeFactory;

    public AddUserByContactTextFlowHandler(ITelegramBotClient bot, IServiceScopeFactory scopeFactory)
    {
        _bot = bot;
        _scopeFactory = scopeFactory;
    }

    public async Task<bool> TryHandleAsync(Update update, CancellationToken cancellationToken)
    {
        var message = update.Message;
        if (message is null)
		{
			return false;
		}

        var chatId = message.Chat.Id;

        using var scope = _scopeFactory.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IAddUserStateStore>();
        if (!store.IsWaitingContact(chatId))
		{
			return false; // не наш сценарий
		}

		// 1) Отмена текстом
        var text = message.Text?.Trim();
        if (!string.IsNullOrWhiteSpace(text) && text.Equals("Отмена", StringComparison.OrdinalIgnoreCase))
        {
            store.Clear(chatId);
            await _bot.SendMessage(
                chatId: chatId,
                text: "Ок, отменено.",
                replyMarkup: new ReplyKeyboardRemove(),
                cancellationToken: cancellationToken);
            return true;
        }

        // 2) Ждём именно контакт
        var contact = message.Contact;
        if (contact is null)
        {
            await _bot.SendMessage(
                chatId: chatId,
                text: "Я ожидаю контакт. Нажмите «📇 Отправить контакт» или «Отмена».",
                cancellationToken: cancellationToken);
            return true;
        }

        var telegramUserId = contact.UserId;
        if (telegramUserId is null or <= 0)
        {
            await _bot.SendMessage(
                chatId: chatId,
                text: "Не получилось прочитать TelegramId из контакта. Отправьте контакт именно через кнопку «📇 Отправить контакт».",
                cancellationToken: cancellationToken);
            return true;
        }

        var userService = scope.ServiceProvider.GetService<TelegramUserService>();
        if (userService is null)
        {
            await _bot.SendMessage(chatId, "Ошибка: TelegramUserService не зарегистрирован.", cancellationToken: cancellationToken);
            return true;
        }

        var ok = await userService.TryAddUserAsync(
            new AddUserModel(
                TelegramId: telegramUserId.Value,
                FirstName: contact.FirstName,
                LastName: contact.LastName,
                UserName: null),
            cancellationToken);

        store.Clear(chatId);

        await _bot.SendMessage(
            chatId: chatId,
            text: ok ? "Пользователь добавлен." : "Пользователь уже существует в системе.",
            replyMarkup: new ReplyKeyboardRemove(),
            cancellationToken: cancellationToken);

        return true;
    }
}