using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace EFCore.Configurations;

public sealed class WireGuardPeersConfiguration : IEntityTypeConfiguration<WireGuardPeer>
{
	public void Configure(EntityTypeBuilder<WireGuardPeer> builder)
	{
		builder.HasKey(x => x.Id);

		builder.Property(x => x.WireGuardId)
			.IsRequired();

		builder.HasIndex(x => x.WireGuardId)
			.IsUnique();

		builder.Property(x => x.Name)
			.HasMaxLength(128)
			.IsRequired();

		builder.Property(x => x.Address)
			.HasMaxLength(64)
			.IsRequired();

		builder.Property(x => x.PublicKey)
			.HasMaxLength(256)
			.IsRequired();

		builder.Property(x => x.IsEnabled)
			.IsRequired();

		var dateOnlyConverter = new ValueConverter<DateOnly, string>(
			v => v.ToString("yyyy-MM-dd"),
			v => DateOnly.Parse(v));

		builder.Property(x => x.CreatedAt)
			.HasConversion(dateOnlyConverter)
			.IsRequired();
	}
}