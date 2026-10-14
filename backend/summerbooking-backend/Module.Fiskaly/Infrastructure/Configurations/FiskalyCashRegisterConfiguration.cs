using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Fiskaly.Domain.Entities;
using Module.Fiskaly.Domain.Enums;

namespace Module.Fiskaly.Infrastructure.Configurations;

public sealed class FiskalyCashRegisterConfiguration : IEntityTypeConfiguration<CashRegister>
{
	public void Configure(EntityTypeBuilder<CashRegister> builder)
	{
		builder.ToTable("cash_registers", "fiskaly", t => t.HasComment(
			"Registratore di cassa / System FISCAL_DEVICE registrato su Fiskaly SIGN IT, con cui si firmano i documenti commerciali."));

		builder.HasKey(x => x.Id).HasName("pk_fiskaly_cash_registers");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.AccountId)
			.HasColumnName("account_id")
			.IsRequired()
			.HasComment("Account Fiskaly (stabilimento) a cui la cassa appartiene.");
		// Overload con nome: due HasIndex sulle stesse colonne senza nome sono LO STESSO indice per EF,
		// e il secondo ne sovrascrive nome, filtro e unicità. Fino al 2026-10-06 a DB ne esisteva uno solo.
		builder.HasIndex(x => x.AccountId, "ix_fiskaly_cash_registers_account")
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_fiskaly_cash_registers_account");
		builder.HasIndex(x => x.AccountId, "uq_fiskaly_cash_registers_default")
			.IsUnique()
			.HasFilter("is_default AND deleted_at IS NULL")
			.HasDatabaseName("uq_fiskaly_cash_registers_default");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, copiato — nessuna FK cross-schema. Denormalizzato per lo scoping/indici senza join.");
		builder.HasIndex(x => new { x.PropertyId, x.SystemState, x.SystemMode })
			.HasFilter("deleted_at IS NULL AND system_state = 'commissioned' AND system_mode = 'operative'")
			.HasDatabaseName("ix_fiskaly_cash_registers_property_active");

		builder.Property(x => x.FiskalySystemId)
			.HasColumnName("fiskaly_system_id")
			.HasMaxLength(36)
			.HasComment("Id del System su Fiskaly; chiave con cui scontrini e sequenze giornaliere si legano alla cassa.");
		builder.HasIndex(x => x.FiskalySystemId)
			.IsUnique()
			.HasFilter("fiskaly_system_id IS NOT NULL")
			.HasDatabaseName("uq_fiskaly_cash_registers_system");

		builder.Property(x => x.SystemType)
			.HasColumnName("system_type")
			.HasColumnType("fiskaly.system_type")
			.IsRequired()
			.HasDefaultValueSql("'fiscal_device'")
			.HasComment("Tipo di System su Fiskaly (di fatto costante: FISCAL_DEVICE).");

		builder.Property(x => x.ProducerNumber)
			.HasColumnName("producer_number")
			.HasMaxLength(128)
			.HasComment("Numero produttore del dispositivo (producer.number); non modificabile dopo la creazione.");

		builder.Property(x => x.ProducerName)
			.HasColumnName("producer_name")
			.HasMaxLength(256)
			.HasComment("Nome/descrizione del produttore — campo descrittivo, non chiave di riconoscimento della cassa software.");

		builder.Property(x => x.SoftwareName)
			.HasColumnName("software_name")
			.HasMaxLength(128)
			.HasComment("Nome del software gestionale (software.name verso Fiskaly).");

		builder.Property(x => x.SoftwareVersion)
			.HasColumnName("software_version")
			.HasMaxLength(64)
			.HasComment("Versione del software gestionale (software.version verso Fiskaly); un cambio forza decommission e ricreazione del System.");

		builder.Property(x => x.SystemState)
			.HasColumnName("system_state")
			.HasColumnType("fiskaly.entity_state")
			.HasComment("Stato del System su Fiskaly (acquired/commissioned/decommissioned).");

		builder.Property(x => x.SystemMode)
			.HasColumnName("system_mode")
			.HasColumnType("fiskaly.entity_mode")
			.HasComment("Modalità operativa del System su Fiskaly (inactive/operative/suspended/degraded).");

		builder.Property(x => x.IsDefault)
			.HasColumnName("is_default")
			.IsRequired()
			.HasDefaultValue(false)
			.HasComment("True = cassa di default dello stabilimento, usata quando l'emissione non specifica un system id.");

		builder.Property(x => x.IsSoftware)
			.HasColumnName("is_software")
			.IsRequired()
			.HasDefaultValue(false)
			.HasComment("True = registratore software auto-creato dal backend; colonna autorevole, non modificabile via API.");

		builder.Property(x => x.CreatedByUserId)
			.HasColumnName("created_by_user_id")
			.HasComment("public_id di identity.access_users, copiato — nessuna FK cross-schema. Operatore che ha creato la cassa.");

		builder.Property(x => x.UpdatedByUserId)
			.HasColumnName("updated_by_user_id")
			.HasComment("public_id di identity.access_users, copiato — nessuna FK cross-schema. Ultimo operatore che ha modificato la cassa.");

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
			.HasComment("Soft-delete, valorizzato da deleteEntities, closeAccount, dalle cascate di decommission.");
	}
}
