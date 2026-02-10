using Domain.Entities;
using Domain.Interfaces.Repository;
using EFCore;
using Microsoft.EntityFrameworkCore;

namespace Application.Repository;

public class WireGuardPeerRepository : IWireGuardPeerRepository
{
	private readonly AppDbContext _dbContext;

	public WireGuardPeerRepository(AppDbContext dbContext)
	{
		_dbContext = dbContext;
	}

	public void Add(WireGuardPeer peer)
		=> _dbContext.WireGuardPeers.Add(peer);

	public void AddRange(WireGuardPeer[] peers)
		=> _dbContext.WireGuardPeers.AddRange(peers);

	public void Remove(WireGuardPeer peer)
		=> _dbContext.WireGuardPeers.Remove(peer);

	public Task<WireGuardPeer[]> GetPeersAsync(CancellationToken cancellationToken)
		=> _dbContext
			.WireGuardPeers
			.AsNoTracking()
			.ToArrayAsync(cancellationToken);

	public Task<WireGuardPeer?> GetByWireGuardIdAsync(Guid wireGuardId, CancellationToken cancellationToken)
		=> _dbContext
			.WireGuardPeers
			.FirstOrDefaultAsync(x => x.WireGuardId == wireGuardId, cancellationToken);
}