using Domain.Interfaces;

namespace Domain.Entities;

/// <summary>
/// Снимок счётчиков трафика WireGuard-клиента — нужен для расчёта статистики за периоды,
/// т.к. живые счётчики wg-easy сбрасываются при перезапуске интерфейса на сервере.
/// </summary>
public class WireGuardTrafficSnapshot : IEntity
{
    public int Id { get; init; }

    /// <summary>
    /// Идентификатор клиента в WireGuard (как в wg-easy)
    /// </summary>
    public Guid WireGuardId { get; init; }

    /// <summary>
    /// Когда снят снимок
    /// </summary>
    public DateTime CapturedAtUtc { get; init; }

    /// <summary>
    /// Байт получено сервером от клиента (нарастающий итог с последнего рестарта интерфейса)
    /// </summary>
    public long TransferRx { get; init; }

    /// <summary>
    /// Байт отправлено сервером клиенту (нарастающий итог с последнего рестарта интерфейса)
    /// </summary>
    public long TransferTx { get; init; }
}
