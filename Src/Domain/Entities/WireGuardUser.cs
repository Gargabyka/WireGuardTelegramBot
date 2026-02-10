using Domain.Interfaces;

namespace Domain.Entities;

/// <summary>
/// Связь пользователя и peer
/// </summary>
public class WireGuardUser : IEntity
{
	/// <summary>
	/// Идентификатор
	/// </summary>
	public int Id { get; init; }

	/// <summary>
	/// Идентификатор Telegram
	/// </summary>
	public int TelegramUserId { get; set; }

	/// <summary>
	/// Идентификатор WireGuard
	/// </summary>
	public int PeerId { get; set; }

	public virtual TelegramUser Telegram { get; set; }

	public virtual WireGuardPeer Peer { get; set; }
}