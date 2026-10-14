using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Payment.Domain.Entities;
using Module.Payment.Domain.Enums;

namespace Module.Payment.Infrastructure.Configurations;

/// <summary>Mapping EF Core di <see cref="Charge"/> su payment.transaction_charges.</summary>
public sealed class ChargeConfiguration : IEntityTypeConfiguration<Charge>
{
	public void Configure(EntityTypeBuilder<Charge> builder)
	{
		builder.ToTable("transaction_charges", "payment", t => t.HasComment(
			"Livello gateway dell'incasso online: un PaymentIntent Stripe (o una registrazione paga-in-struttura)."));

		builder.HasKey(x => x.Id).HasName("pk_payment_transaction_charges");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId)
			.HasColumnName("public_id")
			.ValueGeneratedNever()
			.IsRequired()
			.HasComment("Id pubblico stabile, copiato cross-modulo (C4 autoreceipt; URL gestionale).");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, copiato — nessuna FK cross-schema; stabilimento che incassa.");

		builder.Property(x => x.ConnectedAccountId)
			.HasColumnName("connected_account_id")
			.HasComment("Connected account su cui è avvenuto l'incasso; NULL per le righe paga-in-struttura.");

		builder.Property(x => x.StripeAccountIdSnapshot)
			.HasColumnName("stripe_account_id_snapshot")
			.HasMaxLength(32)
			.HasComment("Id acct_… congelato al momento dell'incasso, accanto alla FK viva connected_account_id (snapshot su entità mutabile).");

		builder.Property(x => x.ReservationPublicId)
			.HasColumnName("reservation_public_id")
			.HasComment("public_id di booking.booking_reservations, copiato — nessuna FK cross-schema; prenotazione pagata.");

		builder.Property(x => x.SubjectKind)
			.HasColumnName("subject_kind")
			.HasColumnType("payment.transaction_subject")
			.HasDefaultValueSql("'reservation'")
			.HasComment("Cosa si sta pagando: prenotazione con postazioni o di soli servizi.");

		builder.Property(x => x.PaymentType)
			.HasColumnName("payment_type")
			.HasColumnType("payment.online_payment_type")
			.HasDefaultValueSql("'pay_now'")
			.IsRequired()
			.HasComment("Modalità di pagamento online scelta: tutto subito, acconto+saldo, o paga in struttura.");

		builder.Property(x => x.SalesChannel)
			.HasColumnName("sales_channel")
			.HasColumnType("payment.sales_channel")
			.HasDefaultValueSql("'redirect_checkout'")
			.IsRequired()
			.HasComment("Canale di partenza del pagamento, determina il tasso di commissione applicato.");

		builder.Property(x => x.Status)
			.HasColumnName("status")
			.HasColumnType("payment.charge_status")
			.HasDefaultValueSql("'pending'")
			.IsRequired()
			.HasComment("Stato interno della transazione, derivato dallo stato Stripe.");

		builder.Property(x => x.StripeStatus)
			.HasColumnName("stripe_status")
			.HasMaxLength(40)
			.HasComment("Stato grezzo del PaymentIntent Stripe conservato verbatim, snapshot per diagnostica/riconciliazione.");

		builder.Property(x => x.StripePaymentIntentId)
			.HasColumnName("stripe_payment_intent_id")
			.HasMaxLength(255)
			.HasComment("Id del PaymentIntent Stripe; NULL per paga-in-struttura. 255 (non 40): il legacy ha " +
				"dimostrato che questo campo puo' contenere anche un Checkout Session id (\"cs_live_...\", fino a " +
				"66+ caratteri) invece di un vero PaymentIntent id (\"pi_...\") - bug reale osservato il " +
				"2026-09-15, 190 righe scartate a NULL dalla migrazione perche' il varchar(40) le rifiutava.");

		builder.Property(x => x.StripeChargeId)
			.HasColumnName("stripe_charge_id")
			.HasMaxLength(255)
			.HasComment("Id del Charge Stripe (ch_…) associato al PaymentIntent, usato per i rimborsi. 255: " +
				"stessa famiglia di id Stripe di StripePaymentIntentId sopra, stesso margine per coerenza.");

		builder.Property(x => x.GrossAmount)
			.HasColumnName("gross_amount")
			.HasColumnType("numeric(12,2)")
			.IsRequired()
			.HasComputedColumnSql("paid_amount + pending_amount + hold_amount", stored: true)
			.HasComment("Importo lordo dell'operazione (quanto paga il cliente) — colonna generata " +
				"(paid_amount + pending_amount + hold_amount). Sola lettura: la tabella e' " +
				"append/stato e i rimborsi sono righe di payment_transaction_refunds, non modifiche " +
				"di questa riga.");

		builder.Property(x => x.PaidAmount)
			.HasColumnName("paid_amount")
			.HasColumnType("numeric(12,2)")
			.HasDefaultValue(0m)
			.IsRequired()
			.HasComment("Importo effettivamente incassato (catturato).");

		builder.Property(x => x.HoldAmount)
			.HasColumnName("hold_amount")
			.HasColumnType("numeric(12,2)")
			.HasDefaultValue(0m)
			.IsRequired()
			.HasComment("Importo autorizzato con cattura manuale, fondi bloccati non ancora incassati.");

		builder.Property(x => x.PendingAmount)
			.HasColumnName("pending_amount")
			.HasColumnType("numeric(12,2)")
			.HasDefaultValue(0m)
			.IsRequired()
			.HasComment("Residuo ancora da pagare, senza fondi bloccati.");

		builder.Property(x => x.ApplicationFeeAmount)
			.HasColumnName("application_fee_amount")
			.HasColumnType("numeric(12,2)")
			.HasDefaultValue(0m)
			.IsRequired()
			.HasComment("Commissione di piattaforma trattenuta (Stripe Connect), calcolata server-side.");

		builder.Property(x => x.Currency)
			.HasColumnName("currency")
			.HasColumnType("char(3)")
			.HasDefaultValue("EUR")
			.IsRequired()
			.HasComment("Valuta ISO 4217 dell'operazione.");

		builder.Property(x => x.CustomerPublicId)
			.HasColumnName("customer_public_id")
			.HasComment("public_id di identity.customer_customers, copiato — nessuna FK cross-schema; cliente pagante.");

		builder.Property(x => x.OccurredAt)
			.HasColumnName("occurred_at")
			.HasDefaultValueSql("now()")
			.IsRequired()
			.HasComment("Data/ora dell'evento gateway (creazione PaymentIntent); decorrenza retention.");

		builder.Property(x => x.CapturedAt)
			.HasColumnName("captured_at")
			.HasComment("Quando l'hold è stato catturato.");

		builder.Property(x => x.CanceledAt)
			.HasColumnName("canceled_at")
			.HasComment("Quando la transazione è stata annullata.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at")
			.HasComment("Esposto per uniformità: la tabella è append/stato, non si soft-elimina (scrittura contabile, retention 10 anni).");

		builder.HasIndex(x => x.PublicId)
			.IsUnique()
			.HasDatabaseName("uq_payment_transaction_charges_public_id");

		builder.HasIndex(x => x.StripePaymentIntentId)
			.IsUnique()
			.HasFilter("stripe_payment_intent_id IS NOT NULL")
			.HasDatabaseName("uq_payment_transaction_charges_payment_intent");

		builder.HasIndex(x => x.ReservationPublicId)
			.HasDatabaseName("ix_payment_transaction_charges_reservation");

		builder.HasIndex(x => new { x.PropertyId, x.CreatedAt })
			.HasDatabaseName("ix_payment_transaction_charges_property_date");

		builder.HasIndex(x => new { x.PropertyId, x.Status })
			.HasFilter("status IN ('pending','authorized')")
			.HasDatabaseName("ix_payment_transaction_charges_status");

		builder.HasIndex(x => x.ConnectedAccountId)
			.HasDatabaseName("ix_payment_transaction_charges_connected_account");

		builder.HasOne(x => x.ConnectedAccount)
			.WithMany()
			.HasForeignKey(x => x.ConnectedAccountId)
			.HasConstraintName("fk_payment_transaction_charges_connected_account")
			.OnDelete(DeleteBehavior.Restrict);
	}
}
