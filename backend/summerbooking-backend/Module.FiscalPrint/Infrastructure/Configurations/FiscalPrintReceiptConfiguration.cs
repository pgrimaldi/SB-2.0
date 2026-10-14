using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.FiscalPrint.Domain.Entities;

namespace Module.FiscalPrint.Infrastructure.Configurations;

public sealed class FiscalPrintReceiptConfiguration : IEntityTypeConfiguration<FiscalPrintReceipt>
{
	public void Configure(EntityTypeBuilder<FiscalPrintReceipt> builder)
	{
		builder.ToTable("receipts", "fiscalprint", t => t.HasComment(
			"Scontrino/ricevuta stampato su registratore di cassa RT fisico, con la risposta grezza del dispositivo e le proiezioni derivate per le liste."));

		builder.HasKey(x => x.Id).HasName("pk_fiscalprint_receipts");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId)
			.HasColumnName("public_id")
			.ValueGeneratedNever()
			.IsRequired()
			.HasComment("Id pubblico stabile (UUIDv7) per l'URL del dettaglio gestionale (getReceiptDetails).");
		builder.HasIndex(x => x.PublicId).IsUnique().HasDatabaseName("uq_fiscalprint_receipts_public_id");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties (stabilimento che ha stampato), copiato — nessuna FK cross-schema.");

		builder.Property(x => x.PrinterId)
			.HasColumnName("printer_id")
			.HasComment("Stampante (fiscalprint.printer_configs) usata per la stampa.");

		builder.Property(x => x.ReservationPublicId)
			.HasColumnName("reservation_public_id")
			.HasComment("public_id di booking.reservations, valorizzato solo se lo scontrino è stampato dal dettaglio di una prenotazione — copiato, nessuna FK cross-schema.");

		builder.Property(x => x.BookingNumber)
			.HasColumnName("booking_number")
			.HasMaxLength(32)
			.HasComment("Numero prenotazione (business key denormalizzata da booking.reservations, per comodità del gestionale).");

		builder.Property(x => x.ReceiptNumber)
			.HasColumnName("receipt_number")
			.HasComment("Numero progressivo dello scontrino RT per (printer_id, giorno fiscale), assegnato da fiscalprint.serial_numbers.");

		builder.Property(x => x.RtFiscalReceiptNumber)
			.HasColumnName("rt_fiscal_receipt_number")
			.HasMaxLength(20)
			.HasComment("Numero scontrino restituito dal registratore RT — proiezione read-only non autorevole di device_response.");

		builder.Property(x => x.RtFiscalAmount)
			.HasColumnName("rt_fiscal_amount")
			.HasColumnType("numeric(12,2)")
			.HasComment("Importo dello scontrino RT — proiezione derivata da device_response (virgola decimale del dispositivo convertita a numeric).");

		builder.Property(x => x.RtZReportNumber)
			.HasColumnName("rt_z_report_number")
			.HasMaxLength(20)
			.HasComment("Numero della chiusura Z in cui rientra lo scontrino — proiezione read-only di device_response.");

		builder.Property(x => x.RtSerialNumber)
			.HasColumnName("rt_serial_number")
			.HasMaxLength(40)
			.HasComment("Matricola del registratore telematico che ha stampato — proiezione read-only di device_response.");

		builder.Property(x => x.PrintedAt)
			.HasColumnName("printed_at")
			.IsRequired()
			.HasDefaultValueSql("now()")
			.HasComment("Data/ora della stampa; determina il giorno fiscale usato dalla sequenza dei numeri progressivi.");

		builder.Property(x => x.DeviceResponse)
			.HasColumnName("device_response")
			.HasColumnType("jsonb")
			.HasComment("Risposta grezza del registratore telematico (lastCommand, printerStatus, fiscalReceiptNumber/Amount/Date/Time, zRepNumber, serialNumber, …) — passthrough di un sistema esterno, mai interrogata per campo.");

		builder.Property(x => x.CreatedByUserId)
			.HasColumnName("created_by_user_id")
			.HasComment("public_id di identity.access_users, operatore che ha lanciato la stampa — copiato, nessuna FK cross-schema.");

		builder.Property(x => x.CreatedAt)
			.HasColumnName("created_at")
			.HasDefaultValueSql("now()")
			.HasComment("Data/ora di creazione della riga.");

		builder.Property(x => x.UpdatedAt)
			.HasColumnName("updated_at")
			.HasDefaultValueSql("now()")
			.HasComment("Data/ora di ultima modifica della riga.");

		builder.Property(x => x.DeletedAt)
			.HasColumnName("deleted_at")
			.HasComment("Soft-delete esposto per uniformità con le altre entità del modulo; mai valorizzato in pratica — log di stampe / fatto fiscale, retention da obbligo di conservazione.");

		builder.HasIndex(x => new { x.PropertyId, x.PrintedAt })
			.HasDatabaseName("ix_fiscalprint_receipts_property_date");
		// PrintedAt in ordine decrescente nella query applicativa (elenco stampe più recenti prima);
		// l'indice btree copre entrambe le direzioni di scan.

		builder.HasIndex(x => x.ReservationPublicId)
			.HasFilter("reservation_public_id IS NOT NULL")
			.HasDatabaseName("ix_fiscalprint_receipts_reservation");

		builder.HasIndex(x => x.PrinterId)
			.HasDatabaseName("ix_fiscalprint_receipts_printer");

		builder.HasOne(x => x.Printer)
			.WithMany()
			.HasForeignKey(x => x.PrinterId)
			.HasConstraintName("fk_fiscalprint_receipts_printer")
			.OnDelete(DeleteBehavior.SetNull);
	}
}
