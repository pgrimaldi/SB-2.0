using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Fiskaly.Domain.Entities;
using Module.Fiskaly.Domain.Enums;

namespace Module.Fiskaly.Infrastructure.Configurations;

public sealed class ReceiptConfiguration : IEntityTypeConfiguration<Receipt>
{
	public void Configure(EntityTypeBuilder<Receipt> builder)
	{
		builder.ToTable("receipts", "fiskaly", t =>
		{
			t.HasComment("Documento commerciale emesso (fiscale o non fiscale) per una prenotazione — testata unica, fatto fiscale storico immutabile.");
			t.HasCheckConstraint("ck_fiskaly_receipts_fiscal_cash_register",
				"document_kind = 'non_fiscal' OR cash_register_id IS NOT NULL");
			t.HasCheckConstraint("ck_fiskaly_receipts_fiscal_account",
				"document_kind = 'non_fiscal' OR fiskaly_account_id IS NOT NULL");
			t.HasCheckConstraint("ck_fiskaly_receipts_amounts",
				"round(taxable_amount + vat_amount, 2) = total_amount");
			t.HasCheckConstraint("ck_fiskaly_receipts_autoreceipt_no_user",
				"source = 'manual' OR created_by_user_id IS NULL");
		});

		builder.HasKey(x => x.Id).HasName("pk_fiskaly_receipts");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId)
			.HasColumnName("public_id")
			.ValueGeneratedNever()
			.IsRequired()
			.HasComment("Id pubblico stabile (UUIDv7); usato in URL gestionale e copiato cross-modulo da C3/C6.");
		builder.HasIndex(x => x.PublicId).IsUnique().HasDatabaseName("uq_fiskaly_receipts_public_id");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, copiato — nessuna FK cross-schema. Stabilimento emittente.");
		builder.HasIndex(x => new { x.PropertyId, x.CreatedAt })
			.HasDatabaseName("ix_fiskaly_receipts_property_date");
		builder.HasIndex(x => new { x.PropertyId, x.DocumentKind, x.RecordState })
			.HasDatabaseName("ix_fiskaly_receipts_flow");
		builder.HasIndex(x => new { x.PropertyId, x.SignedAt })
			.HasFilter("record_state = 'completed' AND record_mode = 'finished'")
			.HasDatabaseName("ix_fiskaly_receipts_completed");

		builder.Property(x => x.DocumentKind)
			.HasColumnName("document_kind")
			.HasColumnType("fiskaly.receipt_kind")
			.IsRequired()
			.HasComment("Discriminatore di testata: fiscal (firmato SIGN IT) oppure non_fiscal (pre-conto/proforma).");

		builder.Property(x => x.Source)
			.HasColumnName("source")
			.HasColumnType("fiskaly.receipt_source")
			.IsRequired()
			.HasDefaultValueSql("'manual'")
			.HasComment("Origine dell'emissione: manual (operatore) oppure autoreceipt (post-pagamento Stripe).");

		builder.Property(x => x.FiskalyAccountId)
			.HasColumnName("fiskaly_account_id")
			.HasComment("Account fiscale sotto cui è stato emesso il documento.");
		builder.HasOne<FiskalyAccount>()
			.WithMany()
			.HasForeignKey(x => x.FiskalyAccountId)
			.HasConstraintName("fk_fiskaly_receipts_account")
			.OnDelete(DeleteBehavior.Restrict);

		builder.Property(x => x.CashRegisterId)
			.HasColumnName("cash_register_id")
			.HasComment("Registratore di cassa che ha firmato il documento.");
		builder.HasOne<CashRegister>()
			.WithMany()
			.HasForeignKey(x => x.CashRegisterId)
			.HasConstraintName("fk_fiskaly_receipts_cash_register")
			.OnDelete(DeleteBehavior.Restrict);
		builder.HasIndex(x => new { x.CashRegisterId, x.SignedAt })
			.HasFilter("document_kind = 'fiscal'")
			.HasDatabaseName("ix_fiskaly_receipts_cash_register");

		builder.Property(x => x.FiskalySystemIdSnapshot)
			.HasColumnName("fiskaly_system_id_snapshot")
			.HasMaxLength(36)
			.HasComment("Id System Fiskaly congelato al momento della firma — snapshot accanto alla FK viva cash_register_id.");

		builder.Property(x => x.DocumentNumber)
			.HasColumnName("document_number")
			.HasComment("Numero progressivo fiscale assegnato dalla sequenza giornaliera della cassa; non univoco (riparte ogni giorno/cassa).");

		builder.Property(x => x.RecordId)
			.HasColumnName("record_id")
			.HasMaxLength(36)
			.HasComment("Id del record TRANSACTION::RECEIPT su Fiskaly, referenziato dalla CANCELLATION.");
		builder.HasIndex(x => x.RecordId)
			.IsUnique()
			.HasFilter("record_id IS NOT NULL")
			.HasDatabaseName("uq_fiskaly_receipts_record");

		builder.Property(x => x.IntentionRecordId)
			.HasColumnName("intention_record_id")
			.HasMaxLength(36)
			.HasComment("Id del record INTENTION consumato dalla TRANSACTION di emissione.");

		builder.Property(x => x.IntentionCancellationRecordId)
			.HasColumnName("intention_cancellation_record_id")
			.HasMaxLength(36)
			.HasComment("Id del record INTENTION consumato dalla TRANSACTION di annullamento.");

		builder.Property(x => x.RecordState)
			.HasColumnName("record_state")
			.HasColumnType("fiskaly.receipt_record_state")
			.IsRequired()
			.HasComment("Stato interno del flusso di emissione/annullamento/generazione.");

		builder.Property(x => x.FiskalyStateRaw)
			.HasColumnName("fiskaly_state_raw")
			.HasMaxLength(40)
			.HasComment("Stato grezzo del record TRANSACTION restituito da SIGN IT, verbatim, per diagnostica.");

		builder.Property(x => x.RecordMode)
			.HasColumnName("record_mode")
			.HasColumnType("fiskaly.record_mode")
			.HasComment("Mode del record TRANSACTION Fiskaly.");

		builder.Property(x => x.SignedAt)
			.HasColumnName("signed_at")
			.HasComment("Istante di firma; determina il giorno fiscale della sequenza numerica e la data documento sul PDF.");

		builder.Property(x => x.Currency)
			.HasColumnName("currency")
			.HasColumnType("char(3)")
			.IsRequired()
			.HasDefaultValue("EUR")
			.HasComment("Valuta ISO 4217 del documento.");

		builder.Property(x => x.LanguageCode)
			.HasColumnName("language_code")
			.HasColumnType("fiskaly.language_code")
			.IsRequired()
			.HasDefaultValueSql("'it'")
			.HasComment("Lingua del PDF / eReceipt. Vocabolario concettualmente SharedKernel, registrato localmente in fiskaly finché un secondo modulo non lo userà davvero (vedi Domain/Enums/LanguageCode.cs).");

		builder.Property(x => x.TotalAmount)
			.HasColumnName("total_amount")
			.HasColumnType("numeric(12,2)")
			.IsRequired()
			.HasComment("Totale del documento IVA inclusa, sconto già applicato — quanto il cliente paga con questo scontrino.");

		builder.Property(x => x.TaxableAmount)
			.HasColumnName("taxable_amount")
			.HasColumnType("numeric(12,2)")
			.IsRequired()
			.HasComment("Imponibile del documento, al netto dello sconto.");

		builder.Property(x => x.VatAmount)
			.HasColumnName("vat_amount")
			.HasColumnType("numeric(12,2)")
			.IsRequired()
			.HasComment("Totale IVA del documento.");

		builder.Property(x => x.DiscountAmount)
			.HasColumnName("discount_amount")
			.HasColumnType("numeric(12,2)")
			.IsRequired()
			.HasDefaultValue(0m)
			.HasComment("Sconto totale applicato al documento, già scontato da total_amount.");

		builder.Property(x => x.InvoicedPaymentAmount)
			.HasColumnName("invoiced_payment_amount")
			.HasColumnType("numeric(12,2)")
			.HasComment("Importo dell'incasso specifico che questo scontrino ha fatturato.");

		builder.Property(x => x.InvoicedPaymentAt)
			.HasColumnName("invoiced_payment_at")
			.HasComment("Data dell'incasso fatturato da questo scontrino.");

		builder.Property(x => x.InvoicedPaymentMode)
			.HasColumnName("invoiced_payment_mode")
			.HasColumnType("fiskaly.payment_mode")
			.HasComment("Forma dell'incasso fatturato da questo scontrino (vocabolario condiviso con booking/payment).");

		builder.Property(x => x.SellerLegalNameSnapshot)
			.HasColumnName("seller_legal_name_snapshot")
			.HasMaxLength(128)
			.HasComment("Ragione sociale del venditore congelata al momento dell'emissione.");

		builder.Property(x => x.SellerTradeNameSnapshot)
			.HasColumnName("seller_trade_name_snapshot")
			.HasMaxLength(128)
			.HasComment("Nome commerciale del venditore congelato al momento dell'emissione.");

		builder.Property(x => x.SellerVatNumberSnapshot)
			.HasColumnName("seller_vat_number_snapshot")
			.HasMaxLength(128)
			.HasComment("Partita IVA del venditore congelata al momento dell'emissione.");

		builder.Property(x => x.SellerFiscalCodeSnapshot)
			.HasColumnName("seller_fiscal_code_snapshot")
			.HasMaxLength(16)
			.HasComment("Codice fiscale del venditore congelato al momento dell'emissione.");

		builder.Property(x => x.SellerAddressSnapshot)
			.HasColumnName("seller_address_snapshot")
			.HasMaxLength(300)
			.HasComment("Indirizzo del venditore congelato e già formattato per il documento al momento dell'emissione.");

		builder.Property(x => x.ReservationPublicId)
			.HasColumnName("reservation_public_id")
			.HasComment("public_id di booking.reservations, copiato — nessuna FK cross-schema. Prenotazione fatturata.");
		builder.HasIndex(x => x.ReservationPublicId).HasDatabaseName("ix_fiskaly_receipts_reservation");

		builder.Property(x => x.BookingNumber)
			.HasColumnName("booking_number")
			.HasMaxLength(32)
			.IsRequired()
			.HasComment("Numero prenotazione — business key denormalizzata per autorizzazioni e invio senza join.");
		builder.HasIndex(x => x.BookingNumber).HasDatabaseName("ix_fiskaly_receipts_booking_number");

		builder.Property(x => x.PdfAssetId)
			.HasColumnName("pdf_asset_id")
			.HasComment("Riferimento al PDF eReceipt del documento.");
		builder.HasOne<MediaAsset>()
			.WithMany()
			.HasForeignKey(x => x.PdfAssetId)
			.HasConstraintName("fk_fiskaly_receipts_pdf_asset")
			.OnDelete(DeleteBehavior.Restrict);

		builder.Property(x => x.EReceiptUuid)
			.HasColumnName("e_receipt_uuid")
			.HasMaxLength(36)
			.HasComment("Id del PDF eReceipt, usato come nome file su storage.");
		builder.HasIndex(x => x.EReceiptUuid)
			.IsUnique()
			.HasFilter("e_receipt_uuid IS NOT NULL")
			.HasDatabaseName("uq_fiskaly_receipts_ereceipt_uuid");

		builder.Property(x => x.FiskalyResponse)
			.HasColumnName("fiskaly_response")
			.HasColumnType("jsonb")
			.HasComment("Risposta grezza completa di Fiskaly per il record TRANSACTION — passthrough di sistema esterno, solo audit/diagnostica.");

		builder.Property(x => x.CancelledAt)
			.HasColumnName("cancelled_at")
			.HasComment("Istante di annullamento dello scontrino.");

		builder.Property(x => x.CancelledByUserId)
			.HasColumnName("cancelled_by_user_id")
			.HasComment("public_id di identity.access_users, copiato — nessuna FK cross-schema. Chi ha annullato lo scontrino.");

		builder.Property(x => x.CancellationReason)
			.HasColumnName("cancellation_reason")
			.HasMaxLength(255)
			.HasComment("Motivo dell'annullamento.");

		builder.Property(x => x.CreatedByUserId)
			.HasColumnName("created_by_user_id")
			.HasComment("public_id di identity.access_users, copiato — nessuna FK cross-schema. Operatore che ha emesso il documento; NULL per source=autoreceipt.");

		builder.HasMany(x => x.Lines)
			.WithOne()
			.HasForeignKey(l => l.ReceiptId)
			.HasConstraintName("fk_fiskaly_receipt_lines_receipt")
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasMany(x => x.Payments)
			.WithOne()
			.HasForeignKey(p => p.ReceiptId)
			.HasConstraintName("fk_fiskaly_receipt_payments_receipt")
			.OnDelete(DeleteBehavior.Cascade);

		builder.Property(x => x.CreatedAt)
			.HasColumnName("created_at")
			.IsRequired()
			.HasDefaultValueSql("now()")
			.HasComment("Creazione riga (inizio flusso) / ultimo step. getReceipts filtra per intervallo su questa colonna.");

		builder.Property(x => x.UpdatedAt)
			.HasColumnName("updated_at")
			.IsRequired()
			.HasDefaultValueSql("now()")
			.HasComment("Ultimo step del flusso di emissione/annullamento.");

		builder.Property(x => x.DeletedAt)
			.HasColumnName("deleted_at")
			.HasComment("Esposto per uniformità ma non usato: fatto fiscale, conservazione 10 anni; l'annullamento è record_state = cancelled, non un soft-delete.");
	}
}
