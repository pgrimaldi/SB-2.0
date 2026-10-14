using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Catalog.Domain.Entities;

namespace Module.Catalog.Infrastructure.Configurations;

/// <summary>Mapping EF Core di <see cref="RevenueDiscountedSpot"/> su catalog.pricing_revenue_discounted_spots.</summary>
public sealed class RevenueDiscountedSpotConfiguration : IEntityTypeConfiguration<RevenueDiscountedSpot>
{
	public void Configure(EntityTypeBuilder<RevenueDiscountedSpot> builder)
	{
		builder.ToTable("pricing_revenue_discounted_spots", "catalog", t => t.HasComment(
			"Postazioni a cui si applica lo sconto Revenue Management quando la configurazione è limitata a postazioni specifiche."));

		builder.HasKey(x => x.Id).HasName("pk_catalog_pricing_revenue_discounted_spots");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.SettingsId)
			.HasColumnName("settings_id")
			.IsRequired()
			.HasComment("Configurazione Revenue Management a cui appartiene la restrizione.");

		builder.Property(x => x.SpotId)
			.HasColumnName("spot_id")
			.IsRequired()
			.HasComment("Postazione a cui si applica lo sconto.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at")
			.HasDefaultValueSql("now()").HasComment("Data/ora di creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at")
			.HasDefaultValueSql("now()").HasComment("Data/ora di ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at")
			.HasComment("Soft-delete: valorizzata quando la restrizione viene rimossa.");

		builder.HasOne(x => x.Settings)
			.WithMany(s => s.DiscountedSpots)
			.HasForeignKey(x => x.SettingsId)
			.HasConstraintName("fk_catalog_pricing_revenue_discounted_spots_settings")
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasOne(x => x.Spot)
			.WithMany()
			.HasForeignKey(x => x.SpotId)
			.HasConstraintName("fk_catalog_pricing_revenue_discounted_spots_spot")
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasIndex(x => new { x.SettingsId, x.SpotId })
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_catalog_pricing_revenue_discounted_spots_settings_spot");

		builder.HasIndex(x => x.SpotId)
			.HasDatabaseName("ix_catalog_pricing_revenue_discounted_spots_spot");
	}
}
