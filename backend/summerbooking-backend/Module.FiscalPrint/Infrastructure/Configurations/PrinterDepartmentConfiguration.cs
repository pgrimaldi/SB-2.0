using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.FiscalPrint.Domain.Entities;

namespace Module.FiscalPrint.Infrastructure.Configurations;

public sealed class PrinterDepartmentConfiguration : IEntityTypeConfiguration<PrinterDepartment>
{
	public void Configure(EntityTypeBuilder<PrinterDepartment> builder)
	{
		builder.ToTable("printer_departments", "fiscalprint", t =>
		{
			t.HasComment("Mappatura reparto fisico del registratore di cassa RT → aliquota IVA con cui il reparto è pre-programmato.");
			t.HasCheckConstraint("ck_fiscalprint_printer_departments_number", "dept_number > 0");
			t.HasCheckConstraint("ck_fiscalprint_printer_departments_rate", "vat_rate >= 0");
		});

		builder.HasKey(x => x.Id).HasName("pk_fiscalprint_printer_departments");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PrinterId)
			.HasColumnName("printer_id")
			.IsRequired()
			.HasComment("Stampante (fiscalprint.printer_configs) a cui il reparto appartiene.");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, denormalizzato per lo scoping — copiato, nessuna FK cross-schema.");

		builder.Property(x => x.DeptNumber)
			.HasColumnName("dept_number")
			.IsRequired()
			.HasComment("Numero del reparto fisico sul registratore RT (esplicito, non più posizionale come nel legacy).");

		builder.Property(x => x.VatRate)
			.HasColumnName("vat_rate")
			.HasColumnType("numeric(5,2)")
			.IsRequired()
			.HasComment("Aliquota IVA con cui il reparto è pre-programmato sulla stampante.");

		builder.Property(x => x.CreatedAt)
			.HasColumnName("created_at")
			.HasDefaultValueSql("now()")
			.HasComment("Data/ora di creazione della mappatura.");

		builder.Property(x => x.UpdatedAt)
			.HasColumnName("updated_at")
			.HasDefaultValueSql("now()")
			.HasComment("Data/ora di ultima modifica della mappatura.");

		builder.Property(x => x.DeletedAt)
			.HasColumnName("deleted_at")
			.HasComment("Soft-delete, in cascata dalla cancellazione della stampante proprietaria.");

		builder.HasIndex(x => new { x.PrinterId, x.DeptNumber })
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_fiscalprint_printer_departments_number");

		// Overload con nome: due HasIndex sulle stesse colonne senza nome sono LO STESSO indice per EF,
		// e il secondo ne sovrascrive nome, filtro e unicità. Fino al 2026-10-06 a DB ne esisteva uno solo.
		builder.HasIndex(x => new { x.PrinterId, x.VatRate }, "uq_fiscalprint_printer_departments_rate")
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_fiscalprint_printer_departments_rate");

		builder.HasIndex(x => new { x.PrinterId, x.VatRate }, "ix_fiscalprint_printer_departments_resolver")
			.HasDatabaseName("ix_fiscalprint_printer_departments_resolver");

		builder.HasOne(x => x.Printer)
			.WithMany(p => p.Departments)
			.HasForeignKey(x => x.PrinterId)
			.HasConstraintName("fk_fiscalprint_printer_departments_printer")
			.OnDelete(DeleteBehavior.Cascade);
	}
}
