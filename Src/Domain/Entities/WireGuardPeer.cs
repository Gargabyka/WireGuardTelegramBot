using Domain.Interfaces;
using Domain.Models;

namespace Domain.Entities;

/// <summary>
/// Соединения WireGuard
/// </summary>
public class WireGuardPeer : IEntity
{
	/// <summary>
	/// Идентификатор
	/// </summary>
	public int Id { get; init; }

	/// <summary>
	/// Идентификатор WireGuard
	/// </summary>
	public Guid WireGuardId { get; set; }

	/// <summary>
	/// Имя клиента
	/// </summary>
	public string Name { get; set; }

	/// <summary>
	/// Признак включения VPN
	/// </summary>
	public bool IsEnabled { get; set; }

	/// <summary>
	/// Адрес
	/// </summary>
	public	string Address { get; set; }

	/// <summary>
	/// Публичный ключ
	/// </summary>
	public string PublicKey { get; set; }

	/// <summary>
	/// Дата создания
	/// </summary>
	public DateOnly CreatedAt { get; set; }

	public void Update(WireguardClient client)
	{
		Name = client.Name;
		Address = client.Address;
		CreatedAt = client.CreatedAt;
		IsEnabled = client.Enabled;
		PublicKey = client.PublicKey;
	}
}