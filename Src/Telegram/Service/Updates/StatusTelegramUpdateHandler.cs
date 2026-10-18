using Domain.Enums;
using Domain.Interfaces.Repository;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Common.Helper;
using Telegram.Models;
using Telegram.Service.Interface;
using WireGuard.Services.Interface;

namespace Telegram.Service.Updates;

/// <summary>
/// Обработчик команды /status — диагностика для админа: жив ли WireGuard API,
/// сколько всего/включённых/отключённых peer'ов, сколько пользователей бота.
/// </summary>
public sealed class StatusTelegramUpdateHandler : ITelegramUpdateHandler
{
	private readonly ITelegramBotClient _telegramBot;
	private readonly IWireGuardApiService _wireGuardApiService;
	private readonly TelegramOptions _telegramOptions;
	private readonly HostingReminderOptions _hostingReminderOptions;
	private readonly IServiceScopeFactory _scopeFactory;

	public StatusTelegramUpdateHandler(
		ITelegramBotClient telegramBot,
		IWireGuardApiService wireGuardApiService,
		IOptions<TelegramOptions> telegramOptions,
		IOptions<HostingReminderOptions> hostingReminderOptions,
		IServiceScopeFactory scopeFactory)
	{
		_telegramBot = telegramBot;
		_wireGuardApiService = wireGuardApiService;
		_telegramOptions = telegramOptions.Value;
		_hostingReminderOptions = hostingReminderOptions.Value;
		_scopeFactory = scopeFactory;
	}

	public async Task UpdateHandlerAsync(Update update, CancellationToken cancellationToken)
	{
		var chatId = update.Message?.Chat.Id;
		if (chatId is null)
		{
			return;
		}

		if (!await IsAdminAsync(chatId.Value, cancellationToken))
		{
			return;
		}

		using var scope = _scopeFactory.CreateScope();
		var telegramUserRepo = scope.ServiceProvider.GetRequiredService<ITelegramUserRepository>();
		var hostingReminderStateRepo = scope.ServiceProvider.GetRequiredService<IHostingReminderStateRepository>();

		var usersCount = (await telegramUserRepo.GetUsersAsync(cancellationToken)).Length;

		string wireGuardStatusLine;
		try
		{
			var clients = await _wireGuardApiService.GetWireguardClients(cancellationToken) ?? [];
			var enabledCount = clients.Count(x => x.Enabled);

			wireGuardStatusLine = $"""
			                       WireGuard API: ✅ доступен
			                       Всего peer'ов: {clients.Length} (включено: {enabledCount}, отключено: {clients.Length - enabledCount})
			                       """;
		}
		catch (Exception ex)
		{
			wireGuardStatusLine = $"""
			                       WireGuard API: ❌ недоступен ({ex.Message})
			                       Всего peer'ов: нет данных
			                       """;
		}

		var reminderLine = "Напоминания об оплате: выключены в конфиге";
		if (_hostingReminderOptions.Enabled)
		{
			var reminderState = await hostingReminderStateRepo.GetOrCreateAsync(cancellationToken);
			var nextDue = HostingReminderScheduler.GetDueDate(
				_hostingReminderOptions.LastPaymentDate,
				_hostingReminderOptions.PeriodMonths,
				reminderState.RemindersSentCount + 1);

			reminderLine = $"Напоминания об оплате: следующее {nextDue:dd.MM.yyyy}";
		}

		var text = $"""
		            📊 Статус системы

		            {wireGuardStatusLine}
		            Пользователей бота: {usersCount}
		            {reminderLine}
		            """;

		await _telegramBot.SendMessage(
			chatId: chatId.Value,
			text: text,
			cancellationToken: cancellationToken);
	}

	private async Task<bool> IsAdminAsync(long telegramId, CancellationToken cancellationToken)
	{
		if (_telegramOptions.AdminIds.Contains(telegramId))
		{
			return true;
		}

		using var scope = _scopeFactory.CreateScope();
		var repo = scope.ServiceProvider.GetService<ITelegramUserRepository>();
		if (repo is null)
		{
			return false;
		}

		var user = await repo.GetUserByTelegramIdAsync(telegramId, cancellationToken);
		return user?.Role == UserRole.Administrator;
	}
}
