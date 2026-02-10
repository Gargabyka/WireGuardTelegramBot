namespace WireGuard.Services.Interface;

/// <summary>
/// Интерфейс для работы с WireGuard напрямую
/// </summary>
public interface IWireguardService
{
	/// <summary>
	/// Применить изменения из wg0.conf к запущенному интерфейсу (по настройке).
	/// </summary>
	public Task ApplyConfigAsync(CancellationToken cancellationToken);

	/// <summary>
	/// Применить через wg syncconf (мягкое обновление).
	/// </summary>
	public Task ApplyConfigWithSyncConfAsync(CancellationToken cancellationToken);

	/// <summary>
	/// Применить через wg-quick down/up (жёсткое обновление).
	/// </summary>
	public Task ApplyConfigWithDownUpAsync(CancellationToken cancellationToken);
}