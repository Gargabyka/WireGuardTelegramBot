namespace WireGuard.Models;

/// <summary>
/// Клиенты WireGuard
/// </summary>
/// <param name="Id">Идентификатор пользователя</param>
/// <param name="Name">Именование пользователя</param>
/// <param name="Enabled">Признак, что клиент включен</param>
/// <param name="Address">Id адрес</param>
/// <param name="PublicKey">Публичный ключ</param>
/// <param name="CreatedAt">Дата создания</param>
public record WireguardClients(Guid Id, string Name, bool Enabled, string Address, string PublicKey, DateTimeOffset CreatedAt);