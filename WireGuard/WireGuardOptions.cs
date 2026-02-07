namespace WireGuardTelegramBot.WireGuard;

public class WireGuardOptions
{
    /// <summary>
    /// Имя интерфейса WireGuard (обычно wg0)
    /// </summary>
    public required string InterfaceName { get; init; }

    /// <summary>
    /// Применять через wg syncconf (true) или wg-quick down/up (false)
    /// </summary>
    public bool ApplyWithSyncConf { get; init; }
}