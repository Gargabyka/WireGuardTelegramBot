using Domain.Interfaces.Repository;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Common.Helper;
using Telegram.Enums;
using Telegram.Models;

namespace Telegram.Service.Callback;

/// <summary>
/// Мои соединения (соединения текущего пользователя по TelegramId=chatId)
/// </summary>
[UsedImplicitly]
public sealed class MyConnectionsTelegramCallbackHandler : TelegramCallbackHandlerBase
{
	private const int PageSize = 9;

	private readonly ITelegramBotClient _bot;
	private readonly IServiceScopeFactory _scopeFactory;

	public MyConnectionsTelegramCallbackHandler(
		ITelegramBotClient telegramBot,
		IServiceScopeFactory scopeFactory)
		: base(telegramBot)
	{
		_bot = telegramBot;
		_scopeFactory = scopeFactory;
	}

	public override TelegramCallbackEvent CallbackEvent => TelegramCallbackEvent.MyConnections;

	public override async Task CallbackHandlerAsync(Update update, TelegramRequest request, CancellationToken cancellationToken)
	{
		var (chatId, _, _) = GetCallbackContext(update);

		await TryDeleteOriginMessageAsync(update, cancellationToken);

		using var scope = _scopeFactory.CreateScope();
		var wgUserRepo = scope.ServiceProvider.GetRequiredService<IWireGuardUserRepository>();
		var telegramUserRepo = scope.ServiceProvider.GetRequiredService<ITelegramUserRepository>();

		var user = await telegramUserRepo.GetUserByTelegramIdAsync(chatId, cancellationToken);
		if (user is null)
		{
			await _bot.SendMessage(
				chatId: chatId,
				text: $"Не удалось найти пользователя: {chatId}",
				cancellationToken: cancellationToken);

			return;
		}

		var connections = await wgUserRepo.GetConnectionsByTelegramIdAsync(user.Id, cancellationToken);

		if (connections.Length == 0)
		{
			var backEmpty = BackTo(TelegramCallbackEvent.MainMenu, sourceEvent: TelegramCallbackEvent.MyConnections);

			await _bot.SendMessage(
				chatId: chatId,
				text: "У вас пока нет соединений.",
				replyMarkup: new InlineKeyboardMarkup(
					InlineKeyboardButton.WithCallbackData("⬅ Назад", Encode(backEmpty))),
				cancellationToken: cancellationToken);

			await AnswerAsync(update, cancellationToken);
			return;
		}

		var ordered = connections
			.OrderByDescending(x => x.Peer.CreatedAt)
			.ToArray();

		var totalPages = (int)Math.Ceiling(ordered.Length / (double)PageSize);

		var page = request.Page;
		if (page < 0)
		{
			page = 0;
		}

		if (page > totalPages - 1)
		{
			page = totalPages - 1;
		}

		var pageConnections = ordered
			.Skip(page * PageSize)
			.Take(PageSize)
			.ToArray();

		var rows = BuildPeerRows(pageConnections, page);

		// Навигация
		var prevPage = Math.Max(0, page - 1);
		var nextPage = Math.Min(totalPages - 1, page + 1);

		var navPrev = new TelegramRequest(
			Id: Guid.Empty,
			CallbackEvent: TelegramCallbackEvent.MyConnections,
			SourceEvent: TelegramCallbackEvent.MainMenu,
			Page: prevPage);

		var navNext = new TelegramRequest(
			Id: Guid.Empty,
			CallbackEvent: TelegramCallbackEvent.MyConnections,
			SourceEvent: TelegramCallbackEvent.MainMenu,
			Page: nextPage);

		var canPrev = page > 0;
		var canNext = page < totalPages - 1;

		rows.Add([
			InlineKeyboardButton.WithCallbackData(canPrev ? "⬅️" : " ", Encode(navPrev)),
			InlineKeyboardButton.WithCallbackData($"{page + 1}/{totalPages}", Encode(new TelegramRequest(Guid.Empty, TelegramCallbackEvent.MyConnections, TelegramCallbackEvent.MainMenu, page))),
			InlineKeyboardButton.WithCallbackData(canNext ? "➡️" : " ", Encode(navNext)),
		]);

		var back = BackTo(TelegramCallbackEvent.MainMenu, sourceEvent: TelegramCallbackEvent.MyConnections);
		rows.Add([InlineKeyboardButton.WithCallbackData("⬅ Назад", Encode(back))]);

		await _bot.SendMessage(
			chatId: chatId,
			text: "Ваши соединения (нажмите на имя для подробностей):",
			replyMarkup: new InlineKeyboardMarkup(rows),
			cancellationToken: cancellationToken);

		await AnswerAsync(update, cancellationToken);
	}

	private static List<InlineKeyboardButton[]> BuildPeerRows(IEnumerable<Domain.Entities.WireGuardUser> connections, int page)
	{
		var buttons = connections
			.Select(c =>
			{
				var peer = c.Peer;
				return InlineKeyboardButton.WithCallbackData(
					text: peer.Name,
					callbackData: TelegramCallbackDataCodec.Encode(
						new TelegramRequest(
							Id: peer.WireGuardId, // Guid из WireGuard
							CallbackEvent: TelegramCallbackEvent.GetFullInfo,
							SourceEvent: TelegramCallbackEvent.MyConnections,
							Page: page)));
			})
			.ToList();

		var rows = new List<InlineKeyboardButton[]>();
		for (var i = 0; i < buttons.Count; i += 3)
		{
			rows.Add(buttons.Skip(i).Take(3).ToArray());
		}

		return rows;
	}
}