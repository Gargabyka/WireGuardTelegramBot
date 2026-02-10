using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Enums;
using Telegram.Models;

namespace Telegram.Service.Callback.Admin;

[UsedImplicitly]
public sealed class RefreshConfigHardTelegramCallbackHandler : RefreshConfigApplyTelegramCallbackHandler
{
	public RefreshConfigHardTelegramCallbackHandler(
		ITelegramBotClient telegramBot,
		IOptions<TelegramOptions> telegramOptions,
		IServiceScopeFactory scopeFactory)
		: base(telegramBot, telegramOptions, scopeFactory)
	{
	}

	public override TelegramCallbackEvent CallbackEvent => TelegramCallbackEvent.RefreshConfigHard;

	public override async Task CallbackHandlerAsync(Update update, TelegramRequest request, CancellationToken cancellationToken)
	{
		await ApplyAsync(update, request, soft: false, cancellationToken);
	}
}