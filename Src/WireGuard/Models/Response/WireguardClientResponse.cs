namespace WireGuard.Models.Response;

/// <summary>
/// Возвращает информацию о клиенте WireGuard
/// </summary>
/// <param name="Id">Идентификатор клиента</param>
/// <param name="Name">Имя клиента</param>
public record WireguardClientResponse(Guid Id, string Name);