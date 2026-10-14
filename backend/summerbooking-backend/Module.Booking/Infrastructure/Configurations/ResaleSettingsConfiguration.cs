using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Booking.Domain.Entities;
using Module.Booking.Domain.Enums;

namespace Module.Booking.Infrastructure.Configurations;

public sealed class ResaleSettingsConfiguration : IEntityTypeConfiguration<ResaleSettings>
{
	public void Configure(EntityTypeBuilder<ResaleSettings> builder)
	{
		builder.ToTable("resale_settings", "booking", t => t.HasComment(
			"Configurazione per stabilimento della feature rivendita/sharing del posto: guida il " +
			"comportamento di booking_resale_offers e booking_resale_credit_ledger."));

		builder.HasKey(x => x.Id).HasName("pk_booking_resale_settings");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, copiato — nessuna FK cross-schema");

		builder.Property(x => x.IsEnabled)
			.HasColumnName("is_enabled")
			.HasDefaultValue(false)
			.IsRequired()
			.HasComment("La feature rivendita/sharing è attiva per lo stabilimento");

		builder.Property(x => x.Mode)
			.HasColumnName("mode")
			.HasColumnType("booking.resale_mode")
			.HasDefaultValueSql("'absence'")
			.IsRequired()
			.HasComment("Come viene gestita la cessione di un giorno: solo assenza pianificata senza credito, o sharing con credito al titolare");

		builder.Property(x => x.AppliesTo)
			.HasColumnName("applies_to")
			.HasColumnType("booking.resale_scope[]")
			.HasDefaultValueSql("'{}'")
			.IsRequired()
			.HasComment("Estensioni della rivendita: consentita per pass stagionali, estesa a tutte le postazioni del pacchetto");

		builder.Property(x => x.BothOptionsDisabled)
			.HasColumnName("both_options_disabled")
			.HasDefaultValue(false)
			.IsRequired()
			.HasComment("Disabilita entrambe le modalità (semantica esatta da riverificare col committente, migrato as-is)");

		builder.Property(x => x.CreditUnitsPerDay)
			.HasColumnName("credit_units_per_day")
			.HasDefaultValue(0)
			.IsRequired()
			.HasComment("Quanti crediti simbolici matura il titolare per ogni giorno ceduto e ripreso");

		builder.Property(x => x.CreditCalc)
			.HasColumnName("credit_calc")
			.HasColumnType("booking.resale_credit_calc")
			.HasDefaultValueSql("'fixed_amount'")
			.IsRequired()
			.HasComment("Come si calcola il valore del credito per giorno ceduto: importo fisso o percentuale della tariffa giornaliera");

		builder.Property(x => x.CreditAmount)
			.HasColumnName("credit_amount")
			.HasColumnType("numeric(12,2)")
			.HasDefaultValue(0m)
			.IsRequired()
			.HasComment("Valore del credito: importo € se credit_calc = fixed_amount, percentuale 0-100 se daily_percentage");

		builder.Property(x => x.CreatedAt)
			.HasColumnName("created_at")
			.HasDefaultValueSql("now()")
			.IsRequired()
			.HasComment("Data/ora di creazione della configurazione");

		builder.Property(x => x.UpdatedAt)
			.HasColumnName("updated_at")
			.HasDefaultValueSql("now()")
			.IsRequired()
			.HasComment("Data/ora di ultima modifica della configurazione");

		builder.Property(x => x.DeletedAt)
			.HasColumnName("deleted_at")
			.HasComment("Soft-delete della configurazione");

		builder.HasIndex(x => x.PropertyId)
			.IsUnique()
			.HasDatabaseName("uq_booking_resale_settings_property");
	}
}
