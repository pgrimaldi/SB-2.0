using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Fiskaly.Domain.Entities;
using Module.Fiskaly.Domain.Enums;

namespace Module.Fiskaly.Infrastructure.Configurations;

public sealed class ReceiptLineConfiguration : IEntityTypeConfiguration<ReceiptLine>
{
	public void Configure(EntityTypeBuilder<ReceiptLine> builder)
	{
		builder.ToTable("receipt_lines", "fiskaly", t =>
		{
			t.HasComment("Voce del documento commerciale (pacchetto, articolo, servizio, sovrapprezzo) congelata al momento dell'emissione.");
			t.HasCheckConstraint("ck_fiskaly_receipt_lines_quantity", "quantity > 0");
		});

		builder.HasKey(x => x.Id).HasName("pk_fiskaly_receipt_lines");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.ReceiptId)
			.HasColumnName("receipt_id")
			.IsRequired()
			.HasComment("Documento a cui la voce appartiene.");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, copiato — nessuna FK cross-schema. Denormalizzato per i riepiloghi IVA senza join.");
		builder.HasIndex(x => new { x.PropertyId, x.VatCodeId })
			.HasDatabaseName("ix_fiskaly_receipt_lines_vat");

		builder.Property(x => x.LineNo)
			.HasColumnName("line_no")
			.HasColumnType("smallint")
			.IsRequired()
			.HasComment("Ordine della voce nel documento (1-based).");
		builder.HasIndex(x => new { x.ReceiptId, x.LineNo })
			.IsUnique()
			.HasDatabaseName("ix_fiskaly_receipt_lines_receipt");

		builder.Property(x => x.LineKind)
			.HasColumnName("line_kind")
			.HasColumnType("fiskaly.line_item_type")
			.IsRequired()
			.HasComment("Tipo di voce-prenotazione sottostante (pacchetto/servizio/articolo/sovrapprezzo/…), vocabolario condiviso con booking.");

		builder.Property(x => x.FiskalyEntryType)
			.HasColumnName("fiskaly_entry_type")
			.HasColumnType("fiskaly.entry_type")
			.IsRequired()
			.HasDefaultValueSql("'sale'")
			.HasComment("Vendita o reso.");

		builder.Property(x => x.Category)
			.HasColumnName("category")
			.HasColumnType("fiskaly.entry_category")
			.IsRequired()
			.HasDefaultValueSql("'service'")
			.HasComment("Natura fiscale della voce: servizio o bene.");

		builder.Property(x => x.SourceLinePublicId)
			.HasColumnName("source_line_public_id")
			.HasComment("public_id della riga prenotazione C1 di provenienza (booking_lines / booking_service_lines / booking_line_additional_items secondo line_kind), copiato — nessuna FK cross-schema. NULL per voci sintetiche non mappabili.");
		builder.HasIndex(x => x.SourceLinePublicId)
			.HasFilter("source_line_public_id IS NOT NULL")
			.HasDatabaseName("ix_fiskaly_receipt_lines_source");

		builder.Property(x => x.DescriptionSnapshot)
			.HasColumnName("description_snapshot")
			.HasMaxLength(255)
			.IsRequired()
			.HasComment("Descrizione articolo congelata all'emissione (nome pacchetto/servizio al momento del documento).");

		builder.Property(x => x.Quantity)
			.HasColumnName("quantity")
			.HasColumnType("numeric(12,3)")
			.IsRequired()
			.HasDefaultValue(1m)
			.HasComment("Quantità della voce (ammette frazioni per servizi a peso/ore).");

		builder.Property(x => x.UnitPriceGross)
			.HasColumnName("unit_price_gross")
			.HasColumnType("numeric(12,2)")
			.IsRequired()
			.HasComment("Prezzo unitario IVA inclusa.");

		builder.Property(x => x.UnitPriceNet)
			.HasColumnName("unit_price_net")
			.HasColumnType("numeric(12,2)")
			.IsRequired()
			.HasComment("Prezzo unitario IVA esclusa.");

		builder.Property(x => x.VatCodeId)
			.HasColumnName("vat_code_id")
			.IsRequired()
			.HasComment("Codice IVA/esenzione della voce.");
		builder.HasOne<VatCode>()
			.WithMany()
			.HasForeignKey(x => x.VatCodeId)
			.HasConstraintName("fk_fiskaly_receipt_lines_vat_code")
			.OnDelete(DeleteBehavior.Restrict);

		builder.Property(x => x.VatKind)
			.HasColumnName("vat_kind")
			.HasColumnType("fiskaly.vat_kind")
			.IsRequired()
			.HasDefaultValueSql("'vat_rate'")
			.HasComment("Tipo IVA denormalizzato dal codice IVA e congelato sulla riga al momento dell'emissione.");

		builder.Property(x => x.VatRate)
			.HasColumnName("vat_rate")
			.HasColumnType("numeric(5,2)")
			.IsRequired()
			.HasComment("Aliquota percentuale applicata (0 per le esenzioni) — dato proprio del documento, non uno snapshot.");

		builder.Property(x => x.VatAmount)
			.HasColumnName("vat_amount")
			.HasColumnType("numeric(12,2)")
			.IsRequired()
			.HasComment("IVA della riga.");

		builder.Property(x => x.LineGross)
			.HasColumnName("line_gross")
			.HasColumnType("numeric(12,2)")
			.IsRequired()
			.HasComment("Totale riga IVA inclusa (quantity × unit_price_gross), materializzato per i riepiloghi.");

		builder.Property(x => x.LineNet)
			.HasColumnName("line_net")
			.HasColumnType("numeric(12,2)")
			.IsRequired()
			.HasComment("Imponibile riga, materializzato per i riepiloghi.");

		builder.Property(x => x.PaidAllocation)
			.HasColumnName("paid_allocation")
			.HasColumnType("numeric(12,2)")
			.IsRequired()
			.HasDefaultValue(0m)
			.HasComment("Quota di incasso che questo scontrino ha attribuito a questa voce.");

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
			.HasComment("Esposto per uniformità ma non usato: append-only con la testata (fatto fiscale, retention 10 anni).");
	}
}
