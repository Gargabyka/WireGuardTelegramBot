namespace WireGuard.Models;

/// <summary>
/// Показывает статус соединений в wireguard
/// </summary>
/// <param name="Name">Наименование клиента</param>
/// <param name="PublicKey">Публичный ключ</param>
/// <param name="AllowedIps">Разрешенные IP-адреса</param>
/// <param name="Endpoint">Конечная точка</param>
/// <param name="LatestHandshake"></param>
/// <param name="ReceiveBytes"></param>
/// <param name="TransmitBytes"></param>
public sealed record WireguardPeerStatus(
    string Name,
    string PublicKey,
    string? AllowedIps,
    string? Endpoint,
    DateTimeOffset? LatestHandshake,
    long ReceiveBytes,
    long TransmitBytes);