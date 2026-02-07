using System.Text.Json.Serialization;

namespace WireGuardTelegramBot.WireGuard.Models.Request;

/// <summary>
/// Запрос на создание клиента
/// </summary>
public class AddClientRequest
{
    /// <summary>
    /// Имя клиента
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }
}