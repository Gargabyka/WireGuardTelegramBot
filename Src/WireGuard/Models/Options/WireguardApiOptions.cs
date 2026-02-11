namespace WireGuard.Models.Options;

/// <summary>
/// Класс опций WireGuard
/// </summary>
public sealed class WireguardApiOptions
{
    /// <summary>
    /// Url до API
    /// </summary>
    public required string ApiUrl { get; init; }

    /// <summary>
    /// Пароль от API
    /// </summary>
    public required string ApiPassword { get; init; }
}