using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Catalog.Domain.Entities;
using Module.Catalog.Domain.Enums;

namespace Module.Catalog.Infrastructure.Configurations;

/// <summary>Mapping EF Core di <see cref="OnlineBookingSettings"/> su catalog.property_online_booking_settings.</summary>
public sealed class OnlineBookingSettingsConfiguration : IEntityTypeConfiguration<OnlineBookingSettings>
{
	public void Configure(EntityTypeBuilder<OnlineBookingSettings> builder)
	{
		builder.ToTable("property_online_booking_settings", "catalog", t => t.HasComment(
			"Configurazione del canale di prenotazione online delle postazioni per uno stabilimento (1:1)."));

		builder.HasKey(x => x.Id).HasName("pk_catalog_property_online_booking_settings");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("Stabilimento a cui si applica la configurazione.");

		builder.Property(x => x.IsEnabled)
			.HasColumnName("is_enabled")
			.HasDefaultValue(false)
			.IsRequired()
			.HasComment("Canale prenotazione online postazioni attivo per lo stabilimento.");

		builder.Property(x => x.IsMapPick)
			.HasColumnName("is_map_pick")
			.HasDefaultValue(false)
			.IsRequired()
			.HasComment("Il cliente sceglie la postazione dalla mappa interattiva (vs. lista/richiesta); indipendente dagli altri due flag di restrizione.");

		builder.Property(x => x.IsLocationRestricted)
			.HasColumnName("is_location_restricted")
			.HasDefaultValue(false)
			.IsRequired()
			.HasComment("La prenotazione online è limitata al sottoinsieme di postazioni in allowed_locations.");

		builder.Property(x => x.IsPeriodRestricted)
			.HasColumnName("is_period_restricted")
			.HasDefaultValue(false)
			.IsRequired()
			.HasComment("La prenotazione online è consentita solo nelle finestre di date in allowed_periods.");

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
			.HasComment("Importo o percentuale del sovrapprezzo sulle prenotazioni online (calcolato server-side dal motore prezzo).");

		builder.Property(x => x.IsOnlinePaymentBlocked)
			.HasColumnName("is_online_payment_blocked")
			.HasDefaultValue(false)
			.IsRequired()
			.HasComment("Se true, il cliente non può pagare online: override che blocca il pagamento a prescindere da enabled_payment_methods.");

		builder.Property(x => x.RequiresManualConfirmation)
			.HasColumnName("requires_manual_confirmation")
			.HasDefaultValue(false)
			.IsRequired()
			.HasComment("Le prenotazioni online restano \"da confermare\" e richiedono approvazione manuale del gestore.");

		builder.Property(x => x.AllowsSameDayBooking)
			.HasColumnName("allows_same_day_booking")
			.HasDefaultValue(false)
			.IsRequired()
			.HasComment("Il cliente può prenotare online per il giorno stesso; se false, solo da domani in poi.");

		builder.Property(x => x.AllowsHalfDay)
			.HasColumnName("allows_half_day")
			.HasDefaultValue(false)
			.IsRequired()
			.HasComment("Abilita la prenotazione online per mezza giornata (mattina/pomeriggio).");

		builder.Property(x => x.AllowsSeasonal)
			.HasColumnName("allows_seasonal")
			.HasDefaultValue(false)
			.IsRequired()
			.HasComment("Abilita la prenotazione online \"stagionale\" (pass multi-settimana/mese).");

		builder.Property(x => x.AllowsSharing)
			.HasColumnName("allows_sharing")
			.HasDefaultValue(false)
			.IsRequired()
			.HasComment("Espone al cliente la funzione di sharing durante la prenotazione online (i parametri operativi vivono in C1).");

		builder.Property(x => x.MaxSpotsPerBooking)
			.HasColumnName("max_spots_per_booking")
			.HasComment("Numero massimo di postazioni prenotabili in una singola prenotazione online; NULL = nessun limite.");

		builder.Property(x => x.AllowOnlineBookingScope)
			.HasColumnName("allow_online_booking_scope")
			.HasColumnType("catalog.online_booking_scope")
			.HasDefaultValueSql("'all'")
			.IsRequired()
			.HasComment("Chi può prenotare online: tutti, o solo clienti già in rubrica (contacts_only).");

		builder.Property(x => x.UsefulInfo)
			.HasColumnName("useful_info")
			.HasComment("Testo libero \"informazioni utili\" mostrato al cliente nel flusso online.");

		builder.Property(x => x.GoodToKnow)
			.HasColumnName("good_to_know")
			.HasComment("Testo libero aggiuntivo \"da sapere\" mostrato al cliente.");

		builder.Property(x => x.EnabledPaymentMethods)
			.HasColumnName("enabled_payment_methods")
			.HasColumnType("catalog.online_payment_method[]")
			.HasDefaultValueSql("'{}'")
			.IsRequired()
			.HasComment("Insieme dei metodi di pagamento offerti al cliente per la prenotazione online postazioni.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Creazione della configurazione.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Ultima modifica della configurazione.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").HasComment("Soft-delete (non usato in pratica: per disattivare il canale si usa is_enabled = false).");

		builder.HasIndex(x => x.PropertyId)
			.IsUnique()
			.HasDatabaseName("uq_catalog_property_online_booking_settings_property");

		builder.HasOne<Property>()
			.WithOne()
			.HasForeignKey<OnlineBookingSettings>(x => x.PropertyId)
			.HasConstraintName("fk_catalog_property_online_booking_settings_property")
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasMany(x => x.AllowedLocations)
			.WithOne()
			.HasForeignKey(x => x.SettingsId)
			.HasConstraintName("fk_catalog_property_online_allowed_locations_settings")
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasMany(x => x.AllowedPeriods)
			.WithOne()
			.HasForeignKey(x => x.SettingsId)
			.HasConstraintName("fk_catalog_property_online_allowed_periods_settings")
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasMany(x => x.StayLimits)
			.WithOne()
			.HasForeignKey(x => x.SettingsId)
			.HasConstraintName("fk_catalog_property_online_stay_limits_settings")
			.OnDelete(DeleteBehavior.Cascade);
	}
}
