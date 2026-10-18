using System.Text.Json.Serialization;

namespace WireGuard.Models.Request;

/// <summary>
/// Запрос на авторизацию
/// </summary>
public record AuthorizationRequest
{
    /// <summary>
    /// Пароль
    /// </summary>
    [JsonPropertyName("password")]
    public required string Password { get; init; }
}