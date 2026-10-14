using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Fiskaly.Domain.Entities;
using Module.Fiskaly.Domain.Enums;

namespace Module.Fiskaly.Infrastructure.Configurations;

public sealed class ReceiptLogConfiguration : IEntityTypeConfiguration<ReceiptLog>
{
	public void Configure(EntityTypeBuilder<ReceiptLog> builder)
	{
		builder.ToTable("receipt_logs", "fiskaly", t => t.HasComment(
			"Log append-only del flusso di emissione/annullamento dello scontrino elettronico: una riga per transizione di stato o errore SIGN IT."));

		builder.HasKey(x => x.Id).HasName("pk_fiskaly_receipt_logs");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.ReceiptId)
			.HasColumnName("receipt_id")
			.HasComment("Scontrino a cui il log si riferisce.");
		builder.HasOne<Receipt>()
			.WithMany()
			.HasForeignKey(x => x.ReceiptId)
			.HasConstraintName("fk_fiskaly_receipt_logs_receipt")
			.OnDelete(DeleteBehavior.SetNull);
		builder.HasIndex(x => new { x.ReceiptId, x.CreatedAt })
			.HasDatabaseName("ix_fiskaly_receipt_logs_receipt");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.HasComment("public_id di catalog.properties, copiato — nessuna FK cross-schema. Ridondante con lo scontrino, comodo per filtrare i log.");
		// Overload con nome: due HasIndex sulle stesse colonne senza nome sono LO STESSO indice per EF,
		// e il secondo ne sovrascrive nome, filtro e unicità. Fino al 2026-10-06 a DB ne esisteva uno solo.
		builder.HasIndex(x => new { x.PropertyId, x.CreatedAt }, "ix_fiskaly_receipt_logs_property_date")
			.IsDescending(false, true)
			.HasDatabaseName("ix_fiskaly_receipt_logs_property_date");
		builder.HasIndex(x => new { x.PropertyId, x.CreatedAt }, "ix_fiskaly_receipt_logs_errors")
			.HasFilter("result = 'ko'")
			.HasDatabaseName("ix_fiskaly_receipt_logs_errors");

		// BRIN su created_at: log in solo append, ordine fisico gia'
		// coincidente con l'ordine temporale. Accelera la scansione del job di retention (12 mesi
		// flat) senza il costo di un B-tree su una colonna che nessuna query applicativa filtra da
		// sola. Il partizionamento qui e' stato escluso: 8.063 righe totali (2.1).
		builder.HasIndex(x => x.CreatedAt)
			.HasMethod("brin")
			.HasDatabaseName("brin_fiskaly_receipt_logs_created_at");

		builder.Property(x => x.BookingNumber)
			.HasColumnName("booking_number")
			.HasMaxLength(32)
			.HasComment("Numero prenotazione dello scontrino — denormalizzazione per filtrare i log senza join.");

		builder.Property(x => x.Step)
			.HasColumnName("step")
			.HasColumnType("fiskaly.receipt_log_step")
			.IsRequired()
			.HasComment("Passo/stato del flusso a cui il log si riferisce.");

		builder.Property(x => x.Result)
			.HasColumnName("result")
			.HasColumnType("fiskaly.log_result")
			.IsRequired()
			.HasDefaultValueSql("'ok'")
			.HasComment("Esito del passo.");

		builder.Property(x => x.InternalMessage)
			.HasColumnName("internal_message")
			.HasComment("Messaggio d'errore interno o nota diagnostica; NULL sui log OK.");

		builder.Property(x => x.FiskalyRecordState)
			.HasColumnName("fiskaly_record_state")
			.HasColumnType("fiskaly.fiskaly_record_state")
			.HasComment("Stato grezzo del record restituito da Fiskaly per quel passo.");

		builder.Property(x => x.FiskalyErrorStatus)
			.HasColumnName("fiskaly_error_status")
			.HasColumnType("smallint")
			.HasComment("Status HTTP dell'errore Fiskaly/axios; NULL sui log OK.");

		builder.Property(x => x.FiskalyErrorCode)
			.HasColumnName("fiskaly_error_code")
			.HasMaxLength(60)
			.HasComment("Codice d'errore applicativo/Fiskaly — vocabolario aperto (SIGN IT può introdurne di nuovi), non enum.");

		builder.Property(x => x.FiskalyErrorCategory)
			.HasColumnName("fiskaly_error_category")
			.HasMaxLength(80)
			.HasComment("Breve categoria/descrizione dell'errore Fiskaly.");

		builder.Property(x => x.FiskalyErrorMessage)
			.HasColumnName("fiskaly_error_message")
			.HasComment("Messaggio d'errore Fiskaly completo, non troncato (a differenza del legacy).");

		builder.Property(x => x.FiskalyLogPayload)
			.HasColumnName("fiskaly_log_payload")
			.HasColumnType("jsonb")
			.HasComment("Array/oggetto grezzo dei log d'errore Fiskaly — passthrough di sistema esterno, solo diagnostica.");

		builder.Property(x => x.ActorUserId)
			.HasColumnName("actor_user_id")
			.HasComment("public_id di identity.access_users, copiato — nessuna FK cross-schema. Operatore che ha innescato l'operazione.");

		builder.Property(x => x.CreatedAt)
			.HasColumnName("created_at")
			.IsRequired()
			.HasDefaultValueSql("now()")
			.HasComment("Istante del log. Chiave di ordinamento cronologico dei log di uno scontrino.");

		builder.Property(x => x.UpdatedAt)
			.HasColumnName("updated_at")
			.IsRequired()
			.HasDefaultValueSql("now()")
			.HasComment("Esposto per uniformità ma non usato: tabella di puro log append-only, sempre uguale a created_at.");

		builder.Property(x => x.DeletedAt)
			.HasColumnName("deleted_at")
			.HasComment("Esposto per uniformità ma non usato: log append-only, nessun soft-delete.");
	}
}
