using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Switchboard.Domain.Entities.Voice;
using Module.Switchboard.Domain.Enums.Voice;

namespace Module.Switchboard.Infrastructure.Configurations.Voice;

public sealed class VoiceMenuActionConfiguration : IEntityTypeConfiguration<VoiceMenuAction>
{
	public void Configure(EntityTypeBuilder<VoiceMenuAction> builder)
	{
		builder.ToTable("switchboard_voice_menu_actions", "switchboard", t => t.HasComment(
			"Azione eseguita quando il chiamante seleziona una voce di menù dell'IVR, eseguita in sequenza per execution_order."));

		builder.HasKey(x => x.Id).HasName("pk_switchboard_voice_menu_actions");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId)
			.HasColumnName("public_id")
			.ValueGeneratedNever()
			.IsRequired()
			.HasComment("Id pubblico stabile per l'API del pannello.");

		builder.Property(x => x.MenuItemId)
			.HasColumnName("menu_item_id")
			.IsRequired()
			.HasComment("Voce di menù a cui è collegata l'azione.");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, copiato — nessuna FK cross-schema; denormalizzato per scoping multi-tenant senza join.");

		builder.Property(x => x.ActionType)
			.HasColumnName("action_type")
			.HasColumnType("switchboard.menu_action_type")
			.IsRequired()
			.HasComment("Tipo di azione: hangup (chiude, terminale), forward_sip (inoltra al SIP endpoint con i parametri della config, terminale), send_whatsapp (invia un template, non terminale).");

		builder.Property(x => x.ExecutionOrder)
			.HasColumnName("execution_order")
			.IsRequired()
			.HasDefaultValue(0)
			.HasComment("Ordine di esecuzione delle azioni della stessa voce, ascendente.");

		builder.Property(x => x.WaTemplateId)
			.HasColumnName("wa_template_id")
			.HasComment("Template WhatsApp da inviare (solo send_whatsapp); FK viva, l'azione ne segue lingua/categoria finché il template esiste.");

		builder.Property(x => x.WaTemplateNameSnapshot)
			.HasColumnName("wa_template_name_snapshot")
			.HasMaxLength(512)
			.HasComment("Nome del template su Meta congelato all'associazione, indipendente dal ciclo di vita della FK viva.");

		builder.Property(x => x.WaLanguage)
			.HasColumnName("wa_language")
			.HasMaxLength(10)
			.HasDefaultValue("it")
			.HasComment("Codice lingua del template nel formato Meta; codice di libreria, non enum di dominio.");

		builder.Property(x => x.WaRecipientMode)
			.HasColumnName("wa_recipient_mode")
			.HasColumnType("switchboard.wa_recipient_mode")
			.HasDefaultValueSql("'caller'")
			.HasComment("A chi inviare il template: numero del chiamante corrente o wa_recipient_phone_e164 fisso.");

		builder.Property(x => x.WaRecipientPhoneE164)
			.HasColumnName("wa_recipient_phone_e164")
			.HasMaxLength(20)
			.HasComment("Destinatario fisso in E.164 quando wa_recipient_mode = fixed.");

		builder.Property(x => x.WaBodyParams)
			.HasColumnName("wa_body_params")
			.HasColumnType("jsonb")
			.HasDefaultValueSql("'[]'::jsonb")
			.HasComment("Parametri posizionali del corpo del template Meta, passthrough dell'API di terzi.");

		builder.Property(x => x.WaHeaderParams)
			.HasColumnName("wa_header_params")
			.HasColumnType("jsonb")
			.HasDefaultValueSql("'[]'::jsonb")
			.HasComment("Parametri di header del template Meta, passthrough dell'API di terzi.");

		builder.Property(x => x.WaButtonParams)
			.HasColumnName("wa_button_params")
			.HasColumnType("jsonb")
			.HasDefaultValueSql("'[]'::jsonb")
			.HasComment("Parametri di bottone del template Meta, passthrough dell'API di terzi.");

		builder.ToTable(t =>
		{
			t.HasCheckConstraint("ck_switchboard_voice_menu_actions_wa_template",
				"action_type <> 'send_whatsapp' OR wa_template_id IS NOT NULL");
			t.HasCheckConstraint("ck_switchboard_voice_menu_actions_wa_recipient",
				"wa_recipient_mode <> 'fixed' OR wa_recipient_phone_e164 IS NOT NULL");
		});

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
			.HasDatabaseName("uq_switchboard_voice_menu_actions_public_id");

		builder.HasIndex(x => new { x.MenuItemId, x.ExecutionOrder })
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_switchboard_voice_menu_actions_item_order");

		builder.HasIndex(x => x.WaTemplateId)
			.HasFilter("wa_template_id IS NOT NULL")
			.HasDatabaseName("ix_switchboard_voice_menu_actions_wa_template");

		builder.HasOne(x => x.MenuItem)
			.WithMany(i => i.Actions)
			.HasForeignKey(x => x.MenuItemId)
			.HasConstraintName("fk_switchboard_voice_menu_actions_menu_item")
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasOne(x => x.WaTemplate)
			.WithMany()
			.HasForeignKey(x => x.WaTemplateId)
			.HasConstraintName("fk_switchboard_voice_menu_actions_wa_template")
			.OnDelete(DeleteBehavior.Restrict);
	}
}
