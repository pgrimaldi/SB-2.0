using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Payment.Domain.Entities;
using Module.Payment.Domain.Enums;

namespace Module.Payment.Infrastructure.Configurations;

/// <summary>Mapping EF Core di <see cref="WalletTransaction"/> su payment.wallet_transactions.</summary>
public sealed class WalletTransactionConfiguration : IEntityTypeConfiguration<WalletTransaction>
{
	public void Configure(EntityTypeBuilder<WalletTransaction> builder)
	{
		builder.ToTable("wallet_transactions", "payment", t =>
		{
			t.HasComment("Movimento del borsellino (ricarica o consumo); append-only, il saldo del conto è la somma di queste righe.");
			t.HasCheckConstraint("ck_payment_wallet_transactions_amount", "amount > 0");
		});

		builder.HasKey(x => x.Id).HasName("pk_payment_wallet_transactions");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId)
			.HasColumnName("public_id")
			.ValueGeneratedNever()
			.IsRequired()
			.HasComment("Id pubblico stabile, copiato da C1 (riga servizio pagata col borsellino) e da C4 (pagamento VOUCHER/OUTSTANDING dello scontrino).");

		builder.Property(x => x.WalletId)
			.HasColumnName("wallet_id")
			.IsRequired()
			.HasComment("Conto borsellino movimentato.");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, copiato dal conto — nessuna FK cross-schema; denormalizzato per gli indici.");

		builder.Property(x => x.CustomerId)
			.HasColumnName("customer_id")
			.IsRequired()
			.HasComment("public_id di identity.customer_customers, copiato dal conto — nessuna FK cross-schema; denormalizzato per gli indici.");

		builder.Property(x => x.MovementType)
			.HasColumnName("movement_type")
			.HasColumnType("payment.wallet_movement_type")
			.IsRequired()
			.HasComment("Verso del movimento: credit incrementa balance/lifetime_credited, debit li decrementa.");

		builder.Property(x => x.Source)
			.HasColumnName("source")
			.HasColumnType("payment.wallet_movement_source")
			.HasDefaultValueSql("'top_up'")
			.IsRequired()
			.HasComment("Causale del movimento (ricarica, consumo, accredito sharing, saldo prenotazione, storno, rettifica, scadenza).");

		builder.Property(x => x.Amount)
			.HasColumnName("amount")
			.HasColumnType("numeric(12,2)")
			.IsRequired()
			.HasComment("Importo del movimento, sempre positivo; il verso lo dà movement_type.");

		builder.Property(x => x.Currency)
			.HasColumnName("currency")
			.HasColumnType("char(3)")
			.HasDefaultValue("EUR")
			.IsRequired()
			.HasComment("Valuta ISO 4217, allineata al conto.");

		builder.Property(x => x.BalanceAfter)
			.HasColumnName("balance_after")
			.HasColumnType("numeric(12,2)")
			.IsRequired()
			.HasComment("Saldo del conto dopo questo movimento (running balance), per l'estratto conto e la verifica di integrità.");

		builder.Property(x => x.ReservationPublicId)
			.HasColumnName("reservation_public_id")
			.HasComment("public_id di booking.booking_reservations, copiato — nessuna FK cross-schema; prenotazione a cui il movimento è collegato.");

		builder.Property(x => x.ResaleOfferRef)
			.HasColumnName("resale_offer_ref")
			.HasComment("public_id di booking.booking_resale_offers, copiato — nessuna FK cross-schema; valorizzato per source = ResaleCreditEarned.");

		builder.Property(x => x.ReversesTransactionId)
			.HasColumnName("reverses_transaction_id")
			.HasComment("Movimento stornato, per source = Reversal.");

		builder.Property(x => x.LedgerEntryRef)
			.HasColumnName("ledger_entry_ref")
			.HasComment("public_id di payment.ledger_entries — correlazione debole (non FK EF) verso la riga contabile generata quando il movimento salda una prenotazione: valorizzata a posteriori dall'handler di PaymentRecorded, non in un'unica transazione con l'inserimento del movimento.");

		builder.Property(x => x.OperatedByUserId)
			.HasColumnName("operated_by_user_id")
			.HasComment("public_id di identity.access_users, copiato — nessuna FK cross-schema; operatore che ha registrato il movimento (audit).");

		builder.Property(x => x.OccurredAt)
			.HasColumnName("occurred_at")
			.HasDefaultValueSql("now()")
			.IsRequired()
			.HasComment("Data/ora del movimento, decorrenza retention.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at")
			.HasComment("Esposto per uniformità: append-only (una correzione è un movimento reversal, sempre NULL).");

		builder.HasIndex(x => x.PublicId)
			.IsUnique()
			.HasDatabaseName("uq_payment_wallet_transactions_public_id");

		builder.HasIndex(x => new { x.WalletId, x.OccurredAt })
			.HasDatabaseName("ix_payment_wallet_transactions_wallet_date");

		builder.HasIndex(x => x.ReservationPublicId)
			.HasFilter("reservation_public_id IS NOT NULL")
			.HasDatabaseName("ix_payment_wallet_transactions_reservation");

		builder.HasIndex(x => new { x.PropertyId, x.OccurredAt })
			.HasDatabaseName("ix_payment_wallet_transactions_property_date");

		builder.HasOne(x => x.Wallet)
			.WithMany(w => w.Transactions)
			.HasForeignKey(x => x.WalletId)
			.HasConstraintName("fk_payment_wallet_transactions_wallet")
			.OnDelete(DeleteBehavior.Restrict);

		builder.HasOne(x => x.ReversesTransaction)
			.WithMany()
			.HasForeignKey(x => x.ReversesTransactionId)
			.HasConstraintName("fk_payment_wallet_transactions_reverses_transaction")
			.OnDelete(DeleteBehavior.Restrict);

		builder.OwnsMany(x => x.Items, items => WalletTransactionItemConfiguration.Configure(items));
	}
}
