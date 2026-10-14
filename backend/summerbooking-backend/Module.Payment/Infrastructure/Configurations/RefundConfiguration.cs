using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Payment.Domain.Entities;
using Module.Payment.Domain.Enums;

namespace Module.Payment.Infrastructure.Configurations;

/// <summary>Mapping EF Core di <see cref="Refund"/> su payment.transaction_refunds.</summary>
public sealed class RefundConfiguration : IEntityTypeConfiguration<Refund>
{
	public void Configure(EntityTypeBuilder<Refund> builder)
	{
		builder.ToTable("transaction_refunds", "payment", t =>
		{
			t.HasComment("Un rimborso Stripe su una transazione online, con supporto al rimborso parziale.");
			t.HasCheckConstraint("ck_payment_transaction_refunds_amount", "amount > 0 AND amount <= original_amount");
		});

		builder.HasKey(x => x.Id).HasName("pk_payment_transaction_refunds");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId)
			.HasColumnName("public_id")
			.ValueGeneratedNever()
			.IsRequired()
			.HasComment("Id pubblico stabile, per il gestionale e per eventuale riferimento da C4/C6.");

		builder.Property(x => x.ChargeId)
			.HasColumnName("charge_id")
			.IsRequired()
			.HasComment("Transazione online rimborsata.");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, copiato dalla transazione — nessuna FK cross-schema; denormalizzato per indici ed export.");

		builder.Property(x => x.ReservationPublicId)
			.HasColumnName("reservation_public_id")
			.HasComment("public_id di booking.booking_reservations, copiato — nessuna FK cross-schema; prenotazione a cui il rimborso si riferisce.");

		builder.Property(x => x.Status)
			.HasColumnName("status")
			.HasColumnType("payment.refund_status")
			.HasDefaultValueSql("'pending'")
			.IsRequired()
			.HasComment("Esito del rimborso.");

		builder.Property(x => x.Reason)
			.HasColumnName("reason")
			.HasColumnType("payment.refund_reason")
			.HasDefaultValueSql("'booking_canceled'")
			.IsRequired()
			.HasComment("Motivo del rimborso, per report e per il parametro reason di Stripe.");

		builder.Property(x => x.Amount)
			.HasColumnName("amount")
			.HasColumnType("numeric(12,2)")
			.IsRequired()
			.HasComment("Importo rimborsato in euro; può essere inferiore a original_amount (rimborso parziale).");

		builder.Property(x => x.OriginalAmount)
			.HasColumnName("original_amount")
			.HasColumnType("numeric(12,2)")
			.IsRequired()
			.HasComment("Importo pieno della transazione originaria al momento del rimborso (snapshot).");

		builder.Property(x => x.Currency)
			.HasColumnName("currency")
			.HasColumnType("char(3)")
			.HasDefaultValue("EUR")
			.IsRequired()
			.HasComment("Valuta ISO 4217, allineata alla transazione.");

		builder.Property(x => x.StripeRefundId)
			.HasColumnName("stripe_refund_id")
			.HasMaxLength(40)
			.HasComment("Id dell'oggetto Refund Stripe (re_…), verbatim; correla al webhook charge.refunded.");

		builder.Property(x => x.FailureReason)
			.HasColumnName("failure_reason")
			.HasMaxLength(255)
			.HasComment("Messaggio/codice di fallimento Stripe quando status = failed.");

		builder.Property(x => x.RequestedByUserId)
			.HasColumnName("requested_by_user_id")
			.HasComment("public_id di identity.access_users, copiato — nessuna FK cross-schema; operatore che ha disposto il rimborso (audit fiscale); NULL se automatico.");

		builder.Property(x => x.OccurredAt)
			.HasColumnName("occurred_at")
			.HasDefaultValueSql("now()")
			.IsRequired()
			.HasComment("Data/ora del rimborso, decorrenza retention.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at")
			.HasComment("Esposto per uniformità: append-only (scrittura contabile), sempre NULL.");

		builder.HasIndex(x => x.PublicId)
			.IsUnique()
			.HasDatabaseName("uq_payment_transaction_refunds_public_id");

		builder.HasIndex(x => x.StripeRefundId)
			.IsUnique()
			.HasFilter("stripe_refund_id IS NOT NULL")
			.HasDatabaseName("uq_payment_transaction_refunds_stripe_refund");

		builder.HasIndex(x => x.ChargeId)
			.HasDatabaseName("ix_payment_transaction_refunds_charge");

		builder.HasIndex(x => new { x.PropertyId, x.OccurredAt })
			.HasDatabaseName("ix_payment_transaction_refunds_property_date");

		builder.HasIndex(x => x.Status)
			.HasFilter("status = 'pending'")
			.HasDatabaseName("ix_payment_transaction_refunds_pending");

		builder.HasOne(x => x.Charge)
			.WithMany(c => c.Refunds)
			.HasForeignKey(x => x.ChargeId)
			.HasConstraintName("fk_payment_transaction_refunds_charge")
			.OnDelete(DeleteBehavior.Restrict);
	}
}
