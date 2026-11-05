using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Catalog.Domain.Entities;

namespace Module.Catalog.Infrastructure.Configurations;

/// <summary>Mapping EF Core di <see cref="CountryVatRate"/> su catalog.country_vat_rates.</summary>
public sealed class CountryVatRateConfiguration : IEntityTypeConfiguration<CountryVatRate>
{
	public void Configure(EntityTypeBuilder<CountryVatRate> builder)
	{
		builder.ToTable("country_vat_rates", "catalog", t =>
		{
			t.HasComment("Aliquote IVA selezionabili per Paese dello stabilimento (pacchetti, articoli aggiuntivi, servizi); lookup di piattaforma caricata con le migration.");
			t.HasCheckConstraint("ck_catalog_country_vat_rates_rate", "rate > 0 AND rate < 100");
		});

		builder.HasKey(x => x.Id).HasName("pk_catalog_country_vat_rates");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();
		builder.Property(x => x.CountryCode).HasColumnName("country_code").HasColumnType("char(2)").IsRequired()
			.HasComment("Codice ISO del Paese (catalog.countries.code), senza FK: le aliquote arrivano con le migration, i paesi con l'ETL.");
		builder.Property(x => x.Rate).HasColumnName("rate").HasPrecision(5, 2).IsRequired().HasComment("Aliquota in percentuale (22.00 = 22%).");
		builder.Property(x => x.SortOrder).HasColumnName("sort_order").IsRequired().HasDefaultValue((short)0)
			.HasComment("Ordine nella tendina: la prima è l'aliquota proposta.");

		builder.HasIndex(x => new { x.CountryCode, x.Rate }).IsUnique().HasDatabaseName("uq_catalog_country_vat_rates_country_rate");
	}
}
