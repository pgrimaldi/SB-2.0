using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Catalog.Domain.Entities;

namespace Module.Catalog.Infrastructure.Configurations;

/// <summary>Mapping EF Core di <see cref="CalculationMethod"/> su catalog.pricing_calculation_methods.</summary>
public sealed class CalculationMethodConfiguration : IEntityTypeConfiguration<CalculationMethod>
{
	public void Configure(EntityTypeBuilder<CalculationMethod> builder)
	{
		builder.ToTable("pricing_calculation_methods", "catalog", t => t.HasComment(
			"Lookup dei metodi di calcolo del prezzo multi-giorno di una voce di listino."));

		builder.HasKey(x => x.Id).HasName("pk_catalog_pricing_calculation_methods");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.Code)
			.HasColumnName("code")
			.HasMaxLength(40)
			.IsRequired()
			.HasComment("Codice canonico del metodo (proportional, daily, daily_with_weekend, logarithmic).");
		// Default now() a schema su created_at/updated_at: nessuna convenzione EF globale lo applica
		// (ModuleDbContextConventions mette solo il converter UTC e il query filter soft-delete), e un ETL
		// che omette queste colonne dall'INSERT per lasciare scattare il default fallirebbe sul NOT NULL.
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


		builder.HasIndex(x => x.Code).IsUnique().HasDatabaseName("uq_catalog_pricing_calculation_methods_code");

		builder.Property(x => x.IsActive)
			.HasColumnName("is_active")
			.IsRequired()
			.HasComment("Metodo selezionabile nell'editor prezzi.");

		// Seed obbligatorio: ogni riga di catalog.agreement_price_lists/pricing_price_lists porta un
		// calculation_method_id 1-4 (assunto dall'ETL di migrazione,
		// utility/01-migrazione-test/mappings/catalog/catalog_pricing_price_lists.yaml), e senza
		// queste 4 righe l'INSERT fallisce per violazione della FK
		// fk_catalog_agreement_price_lists_calculation_method (e l'omologa su pricing_price_lists).
		// Ordine/id 1-4 gia' documentato e assunto altrove (vedi commento nella spec YAML sopra) -
		// da NON riordinare senza aggiornare anche l'enum_map li'.
		var seedTimestamp = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
		builder.HasData(
			new { Id = (short)1, Code = "proportional", IsActive = true, CreatedAt = seedTimestamp, UpdatedAt = seedTimestamp },
			new { Id = (short)2, Code = "daily", IsActive = true, CreatedAt = seedTimestamp, UpdatedAt = seedTimestamp },
			new { Id = (short)3, Code = "daily_with_weekend", IsActive = true, CreatedAt = seedTimestamp, UpdatedAt = seedTimestamp },
			new { Id = (short)4, Code = "logarithmic", IsActive = true, CreatedAt = seedTimestamp, UpdatedAt = seedTimestamp });
	}
}
