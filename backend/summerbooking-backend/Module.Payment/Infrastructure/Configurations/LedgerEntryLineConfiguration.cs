using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Payment.Domain.Entities;
using Module.Payment.Domain.Enums;

namespace Module.Payment.Infrastructure.Configurations;

/// <summary>
/// Mapping EF Core di <see cref="LedgerEntryLine"/> su payment.ledger_entry_lines. Applicata dentro
/// <see cref="LedgerEntryConfiguration"/> via OwnsMany, non registrata da ApplyConfigurationsFromAssembly:
/// <see cref="OwnedNavigationBuilder{TOwner,TDependent}"/> non e un <see cref="EntityTypeBuilder{TEntity}"/>
/// (nessun ApplyConfiguration(IEntityTypeConfiguration) su di esso), quindi qui si espone un metodo
/// statico invece di implementare IEntityTypeConfiguration.
/// </summary>
public static class LedgerEntryLineConfiguration
{
	public static void Configure(OwnedNavigationBuilder<LedgerEntry, LedgerEntryLine> builder)
	{
		builder.ToTable("ledger_entry_lines", "payment", t => t.HasComment(
			"Quota di un movimento di pagamento attribuita a un articolo (pacchetto, servizio, voucher, ...), per l'export contabile e i report IVA per riga."));

		builder.HasKey(x => x.Id).HasName("pk_payment_ledger_entry_lines");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		// Riusa EntryId come FK reale della relazione owned verso LedgerEntry. Senza questo
		// WithOwner().HasForeignKey(...), EF Core NON usa la proprieta' scalare EntryId per la FK
		// della owned collection: crea invece una shadow property/colonna SEPARATA
		// (ledger_entry_id, con la vera FK NOT NULL) e lascia entry_id come colonna duplicata,
		// slegata, mai valorizzata da nessun vincolo - bug reale osservato il 2026-09-14: l'ETL di
		// migrazione valorizzava solo entry_id (l'unica colonna nota al modello/alla spec), e ogni
		// singola riga falliva il NOT NULL su ledger_entry_id (colonna "fantasma" invisibile
		// all'entity, ma reale a schema).
		builder.WithOwner().HasForeignKey(x => x.EntryId);

		builder.Property(x => x.EntryId)
			.HasColumnName("entry_id")
			.IsRequired()
			.HasComment("Movimento di pagamento a cui la riga appartiene.");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, copiato dalla testata — nessuna FK cross-schema. Denormalizzato per l'export contabile e i report IVA per riga senza join.");

		builder.HasIndex(x => new { x.PropertyId, x.VatRate })
			.HasDatabaseName("ix_payment_ledger_entry_lines_property");

		builder.Property(x => x.LineKind)
			.HasColumnName("line_kind")
			.HasColumnType("payment.ledger_line_kind")
			.HasDefaultValueSql("'unspecified'")
			.IsRequired()
			.HasComment("Tipo di articolo a cui è attribuita la quota.");

		builder.Property(x => x.DescriptionSnapshot)
			.HasColumnName("description_snapshot")
			.HasMaxLength(255)
			.IsRequired()
			.HasComment("Descrizione dell'articolo congelata al momento del movimento; il nome vivo si legge da C1 quando reservation_line_public_id è valorizzato.");

		builder.Property(x => x.ReservationLinePublicId)
			.HasColumnName("reservation_line_public_id")
			.HasComment("public_id di booking.booking_lines, copiato — nessuna FK cross-schema; riga prenotazione a cui la quota si riferisce, quando risolvibile.");

		builder.Property(x => x.Quantity)
			.HasColumnName("quantity")
			.HasDefaultValue(1)
			.IsRequired()
			.HasComment("Quantità dell'articolo (pacchetti 1; servizi count).");

		builder.Property(x => x.NetAmount)
			.HasColumnName("net_amount")
			.HasColumnType("numeric(12,2)")
			.HasDefaultValue(0m)
			.IsRequired()
			.HasComment("Imponibile della quota di movimento attribuita alla riga.");

		builder.Property(x => x.VatAmount)
			.HasColumnName("vat_amount")
			.HasColumnType("numeric(12,2)")
			.HasDefaultValue(0m)
			.IsRequired()
			.HasComment("Quota IVA della riga.");

		builder.Property(x => x.VatRate)
			.HasColumnName("vat_rate")
			.HasColumnType("numeric(5,2)")
			.IsRequired()
			.HasComment("Aliquota IVA % della riga.");

		builder.Property(x => x.IsAllocated)
			.HasColumnName("is_allocated")
			.HasDefaultValue(true)
			.IsRequired()
			.HasComment("True = la quota è una ripartizione proporzionale del movimento (stima), non un importo pagato distintamente per l'articolo.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at")
			.HasComment("Esposto per uniformità: append-only con la testata, sempre NULL.");

		builder.HasIndex(x => x.EntryId)
			.HasDatabaseName("ix_payment_ledger_entry_lines_entry");

		builder.HasIndex(x => x.ReservationLinePublicId)
			.HasFilter("reservation_line_public_id IS NOT NULL")
			.HasDatabaseName("ix_payment_ledger_entry_lines_reservation_line");

		builder.HasIndex(x => x.VatRate)
			.HasDatabaseName("ix_payment_ledger_entry_lines_vat");
	}
}
