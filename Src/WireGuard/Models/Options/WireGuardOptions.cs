namespace WireGuard.Models.Options;

public class WireGuardOptions
{
    /// <summary>
    /// Имя интерфейса WireGuard (обычно wg0)
    /// </summary>
    public required string InterfaceName { get; init; }
}