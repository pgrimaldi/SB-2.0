using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Payment.Domain.Entities;
using Module.Payment.Domain.Enums;

namespace Module.Payment.Infrastructure.Configurations;

/// <summary>Mapping EF Core di <see cref="PaymentRequestLink"/> su payment.request_links.</summary>
public sealed class PaymentRequestLinkConfiguration : IEntityTypeConfiguration<PaymentRequestLink>
{
	public void Configure(EntityTypeBuilder<PaymentRequestLink> builder)
	{
		builder.ToTable("request_links", "payment", t =>
		{
			t.HasComment("Link di pagamento Stripe inviato al cliente per saldare o confermare una prenotazione.");
			t.HasCheckConstraint("ck_payment_request_links_amount", "amount > 0");
		});

		builder.HasKey(x => x.Id).HasName("pk_payment_request_links");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId)
			.HasColumnName("public_id")
			.ValueGeneratedNever()
			.IsRequired()
			.HasComment("Id pubblico stabile, esposto nell'URL post-pagamento e nel messaggio al cliente.");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, copiato — nessuna FK cross-schema; stabilimento.");

		builder.Property(x => x.ReservationPublicId)
			.HasColumnName("reservation_public_id")
			.HasComment("public_id di booking.booking_reservations, copiato — nessuna FK cross-schema; il link può precedere la prenotazione.");

		builder.Property(x => x.Status)
			.HasColumnName("status")
			.HasColumnType("payment.payment_request_status")
			.HasDefaultValueSql("'pending'")
			.IsRequired()
			.HasComment("Stato del link: creato, pagato, revocato, scaduto o fallito.");

		builder.Property(x => x.Origin)
			.HasColumnName("origin")
			.HasColumnType("payment.payment_request_origin")
			.HasDefaultValueSql("'booking_detail'")
			.IsRequired()
			.HasComment("Da dove è stato generato il link, cambia testi/redirect.");

		builder.Property(x => x.Amount)
			.HasColumnName("amount")
			.HasColumnType("numeric(12,2)")
			.IsRequired()
			.HasComment("Importo richiesto dal link.");

		builder.Property(x => x.Currency)
			.HasColumnName("currency")
			.HasColumnType("char(3)")
			.HasDefaultValue("EUR")
			.IsRequired()
			.HasComment("Valuta ISO 4217.");

		builder.Property(x => x.CancelReservationOnExpiry)
			.HasColumnName("cancel_reservation_on_expiry")
			.HasDefaultValue(false)
			.IsRequired()
			.HasComment("Se true, alla scadenza/annullamento del link va cancellata anche la prenotazione associata.");

		builder.Property(x => x.StripePaymentLinkId)
			.HasColumnName("stripe_payment_link_id")
			.HasMaxLength(40)
			.HasComment("Id del Payment Link / Checkout Session Stripe, valorizzato appena Stripe risponde.");

		builder.Property(x => x.ChargeId)
			.HasColumnName("charge_id")
			.HasComment("Transazione generata dal pagamento del link, valorizzata a status = paid.");

		builder.Property(x => x.ExpiresAt)
			.HasColumnName("expires_at")
			.HasComment("Scadenza del link; NULL se senza scadenza.");

		builder.Property(x => x.PaidAt)
			.HasColumnName("paid_at")
			.HasComment("Quando il link è stato pagato.");

		builder.Property(x => x.CanceledAt)
			.HasColumnName("canceled_at")
			.HasComment("Quando il link è stato revocato.");

		builder.Property(x => x.CreatedByUserId)
			.HasColumnName("created_by_user_id")
			.HasComment("public_id di identity.access_users, copiato — nessuna FK cross-schema; operatore che ha generato il link.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at")
			.HasComment("Esposto per uniformità: il fatto segue la transazione (retention 10 anni).");

		builder.HasIndex(x => x.PublicId)
			.IsUnique()
			.HasDatabaseName("uq_payment_request_links_public_id");

		builder.HasIndex(x => x.StripePaymentLinkId)
			.IsUnique()
			.HasFilter("stripe_payment_link_id IS NOT NULL")
			.HasDatabaseName("uq_payment_request_links_stripe_link");

		builder.HasIndex(x => x.ReservationPublicId)
			.HasDatabaseName("ix_payment_request_links_reservation");

		builder.HasIndex(x => new { x.PropertyId, x.Status })
			.HasDatabaseName("ix_payment_request_links_property_status");

		builder.HasIndex(x => x.ExpiresAt)
			.HasFilter("status = 'pending'")
			.HasDatabaseName("ix_payment_request_links_expiry");

		builder.HasOne(x => x.Charge)
			.WithMany()
			.HasForeignKey(x => x.ChargeId)
			.HasConstraintName("fk_payment_request_links_charge")
			.OnDelete(DeleteBehavior.Restrict);
	}
}
