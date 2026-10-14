using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Catalog.Domain.Entities;
using Module.Catalog.Domain.Enums;

namespace Module.Catalog.Infrastructure.Configurations;

/// <summary>Mapping EF Core di <see cref="ServiceBookingSettings"/> su catalog.property_service_booking_settings.</summary>
public sealed class ServiceBookingSettingsConfiguration : IEntityTypeConfiguration<ServiceBookingSettings>
{
	public void Configure(EntityTypeBuilder<ServiceBookingSettings> builder)
	{
		builder.ToTable("property_service_booking_settings", "catalog", t => t.HasComment(
			"Configurazione del canale di prenotazione online dei servizi (bar/normali/a ore) per uno stabilimento (1:1)."));

		builder.HasKey(x => x.Id).HasName("pk_catalog_property_service_booking_settings");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("Stabilimento a cui si applica la configurazione.");

		builder.Property(x => x.IsEnabled)
			.HasColumnName("is_enabled")
			.HasDefaultValue(false)
			.IsRequired()
			.HasComment("Canale prenotazione online servizi attivo.");

		builder.Property(x => x.SurchargeType)
			.HasColumnName("surcharge_type")
			.HasColumnType("catalog.surcharge_type")
			.HasDefaultValueSql("'disable'")
			.IsRequired()
			.HasComment("Come interpretare surcharge_value: disabilitato, percentuale o importo fisso.");

		builder.Property(x => x.SurchargeValue)
			.HasColumnName("surcharge_value")
			.HasColumnType("numeric(12,2)")
			.HasDefaultValue(0m)
			.IsRequired()
			.HasComment("Importo o percentuale del sovrapprezzo sulle prenotazioni online di servizi.");

		builder.Property(x => x.RequiresManualConfirmation)
			.HasColumnName("requires_manual_confirmation")
			.HasDefaultValue(false)
			.IsRequired()
			.HasComment("Le prenotazioni servizi online restano \"da confermare\" e richiedono approvazione manuale del gestore.");

		builder.Property(x => x.SectionTitle)
			.HasColumnName("section_title")
			.HasMaxLength(255)
			.HasComment("Titolo personalizzato della sezione \"prenota i servizi online\" sul sito pubblico.");

		builder.Property(x => x.UsefulInfo)
			.HasColumnName("useful_info")
			.HasComment("Testo informativo mostrato durante la prenotazione online di un servizio.");

		builder.Property(x => x.EnabledPaymentMethods)
			.HasColumnName("enabled_payment_methods")
			.HasColumnType("catalog.online_payment_method[]")
			.HasDefaultValueSql("'{}'")
			.IsRequired()
			.HasComment("Insieme dei metodi di pagamento offerti per la prenotazione online di servizi.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Creazione della configurazione.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Ultima modifica della configurazione.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").HasComment("Soft-delete.");

		builder.HasIndex(x => x.PropertyId)
			.IsUnique()
			.HasDatabaseName("uq_catalog_property_service_booking_settings_property");

		builder.HasOne<Property>()
			.WithOne()
			.HasForeignKey<ServiceBookingSettings>(x => x.PropertyId)
			.HasConstraintName("fk_catalog_property_service_booking_settings_property")
			.OnDelete(DeleteBehavior.Cascade);
	}
}
