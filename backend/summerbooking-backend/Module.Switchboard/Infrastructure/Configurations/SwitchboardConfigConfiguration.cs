using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Switchboard.Domain.Entities;
using Module.Switchboard.Domain.Enums;

namespace Module.Switchboard.Infrastructure.Configurations;

public sealed class SwitchboardConfigConfiguration : IEntityTypeConfiguration<SwitchboardConfig>
{
	public void Configure(EntityTypeBuilder<SwitchboardConfig> builder)
	{
		builder.ToTable("switchboard_configs", "switchboard", t => t.HasComment(
			"Configurazione del centralino virtuale (IVR Telnyx + inoltro SIP + WhatsApp Business) di uno stabilimento; una riga per property_id."));

		builder.HasKey(x => x.Id).HasName("pk_switchboard_configs");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId)
			.HasColumnName("public_id")
			.ValueGeneratedNever()
			.IsRequired()
			.HasComment("Id pubblico stabile usato dal pannello back-office e negli export GDPR.");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, copiato — nessuna FK cross-schema. Chiave di accesso di tutti gli endpoint del modulo.");

		builder.Property(x => x.IsActive)
			.HasColumnName("is_active")
			.IsRequired()
			.HasDefaultValue(false)
			.HasComment("Interruttore del centralino: la chiamata è accettata solo se la config esiste ed è attiva.");

		builder.Property(x => x.RoutingMode)
			.HasColumnName("routing_mode")
			.HasColumnType("switchboard.call_routing_mode")
			.IsRequired()
			.HasDefaultValueSql("'direct_sip'")
			.HasComment("Modalità di servizio della chiamata in ingresso: ivr (risponde via Call Control App) o direct_sip (auto-bridge diretto).");

		builder.Property(x => x.TelnyxPhoneE164)
			.HasColumnName("telnyx_phone_e164")
			.HasMaxLength(20)
			.HasComment("Numero PSTN geografico Telnyx dello stabilimento, in E.164; chiave di lookup della chiamata in ingresso.");

		builder.Property(x => x.TelnyxConnectionId)
			.HasColumnName("telnyx_connection_id")
			.HasMaxLength(64)
			.HasComment("Id della credential connection Telnyx dell'endpoint SIP (identificatore esterno opaco).");

		builder.Property(x => x.TelnyxCallControlAppId)
			.HasColumnName("telnyx_call_control_app_id")
			.HasMaxLength(64)
			.HasComment("Id della Call Control Application Telnyx; presente quando routing_mode = ivr.");

		builder.Property(x => x.TelnyxMessagingProfileId)
			.HasColumnName("telnyx_messaging_profile_id")
			.HasMaxLength(64)
			.HasComment("Id del messaging profile Telnyx usato per gli SMS (es. OTP di verifica numero WhatsApp).");

		builder.Property(x => x.SipEndpointId)
			.HasColumnName("sip_endpoint_id")
			.HasMaxLength(64)
			.HasComment("Id della telephony credential Telnyx dell'utenza softphone dello stabilimento.");

		builder.Property(x => x.SipUsername)
			.HasColumnName("sip_username")
			.HasMaxLength(100)
			.HasComment("Username SIP dell'utenza softphone, generato server-side; usato per comporre il SIP URI di inoltro.");

		builder.Property(x => x.SipPasswordSecretRef)
			.HasColumnName("sip_password_secret_ref")
			.HasMaxLength(255)
			.HasComment("Riferimento (ARN/nome) al segreto della password SIP nel secret manager — nessun materiale segreto in tabella.");

		builder.Property(x => x.SipClientType)
			.HasColumnName("sip_client_type")
			.HasMaxLength(50)
			.HasDefaultValue("generic")
			.HasComment("Etichetta descrittiva del client SIP dello stabilimento; nessun effetto sulla logica di chiamata.");

		builder.Property(x => x.SipClientNotes)
			.HasColumnName("sip_client_notes")
			.HasColumnType("text")
			.HasComment("Note libere sull'installazione del client SIP, solo pannello.");

		builder.Property(x => x.WhatsappPhoneNumberId)
			.HasColumnName("whatsapp_phone_number_id")
			.HasMaxLength(64)
			.HasComment("Id Meta Cloud API del numero WhatsApp Business specifico dello stabilimento, se presente ha priorità sul numero generico di piattaforma.");

		builder.Property(x => x.WhatsappPhoneE164)
			.HasColumnName("whatsapp_phone_e164")
			.HasMaxLength(20)
			.HasComment("Numero WhatsApp Business in formato E.164, per la visualizzazione nel pannello.");

		builder.Property(x => x.WelcomeAudioAssetId)
			.HasColumnName("welcome_audio_asset_id")
			.HasComment("Audio riprodotto alla risposta, base del gather DTMF di benvenuto.");

		builder.Property(x => x.FallbackAudioAssetId)
			.HasColumnName("fallback_audio_asset_id")
			.HasComment("Audio riprodotto quando un forward_sip fallisce, prima di chiudere la chiamata.");

		builder.Property(x => x.ForwardCallerIdMode)
			.HasColumnName("forward_caller_id_mode")
			.HasColumnType("switchboard.forward_caller_id_mode")
			.IsRequired()
			.HasDefaultValueSql("'caller'")
			.HasComment("Da quale numero il SIP endpoint vede arrivare la chiamata inoltrata: numero del chiamante, del centralino, o custom.");

		builder.Property(x => x.ForwardCustomCallerIdE164)
			.HasColumnName("forward_custom_caller_id_e164")
			.HasMaxLength(20)
			.HasComment("Caller ID mostrato al SIP endpoint quando forward_caller_id_mode = custom.");

		builder.Property(x => x.ForwardCallerIdName)
			.HasColumnName("forward_caller_id_name")
			.HasMaxLength(100)
			.HasComment("Display name opzionale mostrato accanto al caller ID durante l'inoltro SIP.");

		builder.Property(x => x.ForwardRingTimeoutSeconds)
			.HasColumnName("forward_ring_timeout_seconds")
			.IsRequired()
			.HasDefaultValue((short)90)
			.HasComment("Secondi di squillo del leg SIP prima di considerare fallito l'inoltro.");

		builder.Property(x => x.GatherTimeoutSeconds)
			.HasColumnName("gather_timeout_seconds")
			.IsRequired()
			.HasDefaultValue((short)7)
			.HasComment("Secondi di attesa dell'input DTMF prima del timeout.");

		builder.ToTable(t =>
		{
			t.HasCheckConstraint("ck_switchboard_configs_forward_custom_caller_id",
				"forward_caller_id_mode <> 'custom' OR forward_custom_caller_id_e164 IS NOT NULL");
			t.HasCheckConstraint("ck_switchboard_configs_forward_ring_timeout",
				"forward_ring_timeout_seconds BETWEEN 5 AND 300");
			t.HasCheckConstraint("ck_switchboard_configs_gather_timeout",
				"gather_timeout_seconds BETWEEN 3 AND 60");
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
			.HasDatabaseName("uq_switchboard_configs_public_id");

		builder.HasIndex(x => x.PropertyId)
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_switchboard_configs_property");

		builder.HasIndex(x => x.TelnyxPhoneE164)
			.IsUnique()
			.HasFilter("deleted_at IS NULL AND telnyx_phone_e164 IS NOT NULL")
			.HasDatabaseName("uq_switchboard_configs_phone");

		builder.HasIndex(x => x.SipUsername)
			.IsUnique()
			.HasFilter("deleted_at IS NULL AND sip_username IS NOT NULL")
			.HasDatabaseName("uq_switchboard_configs_sip_username");

		builder.HasOne(x => x.WelcomeAudioAsset)
			.WithMany()
			.HasForeignKey(x => x.WelcomeAudioAssetId)
			.HasConstraintName("fk_switchboard_configs_welcome_audio_asset")
			.OnDelete(DeleteBehavior.SetNull);

		builder.HasOne(x => x.FallbackAudioAsset)
			.WithMany()
			.HasForeignKey(x => x.FallbackAudioAssetId)
			.HasConstraintName("fk_switchboard_configs_fallback_audio_asset")
			.OnDelete(DeleteBehavior.SetNull);
	}
}
