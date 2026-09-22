using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EFCore.Configurations;

public sealed class WireGuardTrafficSnapshotConfiguration : IEntityTypeConfiguration<WireGuardTrafficSnapshot>
{
	public void Configure(EntityTypeBuilder<WireGuardTrafficSnapshot> builder)
	{
		builder.HasKey(x => x.Id);

		builder.Property(x => x.WireGuardId)
			.IsRequired();

		builder.Property(x => x.CapturedAtUtc)
			.IsRequired();

		builder.Property(x => x.TransferRx)
			.IsRequired();

		builder.Property(x => x.TransferTx)
			.IsRequired();

		builder.HasIndex(x => new { x.WireGuardId, x.CapturedAtUtc });
	}
}
