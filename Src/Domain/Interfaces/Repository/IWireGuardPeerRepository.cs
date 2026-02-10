using Domain.Entities;

namespace Domain.Interfaces.Repository;

public interface IWireGuardPeerRepository
{
	/// <summary>
	/// Добавить соединения
	/// </summary>
	/// <param name="peer">Соединение</param>
	public void Add(WireGuardPeer peer);

	/// <summary>
	/// Добавить список соединений
	/// </summary>
	/// <param name="peers">Соединения</param>
	public void AddRange(WireGuardPeer[] peers);

	/// <summary>
	/// Удалить соединение
	/// </summary>
	/// <param name="peer">Соединение</param>
	public void Remove(WireGuardPeer peer);

	/// <summary>
	/// Получить информацию о соединениях
	/// </summary>
	/// <param name="cancellationToken">Токен отмены</param>
	/// <returns>Возвращает список соединений</returns>
	public Task<WireGuardPeer[]> GetPeersAsync(CancellationToken cancellationToken);

	/// <summary>
	/// Найти соединение по идентификатору WireGuard.
	/// </summary>
	public Task<WireGuardPeer?> GetByWireGuardIdAsync(Guid wireGuardId, CancellationToken cancellationToken);
}