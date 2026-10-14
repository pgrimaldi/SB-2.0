using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Module.Switchboard.Domain.Entities;
using Module.Switchboard.Domain.Entities.Messaging;
using Module.Switchboard.Domain.Entities.Voice;
using Module.Switchboard.Domain.Enums;
using Module.Switchboard.Domain.Enums.Messaging;
using Module.Switchboard.Domain.Enums.Voice;
using Shared.Infrastructure;

namespace Module.Switchboard.Infrastructure;

/// <summary>
/// Contesto di persistenza del modulo Switchboard, schema PostgreSQL "switchboard".
/// Nome della classe = solo "DbContext": il namespace la disambigua dagli altri moduli.
/// Mai referenziato fuori da Module.Switchboard.Infrastructure.
/// </summary>
public sealed class DbContext : Microsoft.EntityFrameworkCore.DbContext
{
	public const string Schema = "switchboard";

	private readonly IDateTimeProvider _dateTimeProvider;

	public DbContext(DbContextOptions<DbContext> options, IDateTimeProvider dateTimeProvider) : base(options)
	{
		_dateTimeProvider = dateTimeProvider;
	}

	// DbSet<TEntity> di ogni aggregato/entita del modulo — popolati
	// dall'implementazione del modulo, uno per tabella con IEntityTypeConfiguration corrispondente.
	public DbSet<SwitchboardConfig> SwitchboardConfigs => Set<SwitchboardConfig>();
	public DbSet<SwitchboardMediaAsset> SwitchboardMediaAssets => Set<SwitchboardMediaAsset>();
	public DbSet<MessagingWaTemplate> MessagingWaTemplates => Set<MessagingWaTemplate>();
	public DbSet<MessagingWaEventLog> MessagingWaEventLogs => Set<MessagingWaEventLog>();
	public DbSet<VoiceCallDiagnosticEvent> VoiceCallDiagnosticEvents => Set<VoiceCallDiagnosticEvent>();
	public DbSet<VoiceCallRecord> VoiceCallRecords => Set<VoiceCallRecord>();
	public DbSet<VoiceMenuAction> VoiceMenuActions => Set<VoiceMenuAction>();
	public DbSet<VoiceMenuItem> VoiceMenuItems => Set<VoiceMenuItem>();
	public DbSet<VoiceNumberRegistration> VoiceNumberRegistrations => Set<VoiceNumberRegistration>();
	public DbSet<VoiceSoftphoneDevice> VoiceSoftphoneDevices => Set<VoiceSoftphoneDevice>();

	protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
	{
		optionsBuilder.AddInterceptors(new AuditInterceptor(_dateTimeProvider), new SoftDeleteInterceptor(_dateTimeProvider));
	}

	protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
	{
		ModuleDbContextConventions.ConfigureUtcDateTimes(configurationBuilder);
	}

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.HasDefaultSchema(Schema);

		// Enum nativi PostgreSQL del modulo (modelBuilder.HasPostgresEnum<TEnum>(Schema, "..."))
		// vanno qui.
		modelBuilder.HasPostgresEnum<CallRoutingMode>(Schema, "call_routing_mode");
		modelBuilder.HasPostgresEnum<ForwardCallerIdMode>(Schema, "forward_caller_id_mode");
		modelBuilder.HasPostgresEnum<MediaOrigin>(Schema, "media_origin");
		modelBuilder.HasPostgresEnum<WaTemplateCategory>(Schema, "wa_template_category");
		modelBuilder.HasPostgresEnum<WaTemplateStatus>(Schema, "wa_template_status");
		modelBuilder.HasPostgresEnum<WaHeaderFormat>(Schema, "wa_header_format");
		modelBuilder.HasPostgresEnum<WaWebhookEventType>(Schema, "wa_webhook_event_type");
		modelBuilder.HasPostgresEnum<WaMessageStatus>(Schema, "wa_message_status");
		modelBuilder.HasPostgresEnum<WaConversationOrigin>(Schema, "wa_conversation_origin");
		modelBuilder.HasPostgresEnum<WaPricingCategory>(Schema, "wa_pricing_category");
		modelBuilder.HasPostgresEnum<WaInboundMessageType>(Schema, "wa_inbound_message_type");
		modelBuilder.HasPostgresEnum<CallEventSource>(Schema, "call_event_source");
		modelBuilder.HasPostgresEnum<CallDirectionRaw>(Schema, "call_direction_raw");
		modelBuilder.HasPostgresEnum<CallDirection>(Schema, "call_direction");
		modelBuilder.HasPostgresEnum<CallOutcome>(Schema, "call_outcome");
		modelBuilder.HasPostgresEnum<ForwardResult>(Schema, "forward_result");
		modelBuilder.HasPostgresEnum<MenuActionType>(Schema, "menu_action_type");
		modelBuilder.HasPostgresEnum<WaRecipientMode>(Schema, "wa_recipient_mode");
		modelBuilder.HasPostgresEnum<NumberProvisioningState>(Schema, "number_provisioning_state");
		modelBuilder.HasPostgresEnum<ItCustomerType>(Schema, "it_customer_type");
		modelBuilder.HasPostgresEnum<IdentityDocType>(Schema, "identity_doc_type");
		modelBuilder.HasPostgresEnum<SoftphonePushPlatform>(Schema, "softphone_push_platform");

		modelBuilder.ApplyConfigurationsFromAssembly(typeof(DbContext).Assembly);

		ModuleDbContextConventions.ApplySoftDeleteQueryFilter(modelBuilder);
	}
}
