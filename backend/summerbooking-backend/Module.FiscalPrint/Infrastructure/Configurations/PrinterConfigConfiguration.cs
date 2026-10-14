using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.FiscalPrint.Domain.Entities;

namespace Module.FiscalPrint.Infrastructure.Configurations;

public sealed class PrinterConfigConfiguration : IEntityTypeConfiguration<PrinterConfig>
{
	public void Configure(EntityTypeBuilder<PrinterConfig> builder)
	{
		builder.ToTable("printer_configs", "fiscalprint", t =>
		{
			t.HasComment("Configurazione di una stampante (termica/fiscale RT) di uno stabilimento: modello, connessione, modalità d'uso, intestazioni scontrino.");
			t.HasCheckConstraint("ck_fiscalprint_printer_configs_port_range", "port IS NULL OR port BETWEEN 1 AND 65535");
			t.HasCheckConstraint("ck_fiscalprint_printer_configs_connection", "(connection_kind = 'tcp' AND host IS NOT NULL AND port IS NOT NULL) OR (connection_kind = 'usb' AND usb_device IS NOT NULL)");
		});

		builder.HasKey(x => x.Id).HasName("pk_fiscalprint_printer_configs");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties (stabilimento proprietario), copiato — nessuna FK cross-schema.");

		builder.Property(x => x.Name)
			.HasColumnName("name")
			.HasMaxLength(120)
			.IsRequired()
			.HasComment("Nome libero della stampante assegnato dall'operatore (etichetta in UI).");

		builder.Property(x => x.Model)
			.HasColumnName("model")
			.HasColumnType("fiscalprint.printer_model")
			.IsRequired()
			.HasComment("Modello stampante: determina l'adapter di stampa lato client (createPrinterAdapter).");

		builder.Property(x => x.Mode)
			.HasColumnName("mode")
			.HasColumnType("fiscalprint.printer_mode")
			.IsRequired()
			.HasComment("Modalità d'uso: scontrino fiscale RT, documento non fiscale/preconto, comanda.");

		builder.Property(x => x.ConnectionKind)
			.HasColumnName("connection_kind")
			.HasColumnType("fiscalprint.connection_kind")
			.IsRequired()
			.HasDefaultValueSql("'tcp'")
			.HasComment("Tipo di connessione al dispositivo: tcp (host+port) o usb (usb_device).");

		builder.Property(x => x.Host)
			.HasColumnName("host")
			.HasMaxLength(255)
			.HasComment("Indirizzo di rete della stampante (IP LAN o 127.0.0.1), valorizzato solo per connection_kind = tcp.");

		builder.Property(x => x.Port)
			.HasColumnName("port")
			.HasComment("Porta TCP della stampante, valorizzata solo per connection_kind = tcp.");

		builder.Property(x => x.UsbDevice)
			.HasColumnName("usb_device")
			.HasMaxLength(255)
			.HasComment("Identificatore del device USB locale, valorizzato solo per connection_kind = usb.");

		builder.Property(x => x.UseTls)
			.HasColumnName("use_tls")
			.IsRequired()
			.HasDefaultValue(false)
			.HasComment("True se la comunicazione con la stampante avviene via HTTPS/TLS anziché HTTP in chiaro.");

		builder.Property(x => x.IsActive)
			.HasColumnName("is_active")
			.IsRequired()
			.HasDefaultValue(false)
			.HasComment("Stampante attualmente selezionata/in uso per la sua modalità d'uso.");

		builder.Property(x => x.HeadingLine1)
			.HasColumnName("heading_line_1")
			.HasMaxLength(160)
			.HasComment("Prima riga di intestazione stampata in testa allo scontrino/comanda.");

		builder.Property(x => x.HeadingLine2)
			.HasColumnName("heading_line_2")
			.HasMaxLength(160)
			.HasComment("Seconda riga di intestazione stampata in testa allo scontrino/comanda.");

		builder.Property(x => x.FooterLine)
			.HasColumnName("footer_line")
			.HasMaxLength(160)
			.HasComment("Riga di piè di pagina stampata in coda allo scontrino/comanda.");

		builder.Property(x => x.CreatedByUserId)
			.HasColumnName("created_by_user_id")
			.HasComment("public_id di identity.access_users, operatore che ha configurato la stampante — copiato, nessuna FK cross-schema.");

		builder.Property(x => x.UpdatedByUserId)
			.HasColumnName("updated_by_user_id")
			.HasComment("public_id di identity.access_users, operatore che ha modificato per ultimo la stampante — copiato, nessuna FK cross-schema.");

		builder.Property(x => x.CreatedAt)
			.HasColumnName("created_at")
			.HasDefaultValueSql("now()")
			.HasComment("Data/ora di creazione della configurazione.");

		builder.Property(x => x.UpdatedAt)
			.HasColumnName("updated_at")
			.HasDefaultValueSql("now()")
			.HasComment("Data/ora di ultima modifica della configurazione.");

		builder.Property(x => x.DeletedAt)
			.HasColumnName("deleted_at")
			.HasComment("Soft-delete: valorizzato alla rimozione; is_active riportato a false in automatico (corregge il bug legacy che scollegava i due flag).");

		builder.HasIndex(x => x.PropertyId)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_fiscalprint_printer_configs_property");

		builder.HasIndex(x => new { x.PropertyId, x.Mode })
			.IsUnique()
			.HasFilter("is_active AND deleted_at IS NULL")
			.HasDatabaseName("uq_fiscalprint_printer_configs_active_mode");

		builder.HasMany(x => x.Departments)
			.WithOne(d => d.Printer)
			.HasForeignKey(d => d.PrinterId)
			.HasConstraintName("fk_fiscalprint_printer_departments_printer")
			.OnDelete(DeleteBehavior.Cascade);
	}
}
