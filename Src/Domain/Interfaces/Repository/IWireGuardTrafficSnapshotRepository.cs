using Domain.Entities;

namespace Domain.Interfaces.Repository;

/// <summary>
/// Интерфейс для работы с <see cref="WireGuardTrafficSnapshot"/>
/// </summary>
public interface IWireGuardTrafficSnapshotRepository
{
	/// <summary>
	/// Добавить снимки счётчиков трафика
	/// </summary>
	/// <param name="snapshots">Снимки</param>
	public void AddRange(WireGuardTrafficSnapshot[] snapshots);

	/// <summary>
	/// Получить снимки для расчёта расхода за период
	/// </summary>
	/// <param name="wireGuardId">Идентификатор клиента WireGuard</param>
	/// <param name="sinceUtc">Начало периода (null — за всё время)</param>
	/// <param name="cancellationToken">Токен отмены</param>
	/// <returns>
	/// Возвращает снимки за период в хронологическом порядке. Если период ограничен —
	/// первым элементом идёт последний снимок ДО начала периода (нужен как база для расчёта).
	/// </returns>
	public Task<WireGuardTrafficSnapshot[]> GetSnapshotsForPeriodAsync(
		Guid wireGuardId,
		DateTime? sinceUtc,
		CancellationToken cancellationToken);

	/// <summary>
	/// Получить дату самого первого снимка клиента (с какого момента ведётся учёт)
	/// </summary>
	/// <param name="wireGuardId">Идентификатор клиента WireGuard</param>
	/// <param name="cancellationToken">Токен отмены</param>
	public Task<DateTime?> GetFirstSnapshotDateAsync(Guid wireGuardId, CancellationToken cancellationToken);
}
