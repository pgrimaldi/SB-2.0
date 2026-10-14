using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Payment.Domain.Entities;
using Module.Payment.Domain.Enums;

namespace Module.Payment.Infrastructure.Configurations;

/// <summary>Mapping EF Core di <see cref="LedgerEntry"/> su payment.ledger_entries.</summary>
public sealed class LedgerEntryConfiguration : IEntityTypeConfiguration<LedgerEntry>
{
	public void Configure(EntityTypeBuilder<LedgerEntry> builder)
	{
		builder.ToTable("ledger_entries", "payment", t =>
		{
			t.HasComment("Movimento di incasso/acconto/spesa/rimborso/rettifica relativo a una prenotazione; append-only.");
			t.HasCheckConstraint("ck_payment_ledger_entries_gross_amount", "gross_amount > 0");
		});

		builder.HasKey(x => x.Id).HasName("pk_payment_ledger_entries");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId)
			.HasColumnName("public_id")
			.ValueGeneratedNever()
			.IsRequired()
			.HasComment("Id pubblico stabile, copiato cross-modulo (C4 righe payments[] dello scontrino; export contabile).");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, copiato — nessuna FK cross-schema; stabilimento che incassa.");

		builder.Property(x => x.ReservationPublicId)
			.HasColumnName("reservation_public_id")
			.HasComment("public_id di booking.booking_reservations, copiato — nessuna FK cross-schema; prenotazione del movimento.");

		builder.Property(x => x.AgreementLinePublicId)
			.HasColumnName("agreement_line_public_id")
			.HasComment("public_id di booking.booking_agreement_lines, copiato — nessuna FK cross-schema; valorizzato per addebiti reali su voucher hotel.");

		builder.Property(x => x.EntryType)
			.HasColumnName("entry_type")
			.HasColumnType("payment.ledger_entry_type")
			.HasDefaultValueSql("'payment'")
			.IsRequired()
			.HasComment("Natura del movimento: acconto, incasso, spesa cliente, rimborso o rettifica.");

		builder.Property(x => x.Direction)
			.HasColumnName("direction")
			.HasColumnType("payment.ledger_direction")
			.HasDefaultValueSql("'in'")
			.IsRequired()
			.HasComment("Verso del denaro rispetto allo stabilimento; gli importi restano sempre positivi.");

		builder.Property(x => x.PaymentMode)
			.HasColumnName("payment_mode")
			.HasColumnType("payment.payment_mode")
			.HasDefaultValueSql("'other'")
			.HasComment("Modalità con cui è avvenuto il movimento; NULL sui movimenti legacy non normalizzabili.");

		builder.Property(x => x.Source)
			.HasColumnName("source")
			.HasColumnType("payment.ledger_source")
			.HasDefaultValueSql("'manual'")
			.IsRequired()
			.HasComment("Da dove nasce il movimento (cassa manuale, gateway online, link, POS, borsellino, convenzione).");

		builder.Property(x => x.SourceRefId)
			.HasColumnName("source_ref_id")
			.HasComment("Id interno della riga di origine nello schema payment (charge, request link, pos charge, wallet transaction), tipo determinato da source; NULL se source = manual.");

		builder.Property(x => x.GrossAmount)
			.HasColumnName("gross_amount")
			.HasColumnType("numeric(12,2)")
			.IsRequired()
			.HasComputedColumnSql("net_amount + vat_amount", stored: true)
			.HasComment("Importo lordo del movimento, IVA inclusa — colonna generata (net_amount + " +
				"vat_amount). Sola lettura: le rettifiche contabili sono un nuovo movimento con " +
				"entry_kind = 'adjustment', mai un UPDATE dell'importo esistente.");

		builder.Property(x => x.NetAmount)
			.HasColumnName("net_amount")
			.HasColumnType("numeric(12,2)")
			.IsRequired()
			.HasComment("Imponibile del movimento.");

		builder.Property(x => x.VatAmount)
			.HasColumnName("vat_amount")
			.HasColumnType("numeric(12,2)")
			.IsRequired()
			.HasComment("Quota IVA del movimento.");

		builder.Property(x => x.VatRate)
			.HasColumnName("vat_rate")
			.HasColumnType("numeric(5,2)")
			.IsRequired()
			.HasComment("Aliquota IVA % applicata; se il movimento copre aliquote miste è la prevalente, il dettaglio sta nelle Lines.");

		builder.Property(x => x.Currency)
			.HasColumnName("currency")
			.HasColumnType("char(3)")
			.HasDefaultValue("EUR")
			.IsRequired()
			.HasComment("Valuta ISO 4217.");

		builder.Property(x => x.CustomerPublicId)
			.HasColumnName("customer_public_id")
			.HasComment("public_id di identity.customer_customers, copiato — nessuna FK cross-schema; cliente del movimento (riferimento vivo).");

		builder.Property(x => x.CustomerNameSnapshot)
			.HasColumnName("customer_name_snapshot")
			.HasMaxLength(160)
			.HasComment("Nome cliente congelato al momento del movimento (fatto fiscale), accanto al riferimento vivo customer_public_id.");

		builder.Property(x => x.CustomerEmailSnapshot)
			.HasColumnName("customer_email_snapshot")
			.HasMaxLength(180)
			.HasComment("Email cliente congelata al momento del movimento.");

		builder.Property(x => x.CustomerPhoneSnapshot)
			.HasColumnName("customer_phone_snapshot")
			.HasMaxLength(40)
			.HasComment("Telefono cliente congelato al momento del movimento.");

		builder.Property(x => x.RecordedByUserId)
			.HasColumnName("recorded_by_user_id")
			.HasComment("public_id di identity.access_users, copiato — nessuna FK cross-schema; operatore che ha registrato il movimento manuale (audit fiscale).");

		builder.Property(x => x.OccurredAt)
			.HasColumnName("occurred_at")
			.HasDefaultValueSql("now()")
			.IsRequired()
			.HasComment("Data/ora contabile del movimento, decorrenza retention.");

		builder.Property(x => x.StayFrom)
			.HasColumnName("stay_from")
			.HasComment("Inizio del soggiorno/servizio, denormalizzato per export contabile e filtri stagionali.");

		builder.Property(x => x.StayTo)
			.HasColumnName("stay_to")
			.HasComment("Fine del soggiorno/servizio, denormalizzato per export contabile e filtri stagionali.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at")
			.HasComment("Esposto per uniformità: append-only (una correzione è una nuova riga adjustment, sempre NULL).");

		builder.HasIndex(x => x.PublicId)
			.IsUnique()
			.HasDatabaseName("uq_payment_ledger_entries_public_id");

		builder.HasIndex(x => new { x.ReservationPublicId, x.OccurredAt })
			.HasDatabaseName("ix_payment_ledger_entries_reservation");

		builder.HasIndex(x => new { x.PropertyId, x.OccurredAt })
			.HasDatabaseName("ix_payment_ledger_entries_property_date");

		builder.HasIndex(x => new { x.PropertyId, x.PaymentMode, x.OccurredAt })
			.HasDatabaseName("ix_payment_ledger_entries_property_mode_date");

		builder.HasIndex(x => new { x.Source, x.SourceRefId })
			.HasDatabaseName("ix_payment_ledger_entries_source_ref");

		builder.HasIndex(x => x.AgreementLinePublicId)
			.HasFilter("agreement_line_public_id IS NOT NULL")
			.HasDatabaseName("ix_payment_ledger_entries_agreement_line");

		builder.OwnsMany(x => x.Lines, lines => LedgerEntryLineConfiguration.Configure(lines));
	}
}
