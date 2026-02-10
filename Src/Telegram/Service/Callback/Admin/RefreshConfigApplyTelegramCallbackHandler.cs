using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Enums;
using Telegram.Models;
using WireGuard.Services.Interface;

namespace Telegram.Service.Callback.Admin;

[UsedImplicitly]
public class RefreshConfigApplyTelegramCallbackHandler : TelegramAdminCallbackHandlerBase
{
	private readonly ITelegramBotClient _bot;
	private readonly IServiceScopeFactory _scopeFactory;

	public RefreshConfigApplyTelegramCallbackHandler(
		ITelegramBotClient telegramBot,
		IOptions<TelegramOptions> telegramOptions,
		IServiceScopeFactory scopeFactory)
		: base(telegramBot, telegramOptions, scopeFactory)
	{
		_bot = telegramBot;
		_scopeFactory = scopeFactory;
	}

	public override TelegramCallbackEvent CallbackEvent => TelegramCallbackEvent.RefreshConfigSoft;

	public override async Task CallbackHandlerAsync(Update update, TelegramRequest request, CancellationToken cancellationToken)
	{
		await ApplyAsync(update, request, soft: true, cancellationToken);
	}

	protected async Task ApplyAsync(Update update, TelegramRequest request, bool soft, CancellationToken cancellationToken)
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

		using var scope = _scopeFactory.CreateScope();
		try
		{
			var wg = scope.ServiceProvider.GetRequiredService<IWireguardService>();

			if (soft)
			{
				await wg.ApplyConfigWithSyncConfAsync(cancellationToken);
			}
			else
			{
				await wg.ApplyConfigWithDownUpAsync(cancellationToken);
			}

			var back = new TelegramRequest(Guid.Empty, TelegramCallbackEvent.AdminPanel, TelegramCallbackEvent.AdminPanel);

			await _bot.SendMessage(
				chatId: chatId.Value,
				text: (soft ? "Конфиг применён (мягко: wg syncconf)." : "Конфиг применён (жёстко: wg-quick down/up).")
				      + $"\nСервис: {wg.GetType().Name}",
				replyMarkup: new InlineKeyboardMarkup(
					InlineKeyboardButton.WithCallbackData("⬅ В админ панель", Encode(back))),
				cancellationToken: cancellationToken);
		}
		catch (Exception ex)
		{
			var back = new TelegramRequest(Guid.Empty, TelegramCallbackEvent.AdminPanel, TelegramCallbackEvent.AdminPanel);

			await _bot.SendMessage(
				chatId: chatId.Value,
				text: $"Не удалось применить конфиг.\nСервис: {scope.ServiceProvider.GetRequiredService<IWireguardService>().GetType().Name}\nПричина: {ex.Message}",
				replyMarkup: new InlineKeyboardMarkup(
					InlineKeyboardButton.WithCallbackData("⬅ В админ панель", Encode(back))),
				cancellationToken: cancellationToken);
		}

		await AnswerAsync(update, cancellationToken);
	}
}