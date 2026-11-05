using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Catalog.Domain.Entities;

namespace Module.Catalog.Infrastructure.Configurations;

/// <summary>Mapping EF Core di <see cref="BathhouseBadge"/> su catalog.bathhouse_badges.</summary>
public sealed class BathhouseBadgeConfiguration : IEntityTypeConfiguration<BathhouseBadge>
{
	public void Configure(EntityTypeBuilder<BathhouseBadge> builder)
	{
		builder.ToTable("bathhouse_badges", "catalog", t => t.HasComment(
			"Badge assegnati alle strutture dal systemadmin."));

		builder.HasKey(x => x.Id).HasName("pk_catalog_bathhouse_badges");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.BathhouseId)
			.HasColumnName("bathhouse_id")
			.IsRequired()
			.HasComment("Struttura.");

		builder.Property(x => x.BadgeId)
			.HasColumnName("badge_id")
			.IsRequired()
			.HasComment("Badge.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired().HasDefaultValueSql("now()").HasComment("Creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired().HasDefaultValueSql("now()").HasComment("Ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").HasComment("Soft-delete.");

		builder.HasIndex(x => new { x.BathhouseId, x.BadgeId }, "uq_catalog_bathhouse_badges_badge")
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_catalog_bathhouse_badges_badge");

		builder.HasIndex(x => x.BadgeId, "ix_catalog_bathhouse_badges_badge")
			.HasDatabaseName("ix_catalog_bathhouse_badges_badge");

		builder.HasOne<Bathhouse>()
			.WithMany(p => p.Badges)
			.HasForeignKey(x => x.BathhouseId)
			.HasConstraintName("fk_catalog_bathhouse_badges_bathhouse")
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasOne<ShowcaseBadge>()
			.WithMany()
			.HasForeignKey(x => x.BadgeId)
			.HasConstraintName("fk_catalog_bathhouse_badges_badge")
			.OnDelete(DeleteBehavior.Cascade);
	}
}
