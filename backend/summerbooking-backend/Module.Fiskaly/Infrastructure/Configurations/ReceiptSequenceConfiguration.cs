using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Fiskaly.Domain.Entities;

namespace Module.Fiskaly.Infrastructure.Configurations;

public sealed class ReceiptSequenceConfiguration : IEntityTypeConfiguration<ReceiptSequence>
{
	public void Configure(EntityTypeBuilder<ReceiptSequence> builder)
	{
		builder.ToTable("receipt_sequences", "fiskaly", t => t.HasComment(
			"Contatore progressivo giornaliero del numero documento commerciale per registratore di cassa."));

		builder.HasKey(x => x.Id).HasName("pk_fiskaly_receipt_sequences");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, copiato — nessuna FK cross-schema. Denormalizzato per lo scoping/indici.");
		builder.HasIndex(x => new { x.PropertyId, x.FiscalDate })
			.HasDatabaseName("ix_fiskaly_receipt_sequences_property_date");

		builder.Property(x => x.CashRegisterId)
			.HasColumnName("cash_register_id")
			.IsRequired()
			.HasComment("Registratore di cassa a cui la sequenza appartiene.");
		builder.HasOne<CashRegister>()
			.WithMany()
			.HasForeignKey(x => x.CashRegisterId)
			.HasConstraintName("fk_fiskaly_receipt_sequences_cash_register")
			.OnDelete(DeleteBehavior.Restrict);

		builder.Property(x => x.FiscalDate)
			.HasColumnName("fiscal_date")
			.HasColumnType("date")
			.IsRequired()
			.HasComment("Giorno fiscale del contatore, sempre calcolato in Europe/Rome.");
		builder.HasIndex(x => new { x.CashRegisterId, x.FiscalDate })
			.IsUnique()
			.HasDatabaseName("uq_fiskaly_receipt_sequences_key");

		builder.Property(x => x.LastNumber)
			.HasColumnName("last_number")
			.IsRequired()
			.HasDefaultValue(0)
			.HasComment("Ultimo numero progressivo di documento effettivamente emesso quel giorno su quella cassa; monotòno, incrementato solo su firma riuscita.");

		builder.ToTable(t => t.HasCheckConstraint("ck_fiskaly_receipt_sequences_last_number", "last_number >= 0"));

		builder.Property(x => x.CreatedAt)
			.HasColumnName("created_at")
			.IsRequired()
			.HasDefaultValueSql("now()")
			.HasComment("Creazione della riga.");

		builder.Property(x => x.UpdatedAt)
			.HasColumnName("updated_at")
			.IsRequired()
			.HasDefaultValueSql("now()")
			.HasComment("Ultimo incremento del contatore (aggiornato dall'ON CONFLICT DO UPDATE).");

		builder.Property(x => x.DeletedAt)
			.HasColumnName("deleted_at")
			.HasComment("Esposto per uniformità ma non usato: la sequenza non si cancella.");
	}
}
