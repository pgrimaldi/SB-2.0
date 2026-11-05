using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Catalog.Domain.Entities;

namespace Module.Catalog.Infrastructure.Configurations;

/// <summary>Mapping EF Core di <see cref="DiscountTimeRange"/> su catalog.pricing_discount_time_ranges.</summary>
public sealed class DiscountTimeRangeConfiguration : IEntityTypeConfiguration<DiscountTimeRange>
{
	public void Configure(EntityTypeBuilder<DiscountTimeRange> builder)
	{
		builder.ToTable("pricing_discount_time_ranges", "catalog", t =>
		{
			t.HasComment("Fasce orarie di validità di uno sconto.");
			t.HasCheckConstraint("ck_catalog_pricing_discount_time_ranges_range", "time_to > time_from");
		});

		builder.HasKey(x => x.Id).HasName("pk_catalog_pricing_discount_time_ranges");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.DiscountId)
			.HasColumnName("discount_id")
			.IsRequired()
			.HasComment("Sconto.");

		builder.Property(x => x.From)
			.HasColumnName("time_from")
			.IsRequired()
			.HasColumnType("time")
			.HasComment("Inizio della fascia.");

		builder.Property(x => x.To)
			.HasColumnName("time_to")
			.IsRequired()
			.HasColumnType("time")
			.HasComment("Fine della fascia.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired().HasDefaultValueSql("now()").HasComment("Creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired().HasDefaultValueSql("now()").HasComment("Ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").HasComment("Soft-delete.");

		builder.HasIndex(x => x.DiscountId, "ix_catalog_pricing_discount_time_ranges_discount")
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_catalog_pricing_discount_time_ranges_discount");

		builder.HasOne<Discount>()
			.WithMany(p => p.TimeRanges)
			.HasForeignKey(x => x.DiscountId)
			.HasConstraintName("fk_catalog_pricing_discount_time_ranges_discount")
			.OnDelete(DeleteBehavior.Cascade);
	}
}
