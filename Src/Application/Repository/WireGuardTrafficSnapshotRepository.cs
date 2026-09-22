using Domain.Entities;
using Domain.Interfaces.Repository;
using EFCore;
using Microsoft.EntityFrameworkCore;

namespace Application.Repository;

public class WireGuardTrafficSnapshotRepository : IWireGuardTrafficSnapshotRepository
{
	private readonly AppDbContext _dbContext;

	public WireGuardTrafficSnapshotRepository(AppDbContext dbContext)
	{
		_dbContext = dbContext;
	}

	public void AddRange(WireGuardTrafficSnapshot[] snapshots)
		=> _dbContext.WireGuardTrafficSnapshots.AddRange(snapshots);

	public async Task<WireGuardTrafficSnapshot[]> GetSnapshotsForPeriodAsync(
		Guid wireGuardId,
		DateTime? sinceUtc,
		CancellationToken cancellationToken)
	{
		var query = _dbContext.WireGuardTrafficSnapshots
			.AsNoTracking()
			.Where(x => x.WireGuardId == wireGuardId);

		if (!sinceUtc.HasValue)
		{
			return await query
				.OrderBy(x => x.CapturedAtUtc)
				.ToArrayAsync(cancellationToken);
		}

		// Последний снимок ДО начала периода — база для расчёта расхода в первый же час периода.
		var baseline = await query
			.Where(x => x.CapturedAtUtc < sinceUtc.Value)
			.OrderByDescending(x => x.CapturedAtUtc)
			.FirstOrDefaultAsync(cancellationToken);

		var periodSnapshots = await query
			.Where(x => x.CapturedAtUtc >= sinceUtc.Value)
			.OrderBy(x => x.CapturedAtUtc)
			.ToArrayAsync(cancellationToken);

		return baseline is null
			? periodSnapshots
			: [baseline, .. periodSnapshots];
	}

	public Task<DateTime?> GetFirstSnapshotDateAsync(Guid wireGuardId, CancellationToken cancellationToken)
		=> _dbContext.WireGuardTrafficSnapshots
			.AsNoTracking()
			.Where(x => x.WireGuardId == wireGuardId)
			.OrderBy(x => x.CapturedAtUtc)
			.Select(x => (DateTime?)x.CapturedAtUtc)
			.FirstOrDefaultAsync(cancellationToken);
}
