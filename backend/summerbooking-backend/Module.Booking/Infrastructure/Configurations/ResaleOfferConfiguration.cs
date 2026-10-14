using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Booking.Domain.Entities;
using Module.Booking.Domain.Enums;

namespace Module.Booking.Infrastructure.Configurations;

public sealed class ResaleOfferConfiguration : IEntityTypeConfiguration<ResaleOffer>
{
	public void Configure(EntityTypeBuilder<ResaleOffer> builder)
	{
		builder.ToTable("resale_offers", "booking", t => t.HasComment(
			"Riga (riga prenotazione di origine × postazione × giorno ceduto): il titolare dichiara che " +
			"non userà la postazione quel giorno, che torna prenotabile (assenza pianificata o sharing)."));

		builder.HasKey(x => x.Id).HasName("pk_booking_resale_offers");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, copiato — nessuna FK cross-schema");

		builder.Property(x => x.Origin)
			.HasColumnName("origin")
			.HasColumnType("booking.resale_origin")
			.IsRequired()
			.HasComment("Come è nata l'offerta: assenza pianificata, sharing con credito, o assenza gestita come sharing");

		builder.Property(x => x.SourceReservationId)
			.HasColumnName("source_reservation_id")
			.IsRequired()
			.HasComment("Prenotazione del titolare che cede il giorno");

		builder.Property(x => x.BookingLineId)
			.HasColumnName("booking_line_id")
			.HasComment("Riga postazione ceduta (nullable: offerta a livello prenotazione, raro)");

		builder.Property(x => x.ResourceId)
			.HasColumnName("resource_id")
			.IsRequired()
			.HasComment("public_id di catalog.map_spots (postazione ceduta), copiato — nessuna FK cross-schema");

		builder.Property(x => x.OfferDate)
			.HasColumnName("offer_date")
			.HasColumnType("date")
			.IsRequired()
			.HasComment("Giorno ceduto");

		builder.Property(x => x.OfferedPeriod)
			.HasColumnName("offered_period")
			.HasColumnType("booking.day_period")
			.HasDefaultValueSql("'full_day'")
			.IsRequired()
			.HasComment("Fascia ceduta (full day, mattina, pomeriggio)");

		builder.Property(x => x.RemainingPeriod)
			.HasColumnName("remaining_period")
			.HasColumnType("booking.day_period")
			.HasComment("Fascia ancora disponibile dopo una ripresa parziale; NULL se completamente ripresa o se offered_period non è full_day");

		builder.Property(x => x.OfferState)
			.HasColumnName("offer_state")
			.HasColumnType("booking.resale_offer_state")
			.HasDefaultValueSql("'available'")
			.IsRequired()
			.HasComment("Stato dell'offerta: disponibile, ripreso da un'altra prenotazione, ritirato dal titolare");

		builder.Property(x => x.TakenByReservationId)
			.HasColumnName("taken_by_reservation_id")
			.HasComment("Prenotazione che ha ripreso il giorno ceduto (valorizzato quando offer_state = reassigned)");

		builder.Property(x => x.TakenAt)
			.HasColumnName("taken_at")
			.HasComment("Data/ora della ripresa del giorno");

		builder.Property(x => x.CreditSettled)
			.HasColumnName("credit_settled")
			.HasDefaultValue(false)
			.IsRequired()
			.HasComment("True se il credito per questa ripresa è stato accreditato al titolare (solo origin di tipo sharing)");

		builder.Property(x => x.CreatedAt)
			.HasColumnName("created_at")
			.HasDefaultValueSql("now()")
			.IsRequired()
			.HasComment("Data/ora di creazione dell'offerta");

		builder.Property(x => x.UpdatedAt)
			.HasColumnName("updated_at")
			.HasDefaultValueSql("now()")
			.IsRequired()
			.HasComment("Data/ora di ultima modifica dell'offerta");

		builder.Property(x => x.DeletedAt)
			.HasColumnName("deleted_at")
			.HasComment("Soft-delete dell'offerta");

		builder.HasIndex(x => new { x.PropertyId, x.ResourceId, x.OfferDate })
			.HasFilter("offer_state = 'available' AND deleted_at IS NULL")
			.HasDatabaseName("ix_booking_resale_offers_availability");

		builder.HasIndex(x => x.BookingLineId)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_booking_resale_offers_origin_line");

		builder.HasIndex(x => x.TakenByReservationId)
			.HasFilter("taken_by_reservation_id IS NOT NULL")
			.HasDatabaseName("ix_booking_resale_offers_taken_by");

		builder.HasIndex(x => x.PropertyId)
			.HasFilter("offer_state = 'reassigned' AND credit_settled = false AND origin = 'sharing' AND deleted_at IS NULL")
			.HasDatabaseName("ix_booking_resale_offers_credit_pending");

		builder.HasOne(x => x.SourceReservation)
			.WithMany()
			.HasForeignKey(x => x.SourceReservationId)
			.HasConstraintName("fk_booking_resale_offers_source_reservation")
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasOne(x => x.BookingLine)
			.WithMany()
			.HasForeignKey(x => x.BookingLineId)
			.HasConstraintName("fk_booking_resale_offers_booking_line")
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasOne(x => x.TakenByReservation)
			.WithMany()
			.HasForeignKey(x => x.TakenByReservationId)
			.HasConstraintName("fk_booking_resale_offers_taken_by_reservation")
			.OnDelete(DeleteBehavior.SetNull);
	}
}
