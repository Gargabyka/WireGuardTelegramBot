namespace WireGuardTelegramBot.WireGuard.Services.Interface;

/// <summary>
/// Интерфейс для работы с WireGuard напрямую
/// </summary>
public interface IWireguardService
{
    /// <summary>
    /// Применить изменения из wg0.conf к запущенному интерфейсу.
    /// </summary>
    public Task ApplyConfigAsync(CancellationToken cancellationToken);
}