using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.FiscalPrint.Domain.Entities;

namespace Module.FiscalPrint.Infrastructure.Configurations;

public sealed class FiscalPrintNumberSequenceConfiguration : IEntityTypeConfiguration<FiscalPrintNumberSequence>
{
	public void Configure(EntityTypeBuilder<FiscalPrintNumberSequence> builder)
	{
		builder.ToTable("serial_numbers", "fiscalprint", t =>
		{
			t.HasComment("Contatore del numero progressivo scontrino RT, una riga per (printer_id, fiscal_date); incrementato atomicamente a ogni stampa.");
			t.HasCheckConstraint("ck_fiscalprint_serial_numbers_last_number", "last_number >= 0");
		});

		builder.HasKey(x => x.Id).HasName("pk_fiscalprint_serial_numbers");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PrinterId)
			.HasColumnName("printer_id")
			.IsRequired()
			.HasComment("Stampante RT (dispositivo fiscale) a cui la serie appartiene — il progressivo è per dispositivo, non per stabilimento.");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, denormalizzato da fiscalprint.printer_configs per lo scoping/indici — copiato, nessuna FK cross-schema.");

		builder.Property(x => x.FiscalDate)
			.HasColumnName("fiscal_date")
			.IsRequired()
			.HasComment("Giorno a cui si riferisce il contatore, fuso Europe/Rome.");

		builder.Property(x => x.LastNumber)
			.HasColumnName("last_number")
			.IsRequired()
			.HasDefaultValue(0)
			.HasComment("Ultimo numero progressivo scontrino emesso quel giorno su quella stampante; il prossimo scontrino usa last_number + 1.");

		builder.Property(x => x.CreatedAt)
			.HasColumnName("created_at")
			.HasDefaultValueSql("now()")
			.HasComment("Data/ora di creazione della riga (primo scontrino del giorno su quella stampante).");

		builder.Property(x => x.UpdatedAt)
			.HasColumnName("updated_at")
			.HasDefaultValueSql("now()")
			.HasComment("Data/ora di ultimo incremento, aggiornata a ogni upsert.");

		builder.Property(x => x.DeletedAt)
			.HasColumnName("deleted_at")
			.HasComment("Colonna esposta solo per uniformità con le altre tabelle del modulo; non implementa ISoftDeletable e non è mai scritta — tabella di sequenza/contatore, non entità di dominio cancellabile.");

		builder.HasIndex(x => new { x.PrinterId, x.FiscalDate })
			.IsUnique()
			.HasDatabaseName("uq_fiscalprint_serial_numbers_key");

		builder.HasIndex(x => new { x.PropertyId, x.FiscalDate })
			.HasDatabaseName("ix_fiscalprint_serial_numbers_property_date");

		builder.HasOne(x => x.Printer)
			.WithMany()
			.HasForeignKey(x => x.PrinterId)
			.HasConstraintName("fk_fiscalprint_serial_numbers_printer")
			.OnDelete(DeleteBehavior.Restrict);
	}
}
