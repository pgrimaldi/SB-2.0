using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Switchboard.Domain.Entities.Messaging;
using Module.Switchboard.Domain.Enums.Messaging;

namespace Module.Switchboard.Infrastructure.Configurations.Messaging;

public sealed class MessagingWaTemplateConfiguration : IEntityTypeConfiguration<MessagingWaTemplate>
{
	public void Configure(EntityTypeBuilder<MessagingWaTemplate> builder)
	{
		builder.ToTable("switchboard_messaging_wa_templates", "switchboard", t => t.HasComment(
			"Cache/audit locale di un template WhatsApp Business registrato su Meta per uno stabilimento; non governa l'invio, la fonte di verità resta Meta."));

		builder.HasKey(x => x.Id).HasName("pk_switchboard_messaging_wa_templates");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId)
			.HasColumnName("public_id")
			.ValueGeneratedNever()
			.IsRequired()
			.HasComment("Id pubblico stabile per l'API del pannello.");

		builder.Property(x => x.ConfigId)
			.HasColumnName("config_id")
			.IsRequired()
			.HasComment("Centralino proprietario del template.");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, copiato — nessuna FK cross-schema; denormalizzato per scoping e unicità (property_id, name, language).");

		builder.Property(x => x.TemplateName)
			.HasColumnName("template_name")
			.HasMaxLength(512)
			.IsRequired()
			.HasComment("Nome del template su Meta (minuscolo con underscore, unico per WABA); valore passato a template.name all'invio/cancellazione.");

		builder.Property(x => x.Language)
			.HasColumnName("language")
			.HasMaxLength(10)
			.IsRequired()
			.HasDefaultValue("it")
			.HasComment("Codice lingua del template nel formato Meta (language.code); codice di libreria, non enum di dominio.");

		builder.Property(x => x.Category)
			.HasColumnName("category")
			.HasColumnType("switchboard.wa_template_category")
			.IsRequired()
			.HasDefaultValueSql("'utility'")
			.HasComment("Categoria Meta del template, impatta pricing e regole d'invio.");

		builder.Property(x => x.Components)
			.HasColumnName("components")
			.HasColumnType("jsonb")
			.IsRequired()
			.HasComment("Array dei components del template nel formato Meta Cloud API, salvato e inviato verbatim, mai interrogato per sotto-campo.");

		builder.Property(x => x.MetaStatus)
			.HasColumnName("meta_status")
			.HasColumnType("switchboard.wa_template_status")
			.IsRequired()
			.HasDefaultValueSql("'pending'")
			.HasComment("Stato di approvazione lato Meta, aggiornato dal refresh periodico.");

		builder.Property(x => x.MetaStatusRaw)
			.HasColumnName("meta_status_raw")
			.HasMaxLength(40)
			.HasComment("Stringa di stato così come restituita da Meta, per gestire valori nuovi non ancora mappati nell'enum.");

		builder.Property(x => x.MetaTemplateId)
			.HasColumnName("meta_template_id")
			.HasMaxLength(64)
			.HasComment("Id assegnato da Meta alla creazione, usato per interrogare lo stato.");

		builder.Property(x => x.MetaRejectionReason)
			.HasColumnName("meta_rejection_reason")
			.HasColumnType("text")
			.HasComment("Motivo di rifiuto restituito da Meta, popolato dal refresh di stato.");

		builder.Property(x => x.HeaderFormat)
			.HasColumnName("header_format")
			.HasColumnType("switchboard.wa_header_format")
			.IsRequired()
			.HasDefaultValueSql("'none'")
			.HasComment("Formato dell'header del template, derivato da components alla scrittura — colonna di riepilogo non autorevole.");

		builder.Property(x => x.HasButtons)
			.HasColumnName("has_buttons")
			.IsRequired()
			.HasDefaultValue(false)
			.HasComment("Se il template ha bottoni, derivato da components alla scrittura — colonna di riepilogo non autorevole.");

		builder.Property(x => x.ButtonsCount)
			.HasColumnName("buttons_count")
			.IsRequired()
			.HasDefaultValue((short)0)
			.HasComment("Numero di bottoni del template, derivato da components alla scrittura.");

		builder.Property(x => x.BodyParamCount)
			.HasColumnName("body_param_count")
			.IsRequired()
			.HasDefaultValue((short)0)
			.HasComment("Numero di segnaposto nel corpo del template, derivato da components alla scrittura.");

		builder.Property(x => x.BodyPreview)
			.HasColumnName("body_preview")
			.HasMaxLength(160)
			.HasComment("Primi ~120 caratteri del testo BODY, derivato da components per la lista del pannello senza parsare il blob.");

		// bug reale osservato il 2026-09-16: created_at/updated_at non avevano default now() a
		// schema (nessuna convenzione EF globale lo applica, verificato in ModuleDbContextConventions
		// - solo converter UTC e query filter soft-delete) - un ETL che omette queste colonne
		// dall'INSERT (comportamento normale, per lasciare scattare il default) falliva NOT NULL.
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


		builder.HasIndex(x => x.PublicId)
			.IsUnique()
			.HasDatabaseName("uq_switchboard_messaging_wa_templates_public_id");

		// uq_switchboard_messaging_wa_templates_name_lang (property_id, lower(template_name), language)
		// non è esprimibile con HasIndex su proprietà semplice — indice su espressione lower(template_name),
		// creato via raw SQL nella migration AddMissingDesignedIndexes. Fino al
		// 2026-10-06 quel passo a mano non era mai stato fatto.

		builder.HasIndex(x => x.ConfigId)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_switchboard_messaging_wa_templates_config");

		builder.HasIndex(x => x.MetaTemplateId)
			.HasFilter("meta_template_id IS NOT NULL")
			.HasDatabaseName("ix_switchboard_messaging_wa_templates_meta_id");

		builder.HasIndex(x => new { x.PropertyId, x.MetaStatus })
			.HasDatabaseName("ix_switchboard_messaging_wa_templates_status");

		builder.HasOne(x => x.Config)
			.WithMany(c => c.WaTemplates)
			.HasForeignKey(x => x.ConfigId)
			.HasConstraintName("fk_switchboard_messaging_wa_templates_config")
			.OnDelete(DeleteBehavior.Cascade);
	}
}
