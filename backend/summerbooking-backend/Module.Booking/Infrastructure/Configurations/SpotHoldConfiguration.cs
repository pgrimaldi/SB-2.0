using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Booking.Domain.Entities;
using Module.Booking.Domain.Enums;

namespace Module.Booking.Infrastructure.Configurations;

public sealed class SpotHoldConfiguration : IEntityTypeConfiguration<SpotHold>
{
	public void Configure(EntityTypeBuilder<SpotHold> builder)
	{
		builder.ToTable("spot_holds", "booking", t => t.HasComment(
			"Blocco di una postazione senza prenotazione (manutenzione, VIP, planner, o blocco " +
			"temporaneo di checkout online); nessun cliente, nessun pagamento."));

		builder.HasKey(x => x.Id).HasName("pk_booking_spot_holds");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, copiato dal token — nessuna FK cross-schema");

		builder.Property(x => x.ResourceId)
			.HasColumnName("resource_id")
			.IsRequired()
			.HasComment("public_id di catalog.map_spots (postazione bloccata), copiato — nessuna FK cross-schema");

		builder.Property(x => x.SpotLabelSnapshot)
			.HasColumnName("spot_label_snapshot")
			.HasMaxLength(40)
			.HasComment("Etichetta della postazione congelata al momento del blocco");

		builder.Property(x => x.HoldKind)
			.HasColumnName("hold_kind")
			.HasColumnType("booking.hold_kind")
			.HasDefaultValueSql("'operational'")
			.IsRequired()
			.HasComment("Motivo del blocco: operativo (manutenzione/VIP/planner) o checkout temporaneo durante il pagamento online");

		builder.Property(x => x.HoldStatus)
			.HasColumnName("hold_status")
			.HasColumnType("booking.hold_status")
			.HasDefaultValueSql("'active'")
			.IsRequired()
			.HasComment("Stato del blocco: attivo, liberato manualmente, scaduto, convertito in prenotazione");

		builder.Property(x => x.StartDate)
			.HasColumnName("start_date")
			.HasColumnType("date")
			.IsRequired()
			.HasComment("Inizio dell'intervallo bloccato (estremi inclusi)");

		builder.Property(x => x.EndDate)
			.HasColumnName("end_date")
			.HasColumnType("date")
			.IsRequired()
			.HasComment("Fine dell'intervallo bloccato (estremi inclusi); l'espansione per-giorno è in booking_resource_consumptions");

		builder.Property(x => x.BookingPeriod)
			.HasColumnName("booking_period")
			.HasColumnType("booking.day_period")
			.HasDefaultValueSql("'full_day'")
			.IsRequired()
			.HasComment("Fascia bloccata");

		builder.Property(x => x.HeldUntil)
			.HasColumnName("held_until")
			.HasComment("Scadenza del blocco di checkout (now() + Booking:CheckoutHoldTtl, calcolato server-side); NULL per hold operational");

		builder.Property(x => x.ReasonNote)
			.HasColumnName("reason_note")
			.HasMaxLength(255)
			.HasComment("Nota libera opzionale, motivo della riserva operativa");

		builder.Property(x => x.CreatedByUserId)
			.HasColumnName("created_by_user_id")
			.HasComment("public_id di identity.access_users (operatore che ha creato il blocco, dal token), copiato — nessuna FK cross-schema; NULL per hold di checkout creati dal flusso online");

		builder.Property(x => x.ReleasedAt)
			.HasColumnName("released_at")
			.HasComment("Data/ora di rilascio del blocco (soft-release, per audit)");

		builder.Property(x => x.ReleasedByUserId)
			.HasColumnName("released_by_user_id")
			.HasComment("public_id di identity.access_users (operatore che ha liberato il blocco), copiato — nessuna FK cross-schema");

		builder.Property(x => x.ConvertedReservationId)
			.HasColumnName("converted_reservation_id")
			.HasComment("Prenotazione nata da questo blocco di checkout (hold_status = converted)");

		builder.Property(x => x.CreatedAt)
			.HasColumnName("created_at")
			.HasDefaultValueSql("now()")
			.IsRequired()
			.HasComment("Data/ora di creazione del blocco");

		builder.Property(x => x.UpdatedAt)
			.HasColumnName("updated_at")
			.HasDefaultValueSql("now()")
			.IsRequired()
			.HasComment("Data/ora di ultima modifica del blocco");

		builder.Property(x => x.DeletedAt)
			.HasColumnName("deleted_at")
			.HasComment("Soft-delete del blocco");

		builder.HasIndex(x => new { x.PropertyId, x.ResourceId, x.StartDate, x.EndDate })
			.HasFilter("hold_status = 'active'")
			.HasDatabaseName("ix_booking_spot_holds_property_resource_dates");

		builder.HasIndex(x => x.HeldUntil)
			.HasFilter("hold_status = 'active' AND held_until IS NOT NULL")
			.HasDatabaseName("ix_booking_spot_holds_expiry");

		builder.HasIndex(x => x.PropertyId)
			.HasFilter("hold_status = 'active' AND deleted_at IS NULL")
			.HasDatabaseName("ix_booking_spot_holds_property_active");

		builder.HasOne(x => x.ConvertedReservation)
			.WithMany()
			.HasForeignKey(x => x.ConvertedReservationId)
			.HasConstraintName("fk_booking_spot_holds_converted_reservation")
			.OnDelete(DeleteBehavior.SetNull);
	}
}
