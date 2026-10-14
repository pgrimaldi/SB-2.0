using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Catalog.Domain.Entities;

namespace Module.Catalog.Infrastructure.Configurations;

/// <summary>Mapping EF Core di <see cref="DiscountDateRange"/> su catalog.pricing_discount_date_ranges.</summary>
public sealed class DiscountDateRangeConfiguration : IEntityTypeConfiguration<DiscountDateRange>
{
	public void Configure(EntityTypeBuilder<DiscountDateRange> builder)
	{
		builder.ToTable("pricing_discount_date_ranges", "catalog", t =>
		{
			t.HasComment("Finestra di validità (una fra eventualmente più) di un voucher sconto.");
			t.HasCheckConstraint("ck_catalog_pricing_discount_date_ranges_range", "ends_on >= starts_on");
		});

		builder.HasKey(x => x.Id).HasName("pk_catalog_pricing_discount_date_ranges");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.DiscountId)
			.HasColumnName("discount_id")
			.IsRequired()
			.HasComment("Voucher a cui appartiene la finestra di validità.");
		builder.HasOne<Discount>()
			.WithMany(d => d.ValidityWindows)
			.HasForeignKey(x => x.DiscountId)
			.HasConstraintName("fk_catalog_pricing_discount_date_ranges_discount")
			.OnDelete(DeleteBehavior.Cascade);

		builder.Property(x => x.StartsOn)
			.HasColumnName("starts_on")
			.IsRequired()
			.HasComment("Inizio dell'intervallo in cui il voucher è riscattabile.");

		builder.Property(x => x.EndsOn)
			.HasColumnName("ends_on")
			.IsRequired()
			.HasComment("Fine dell'intervallo in cui il voucher è riscattabile.");

		// bug reale osservato il 2026-09-16: created_at/updated_at non avevano default now() a
		// schema (nessuna convenzione EF globale lo applica, verificato in ModuleDbContextConventions
		// - solo converter UTC e query filter soft-delete) - un ETL che omette queste colonne
		// dall'INSERT (comportamento normale, per lasciare scattare il default) falliva NOT NULL.
		builder.Property(x => x.CreatedAt)
			.HasColumnName("created_at")
			.IsRequired()
			.HasDefaultValueSql("now()")
			.HasComment("Creazione della riga.");

		builder.Property(x => x.UpdatedAt)
			.HasColumnName("updated_at")
			.IsRequired()
			.HasDefaultValueSql("now()")
			.HasComment("Ultima modifica della riga.");


		builder.Property(x => x.DeletedAt)
			.HasColumnName("deleted_at")
			.HasComment("Soft-delete della finestra di validità.");

		builder.HasIndex(x => new { x.DiscountId, x.StartsOn, x.EndsOn })
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_catalog_pricing_discount_date_ranges_discount_range");
	}
}
