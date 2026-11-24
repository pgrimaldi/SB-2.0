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

		builder.Property(x => x.MinBookingDays)
			.HasColumnName("min_booking_days")
			.HasComment("Giorni minimi della prenotazione per avere lo sharing; NULL = nessun minimo");

		builder.Property(x => x.AppliesTo)
			.HasColumnName("applies_to")
			.HasColumnType("booking.resale_scope[]")
			.HasDefaultValueSql("'{}'")
			.IsRequired()
			.HasComment("A cosa si applica lo sharing: solo alle stagionali, a tutti gli articoli (non solo agli ombrelloni)");

		builder.Property(x => x.AbsencesAsSharing)
			.HasColumnName("absences_as_sharing")
			.HasDefaultValue(false)
			.IsRequired()
			.HasComment("Le assenze programmate valgono come sharing: il giorno ripreso fa maturare il credito al titolare");

		builder.Property(x => x.ManualCreditEnabled)
			.HasColumnName("manual_credit_enabled")
			.HasDefaultValue(false)
			.IsRequired()
			.HasComment("Crediti assegnati a mano: per ogni giorno ripreso il titolare matura manual_credit_units_per_day crediti");

		builder.Property(x => x.ManualCreditUnitsPerDay)
			.HasColumnName("manual_credit_units_per_day")
			.HasDefaultValue(0)
			.IsRequired()
			.HasComment("Crediti per ogni giorno ceduto e ripreso");

		builder.Property(x => x.ManualCreditAmount)
			.HasColumnName("manual_credit_amount")
			.HasColumnType("numeric(12,2)")
			.HasDefaultValue(0m)
			.IsRequired()
			.HasComment("Valore in euro di un credito manuale");

		builder.Property(x => x.VoucherCreditEnabled)
			.HasColumnName("voucher_credit_enabled")
			.HasDefaultValue(false)
			.IsRequired()
			.HasComment("Voucher automatico al titolare per ogni giorno ripreso");

		builder.Property(x => x.VoucherCreditAmount)
			.HasColumnName("voucher_credit_amount")
			.HasColumnType("numeric(12,2)")
			.HasDefaultValue(0m)
			.IsRequired()
			.HasComment("Valore in euro del voucher per giorno ripreso");

		builder.Property(x => x.CardTopUpEnabled)
			.HasColumnName("card_top_up_enabled")
			.HasDefaultValue(false)
			.IsRequired()
			.HasComment("Ricarica della tessera prepagata del titolare per ogni giorno ripreso");

		builder.Property(x => x.CardTopUpAmount)
			.HasColumnName("card_top_up_amount")
			.HasColumnType("numeric(12,2)")
			.HasDefaultValue(0m)
			.IsRequired()
			.HasComment("Importo in euro della ricarica per giorno ripreso");

		builder.Ignore(x => x.GivesCredit);

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
