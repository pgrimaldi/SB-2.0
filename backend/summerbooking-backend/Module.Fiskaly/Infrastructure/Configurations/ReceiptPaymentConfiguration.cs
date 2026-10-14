using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Fiskaly.Domain.Entities;

namespace Module.Fiskaly.Infrastructure.Configurations;

public sealed class ReceiptPaymentConfiguration : IEntityTypeConfiguration<ReceiptPayment>
{
	public void Configure(EntityTypeBuilder<ReceiptPayment> builder)
	{
		builder.ToTable("receipt_payments", "fiskaly", t =>
		{
			t.HasComment("Modalità di pagamento con cui è stato incassato l'importo del documento commerciale, congelata all'emissione.");
			t.HasCheckConstraint("ck_fiskaly_receipt_payments_card_kind", "payment_type = 'card' OR card_kind IS NULL");
			t.HasCheckConstraint("ck_fiskaly_receipt_payments_voucher_kind", "payment_type = 'voucher' OR voucher_kind IS NULL");
			t.HasCheckConstraint("ck_fiskaly_receipt_payments_outstanding_concept", "payment_type = 'outstanding' OR outstanding_concept IS NULL");
		});

		builder.HasKey(x => x.Id).HasName("pk_fiskaly_receipt_payments");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.ReceiptId)
			.HasColumnName("receipt_id")
			.IsRequired()
			.HasComment("Documento a cui la riga pagamento appartiene.");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, copiato — nessuna FK cross-schema. Denormalizzato per i riepiloghi senza join.");
		builder.HasIndex(x => new { x.PropertyId, x.PaymentType })
			.HasDatabaseName("ix_fiskaly_receipt_payments_type");

		builder.Property(x => x.PaymentNo)
			.HasColumnName("payment_no")
			.HasColumnType("smallint")
			.IsRequired()
			.HasComment("Ordine della riga pagamento nel documento (1-based).");
		builder.HasIndex(x => new { x.ReceiptId, x.PaymentNo })
			.IsUnique()
			.HasDatabaseName("ix_fiskaly_receipt_payments_receipt");

		builder.Property(x => x.PaymentType)
			.HasColumnName("payment_type")
			.HasColumnType("fiskaly.payment_type")
			.IsRequired()
			.HasComment("Forma di pagamento normalizzata a un tipo valido Fiskaly.");

		builder.Property(x => x.Amount)
			.HasColumnName("amount")
			.HasColumnType("numeric(12,2)")
			.IsRequired()
			.HasComment("Importo lordo incassato con questa forma di pagamento.");

		builder.Property(x => x.DiscountAmount)
			.HasColumnName("discount_amount")
			.HasColumnType("numeric(12,2)")
			.IsRequired()
			.HasDefaultValue(0m)
			.HasComment("Sconto applicato su questa forma di pagamento.");

		builder.Property(x => x.Label)
			.HasColumnName("label")
			.HasMaxLength(120)
			.HasComment("Etichetta descrittiva libera (es. per online/other) o valore originale del drift legacy, testo congelato.");

		builder.Property(x => x.CardKind)
			.HasColumnName("card_kind")
			.HasColumnType("fiskaly.card_kind")
			.HasComment("Tipo carta, valorizzato solo se payment_type = card.");

		builder.Property(x => x.CardNumberMasked)
			.HasColumnName("card_number_masked")
			.HasMaxLength(20)
			.HasComment("Ultime cifre / maschera della carta — mai il PAN completo.");

		builder.Property(x => x.VoucherKind)
			.HasColumnName("voucher_kind")
			.HasColumnType("fiskaly.voucher_kind")
			.HasComment("Tipo voucher, valorizzato solo se payment_type = voucher.");

		builder.Property(x => x.OutstandingConcept)
			.HasColumnName("outstanding_concept")
			.HasColumnType("fiskaly.outstanding_concept")
			.HasComment("Concetto dell'insoluto, valorizzato solo se payment_type = outstanding.");

		builder.Property(x => x.LedgerEntryPublicId)
			.HasColumnName("ledger_entry_public_id")
			.HasComment("public_id di payment.ledger_entries, copiato — nessuna FK cross-schema. Movimento di incasso corrispondente nel libro incassi C3.");
		builder.HasIndex(x => x.LedgerEntryPublicId)
			.HasFilter("ledger_entry_public_id IS NOT NULL")
			.HasDatabaseName("ix_fiskaly_receipt_payments_ledger");

		builder.Property(x => x.CreatedAt)
			.HasColumnName("created_at")
			.IsRequired()
			.HasDefaultValueSql("now()")
			.HasComment("Creazione della riga.");

		builder.Property(x => x.UpdatedAt)
			.HasColumnName("updated_at")
			.IsRequired()
			.HasDefaultValueSql("now()")
			.HasComment("Ultima modifica della riga.");

		builder.Property(x => x.DeletedAt)
			.HasColumnName("deleted_at")
			.HasComment("Esposto per uniformità ma non usato: append-only con la testata.");
	}
}
