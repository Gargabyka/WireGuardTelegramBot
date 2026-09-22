using Domain.Entities;
using Domain.Interfaces;
using Domain.Interfaces.Repository;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WireGuard.Services.Interface;

namespace Telegram.Service;

/// <summary>
/// Периодически снимает снимок счётчиков трафика всех клиентов WireGuard —
/// нужно для расчёта статистики за день/неделю/месяц/всё время, т.к. сами счётчики
/// wg-easy сбрасываются при перезапуске интерфейса на сервере.
/// </summary>
public sealed class WireGuardTrafficSnapshotBackgroundService : BackgroundService
{
    private static readonly TimeSpan SnapshotInterval = TimeSpan.FromHours(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<WireGuardTrafficSnapshotBackgroundService> _logger;

    public WireGuardTrafficSnapshotBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<WireGuardTrafficSnapshotBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(SnapshotInterval);

        do
        {
            await TryCaptureSnapshotAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task TryCaptureSnapshotAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var wireGuardApiService = scope.ServiceProvider.GetRequiredService<IWireGuardApiService>();
            var snapshotRepo = scope.ServiceProvider.GetRequiredService<IWireGuardTrafficSnapshotRepository>();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var clients = await wireGuardApiService.GetWireguardClients(cancellationToken);
            if (clients is null or { Length: 0 })
            {
                return;
            }

            var now = DateTime.UtcNow;

            var snapshots = clients
                .Select(c => new WireGuardTrafficSnapshot
                {
                    WireGuardId = c.Id,
                    CapturedAtUtc = now,
                    TransferRx = c.TransferRx,
                    TransferTx = c.TransferTx,
                })
                .ToArray();

            snapshotRepo.AddRange(snapshots);
            await uow.SaveChangesAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // нормальная остановка
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при снятии снимка трафика WireGuard");
        }
    }
}
