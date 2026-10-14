using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Catalog.Domain.Entities;

namespace Module.Catalog.Infrastructure.Configurations;

/// <summary>Mapping EF Core di <see cref="RevenueSettings"/> su catalog.pricing_revenue_settings.</summary>
public sealed class RevenueSettingsConfiguration : IEntityTypeConfiguration<RevenueSettings>
{
	public void Configure(EntityTypeBuilder<RevenueSettings> builder)
	{
		builder.ToTable("pricing_revenue_settings", "catalog", t => t.HasComment(
			"Configurazione per stabilimento della feature Revenue Management (prezzo suggerito e banner sconto in pagina pubblica)."));

		builder.HasKey(x => x.Id).HasName("pk_catalog_pricing_revenue_settings");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("Stabilimento a cui si applica la configurazione Revenue Management.");

		builder.Property(x => x.IsPriceDiscountEnabled)
			.HasColumnName("is_price_discount_enabled")
			.IsRequired()
			.HasDefaultValue(false)
			.HasComment("Interruttore generale della feature \"notifiche prezzo scontato\".");

		builder.Property(x => x.IsShownOnPublicPage)
			.HasColumnName("is_shown_on_public_page")
			.IsRequired()
			.HasDefaultValue(false)
			.HasComment("Il banner sconto è visibile sulla pagina pubblica dello stabilimento / flusso di prenotazione (indipendente da marketing_event.showOnEstablishmentPage di C6).");

		builder.Property(x => x.DailyBannerCount)
			.HasColumnName("daily_banner_count")
			.HasColumnType("smallint")
			.IsRequired()
			.HasDefaultValue((short)0)
			.HasComment("Numero di volte al giorno in cui mostrare il banner (finestra 10:00–20:00 divisa in altrettanti slot).");

		builder.Property(x => x.DailyBannerShownCount)
			.HasColumnName("daily_banner_shown_count")
			.HasColumnType("smallint")
			.IsRequired()
			.HasDefaultValue((short)0)
			.HasComment("Numero di volte in cui il banner è già stato mostrato oggi; incrementata server-side, azzerata ogni notte.");

		builder.Property(x => x.IsDiscountLimitedToSpots)
			.HasColumnName("is_discount_limited_to_spots")
			.IsRequired()
			.HasDefaultValue(false)
			.HasComment("Se true, lo sconto è ristretto alle postazioni elencate in catalog_pricing_revenue_discounted_spots invece che a tutto lo stabilimento.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at")
			.HasDefaultValueSql("now()").HasComment("Data/ora di creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at")
			.HasDefaultValueSql("now()").HasComment("Data/ora di ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at")
			.HasComment("Soft-delete.");

		builder.HasOne(x => x.Property)
			.WithMany()
			.HasForeignKey(x => x.PropertyId)
			.HasConstraintName("fk_catalog_pricing_revenue_settings_property")
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasMany(x => x.DiscountedSpots)
			.WithOne(s => s.Settings)
			.HasForeignKey(s => s.SettingsId)
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasIndex(x => x.PropertyId)
			.IsUnique()
			.HasDatabaseName("uq_catalog_pricing_revenue_settings_property");
	}
}
