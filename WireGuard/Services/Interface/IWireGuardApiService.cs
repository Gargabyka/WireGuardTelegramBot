using WireGuardTelegramBot.WireGuard.Models;

namespace WireGuardTelegramBot.WireGuard.Services.Interface;

/// <summary>
/// Интерфейс для работы с WireGuard API
/// </summary>
public interface IWireGuardApiService
{
    /// <summary>
    /// Получить клиентов WireGuard
    /// </summary>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Возвращает клиентов</returns>
    public Task<WireguardClients[]?> GetWireguardClients(CancellationToken cancellationToken);

    /// <summary>
    /// Скачать конфигурацию WireGuard клиента
    /// </summary>
    /// <param name="id">Идентификатор клиента</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Массив байтов конфигурации</returns>
    public Task<byte[]> DownloadConfigAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Удалить клиента WireGuard
    /// </summary>
    /// <param name="id">Идентификатор клиента</param>
    /// <param name="cancellationToken">Токен отмены</param>
    public Task DeleteClientAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Добавить клиента WireGuard
    /// </summary>
    /// <param name="name">Имя клиента</param>
    /// <param name="cancellationToken">Токен отмены</param>
    public Task AddClientAsync(string name, CancellationToken cancellationToken);
}