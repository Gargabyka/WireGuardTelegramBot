using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Enums;
using Telegram.Models;

namespace Telegram.Service.Callback.Admin;

[UsedImplicitly]
public sealed class RefreshConfigAskTelegramCallbackHandler : TelegramAdminCallbackHandlerBase
{
	private readonly ITelegramBotClient _bot;

	public RefreshConfigAskTelegramCallbackHandler(
		ITelegramBotClient telegramBot,
		IOptions<TelegramOptions> telegramOptions,
		IServiceScopeFactory scopeFactory)
		: base(telegramBot, telegramOptions, scopeFactory)
	{
		_bot = telegramBot;
	}

	public override TelegramCallbackEvent CallbackEvent => TelegramCallbackEvent.RefreshConfigAsk;

	public override async Task CallbackHandlerAsync(Update update, TelegramRequest request, CancellationToken cancellationToken)
	{
		var chatId = update.CallbackQuery?.Message?.Chat.Id;
		if (chatId is null)
		{
			return;
		}

		if (!await IsAdminAsync(chatId.Value, cancellationToken))
		{
			return;
		}

		await TryDeleteOriginMessageAsync(update, cancellationToken);

		var soft = new TelegramRequest(Guid.Empty, TelegramCallbackEvent.RefreshConfigSoft, TelegramCallbackEvent.AdminPanel);
		var hard = new TelegramRequest(Guid.Empty, TelegramCallbackEvent.RefreshConfigHard, TelegramCallbackEvent.AdminPanel);
		var back = new TelegramRequest(Guid.Empty, TelegramCallbackEvent.AdminPanel, TelegramCallbackEvent.AdminPanel);

		var markup = new InlineKeyboardMarkup([
			[
				InlineKeyboardButton.WithCallbackData("🟢 Мягкое (wg syncconf)", Encode(soft)),
			],
			[
				InlineKeyboardButton.WithCallbackData("🟠 Жёсткое (wg-quick down/up)", Encode(hard)),
			],
			[
				InlineKeyboardButton.WithCallbackData("⬅ Назад", Encode(back)),
			]
		]);

		await _bot.SendMessage(
			chatId: chatId.Value,
			text: "Как применить конфиг WireGuard?",
			replyMarkup: markup,
			cancellationToken: cancellationToken);

		await AnswerAsync(update, cancellationToken);
	}
}