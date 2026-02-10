using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EFCore;

public class AppDbContext : DbContext
{
	public AppDbContext(DbContextOptions<AppDbContext> options)
		: base(options)
	{
	}

	public DbSet<TelegramUser> TelegramUsers { get; set; }

	public DbSet<WireGuardPeer> WireGuardPeers { get; set; }

	public DbSet<WireGuardUser> WireGuardUsers { get; set; }

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
		base.OnModelCreating(modelBuilder);
	}
}