using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Booking.Domain.Entities;
using Module.Booking.Domain.Enums;

namespace Module.Booking.Infrastructure.Configurations;

public sealed class ResaleCreditLedgerEntryConfiguration : IEntityTypeConfiguration<ResaleCreditLedgerEntry>
{
	public void Configure(EntityTypeBuilder<ResaleCreditLedgerEntry> builder)
	{
		builder.ToTable("resale_credit_ledger", "booking", t => t.HasComment(
			"Ledger append-only dei crediti sharing maturati dal titolare quando un giorno ceduto viene " +
			"ripreso da un altro cliente; il saldo è la somma delle righe per cliente."));

		builder.HasKey(x => x.Id).HasName("pk_booking_resale_credit_ledger");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, copiato — nessuna FK cross-schema");

		builder.Property(x => x.CustomerId)
			.HasColumnName("customer_id")
			.IsRequired()
			.HasComment("public_id di identity.customer_customers (cliente titolare a cui il credito è maturato), copiato — nessuna FK cross-schema");

		builder.Property(x => x.ResaleOfferId)
			.HasColumnName("resale_offer_id")
			.HasComment("Offerta/ripresa che ha generato il credito (nullable: aggiustamenti manuali senza offerta)");

		builder.Property(x => x.SourceReservationId)
			.HasColumnName("source_reservation_id")
			.HasComment("Prenotazione del titolare di cui è stato ceduto il giorno");

		builder.Property(x => x.EntryType)
			.HasColumnName("entry_type")
			.HasColumnType("booking.resale_credit_entry")
			.HasDefaultValueSql("'earned'")
			.IsRequired()
			.HasComment("Tipo di movimento: accredito per ripresa, storno di una ripresa annullata, correzione manuale");

		builder.Property(x => x.CreditUnits)
			.HasColumnName("credit_units")
			.HasDefaultValue(0)
			.IsRequired()
			.HasComment("Numero di crediti (unità simboliche) maturati per il movimento");

		builder.Property(x => x.Amount)
			.HasColumnName("amount")
			.HasColumnType("numeric(12,2)")
			.HasDefaultValue(0m)
			.IsRequired()
			.HasComment("Valore economico del credito: segno positivo per earned, negativo per reversed");

		builder.Property(x => x.OccurredAt)
			.HasColumnName("occurred_at")
			.HasDefaultValueSql("now()")
			.IsRequired()
			.HasComment("Data/ora in cui il movimento è avvenuto");

		builder.Property(x => x.WalletEventEmitted)
			.HasColumnName("wallet_event_emitted")
			.HasDefaultValue(false)
			.IsRequired()
			.HasComment("True quando l'evento ResaleCreditEarned è stato consegnato a Payment (C3) per la riconciliazione col borsellino");

		builder.Property(x => x.Note)
			.HasColumnName("note")
			.HasMaxLength(255)
			.HasComment("Nota libera, valorizzata per i movimenti di tipo adjustment");

		builder.Property(x => x.CreatedAt)
			.HasColumnName("created_at")
			.HasDefaultValueSql("now()")
			.IsRequired()
			.HasComment("Data/ora di creazione della riga di ledger");

		builder.Property(x => x.UpdatedAt)
			.HasColumnName("updated_at")
			.HasDefaultValueSql("now()")
			.IsRequired()
			.HasComment("Data/ora di ultima modifica della riga di ledger");

		builder.Property(x => x.DeletedAt)
			.HasColumnName("deleted_at")
			.HasComment("Presente per uniformità di schema, non usato: il ledger è append-only, le correzioni sono righe reversed/adjustment");

		builder.HasIndex(x => new { x.PropertyId, x.CustomerId, x.OccurredAt })
			.HasDatabaseName("ix_booking_resale_credit_ledger_customer")
			.IsDescending(false, false, true);

		// Overload con nome: due HasIndex sulle stesse colonne senza nome sono LO STESSO indice per EF,
		// e il secondo ne sovrascrive nome, filtro e unicità. Fino al 2026-10-06 a DB ne esisteva uno solo.
		builder.HasIndex(x => x.ResaleOfferId, "ix_booking_resale_credit_ledger_offer")
			.HasDatabaseName("ix_booking_resale_credit_ledger_offer");

		builder.HasIndex(x => x.ResaleOfferId, "uq_booking_resale_credit_ledger_offer_entry")
			.IsUnique()
			.HasFilter("entry_type = 'earned'")
			.HasDatabaseName("uq_booking_resale_credit_ledger_offer_entry");

		builder.HasOne(x => x.ResaleOffer)
			.WithMany()
			.HasForeignKey(x => x.ResaleOfferId)
			.HasConstraintName("fk_booking_resale_credit_ledger_resale_offer")
			.OnDelete(DeleteBehavior.SetNull);

		builder.HasOne(x => x.SourceReservation)
			.WithMany()
			.HasForeignKey(x => x.SourceReservationId)
			.HasConstraintName("fk_booking_resale_credit_ledger_source_reservation")
			.OnDelete(DeleteBehavior.SetNull);
	}
}
