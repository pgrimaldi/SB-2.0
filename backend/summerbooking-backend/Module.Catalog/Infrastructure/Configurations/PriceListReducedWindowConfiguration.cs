using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Catalog.Domain.Entities;

namespace Module.Catalog.Infrastructure.Configurations;

/// <summary>Mapping EF Core di <see cref="PriceListReducedWindow"/> su catalog.pricing_price_list_reduced_windows.</summary>
public sealed class PriceListReducedWindowConfiguration : IEntityTypeConfiguration<PriceListReducedWindow>
{
	public void Configure(EntityTypeBuilder<PriceListReducedWindow> builder)
	{
		builder.ToTable("pricing_price_list_reduced_windows", "catalog", t =>
		{
			t.HasComment("Finestra di date che, se coincide esattamente con quella prenotata, attiva i prezzi ridotti per settore.");
			t.HasCheckConstraint("ck_catalog_pricing_price_list_reduced_windows_range", "ends_on >= starts_on");
		});

		builder.HasKey(x => x.Id).HasName("pk_catalog_pricing_price_list_reduced_windows");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PriceListId)
			.HasColumnName("price_list_id")
			.IsRequired()
			.HasComment("Pacchetto a cui si applica la finestra di prezzo ridotto.");
		builder.HasOne<PriceList>()
			.WithMany(p => p.ReducedWindows)
			.HasForeignKey(x => x.PriceListId)
			.HasConstraintName("fk_catalog_pricing_price_list_reduced_windows_price_list")
			.OnDelete(DeleteBehavior.Cascade);

		builder.Property(x => x.StartsOn)
			.HasColumnName("starts_on")
			.IsRequired()
			.HasComment("Inizio dell'intervallo che deve coincidere esattamente con quello della prenotazione.");

		builder.Property(x => x.EndsOn)
			.HasColumnName("ends_on")
			.IsRequired()
			.HasComment("Fine dell'intervallo che deve coincidere esattamente con quello della prenotazione.");

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
			.HasComment("Soft-delete della finestra.");

		builder.HasIndex(x => new { x.PriceListId, x.StartsOn, x.EndsOn })
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_catalog_pricing_price_list_reduced_windows_package_range");

		builder.HasMany(x => x.SectorPrices).WithOne().HasForeignKey(p => p.WindowId);
	}
}
