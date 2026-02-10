using Domain.Entities;
using Domain.Interfaces.Repository;
using EFCore;
using Microsoft.EntityFrameworkCore;

namespace Application.Repository;

public class WireGuardUserRepository : IWireGuardUserRepository
{
	private readonly AppDbContext _dbContext;

	public WireGuardUserRepository(AppDbContext dbContext)
	{
		_dbContext = dbContext;
	}

	public Task<WireGuardUser[]> GetConnectionsByTelegramIdAsync(int telegramUserId, CancellationToken cancellationToken)
		=> _dbContext
			.WireGuardUsers
			.AsNoTracking()
			.Include(x => x.Peer)
			.Where(x => x.TelegramUserId == telegramUserId)
			.ToArrayAsync(cancellationToken);

	public void Add(WireGuardUser wireGuardUser)
		=> _dbContext.WireGuardUsers.Add(wireGuardUser);

	public Task<bool> ExistsAsync(int telegramUserId, int peerId, CancellationToken cancellationToken)
		=> _dbContext
			.WireGuardUsers
			.AsNoTracking()
			.AnyAsync(x => x.TelegramUserId == telegramUserId && x.PeerId == peerId, cancellationToken);

	public Task<int[]> GetUsedPeerIdsAsync(CancellationToken cancellationToken)
		=> _dbContext
			.WireGuardUsers
			.AsNoTracking()
			.Select(x => x.PeerId)
			.Distinct()
			.ToArrayAsync(cancellationToken);

	public Task<int> RemoveLinkAsync(int telegramUserId, int peerId, CancellationToken cancellationToken)
		=> _dbContext
			.WireGuardUsers
			.Where(x => x.TelegramUserId == telegramUserId && x.PeerId == peerId)
			.ExecuteDeleteAsync(cancellationToken);
}