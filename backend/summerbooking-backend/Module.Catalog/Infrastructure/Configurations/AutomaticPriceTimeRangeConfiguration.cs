using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Catalog.Domain.Entities;

namespace Module.Catalog.Infrastructure.Configurations;

/// <summary>Mapping EF Core di <see cref="AutomaticPriceTimeRange"/> su catalog.automatic_price_time_ranges.</summary>
public sealed class AutomaticPriceTimeRangeConfiguration : IEntityTypeConfiguration<AutomaticPriceTimeRange>
{
	public void Configure(EntityTypeBuilder<AutomaticPriceTimeRange> builder)
	{
		builder.ToTable("automatic_price_time_ranges", "catalog", t =>
		{
			t.HasComment("Fasce orarie di un periodo di validità di un prezzo automatico.");
			t.HasCheckConstraint("ck_catalog_automatic_price_time_ranges_range", "time_to > time_from");
		});

		builder.HasKey(x => x.Id).HasName("pk_catalog_automatic_price_time_ranges");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PeriodId)
			.HasColumnName("period_id")
			.IsRequired()
			.HasComment("Periodo di validità (automatic_price_periods).");

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

		builder.HasIndex(x => x.PeriodId, "ix_catalog_automatic_price_time_ranges_period")
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_catalog_automatic_price_time_ranges_period");

		builder.HasOne<AutomaticPricePeriod>()
			.WithMany(p => p.TimeRanges)
			.HasForeignKey(x => x.PeriodId)
			.HasConstraintName("fk_catalog_automatic_price_time_ranges_period")
			.OnDelete(DeleteBehavior.Cascade);
	}
}
