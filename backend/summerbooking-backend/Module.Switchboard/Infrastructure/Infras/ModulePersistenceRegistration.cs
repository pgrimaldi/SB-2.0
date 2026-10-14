using BuildingBlocks.Infrastructure.Persistence;
using BuildingBlocks.Infrastructure.Retention;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Module.Switchboard.Domain.Enums;
using Module.Switchboard.Domain.Enums.Messaging;
using Module.Switchboard.Domain.Enums.Voice;
using Npgsql;
using Shared.Infrastructure;

namespace Module.Switchboard.Infrastructure.Infras;

/// <summary>
/// Registrazione DI della persistenza del modulo Switchboard, richiamata da Module.AddSwitchboardModule().
/// Stessa costruzione (data source + enum mapping) della DesignTimeDbContextFactory, ma con
/// AddDbContext (Scoped) per l'uso a runtime dentro Boot.
/// </summary>
public static class ModulePersistenceRegistration
{
	public static IServiceCollection AddModuleSwitchboardPersistence(this IServiceCollection services, IConfiguration configuration)
	{
		services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

		var dataSourceBuilder = new NpgsqlDataSourceBuilder(ModuleConnectionStrings.Resolve(configuration));

		// Mappatura degli enum nativi PostgreSQL del modulo (dataSourceBuilder.MapEnum<TEnum>("switchboard...."))
		// va qui, in coppia con le modelBuilder.HasPostgresEnum<TEnum> di DbContext.OnModelCreating.
		dataSourceBuilder.MapEnum<CallRoutingMode>("switchboard.call_routing_mode");
		dataSourceBuilder.MapEnum<ForwardCallerIdMode>("switchboard.forward_caller_id_mode");
		dataSourceBuilder.MapEnum<MediaOrigin>("switchboard.media_origin");
		dataSourceBuilder.MapEnum<WaTemplateCategory>("switchboard.wa_template_category");
		dataSourceBuilder.MapEnum<WaTemplateStatus>("switchboard.wa_template_status");
		dataSourceBuilder.MapEnum<WaHeaderFormat>("switchboard.wa_header_format");
		dataSourceBuilder.MapEnum<WaWebhookEventType>("switchboard.wa_webhook_event_type");
		dataSourceBuilder.MapEnum<WaMessageStatus>("switchboard.wa_message_status");
		dataSourceBuilder.MapEnum<WaConversationOrigin>("switchboard.wa_conversation_origin");
		dataSourceBuilder.MapEnum<WaPricingCategory>("switchboard.wa_pricing_category");
		dataSourceBuilder.MapEnum<WaInboundMessageType>("switchboard.wa_inbound_message_type");
		dataSourceBuilder.MapEnum<CallEventSource>("switchboard.call_event_source");
		dataSourceBuilder.MapEnum<CallDirectionRaw>("switchboard.call_direction_raw");
		dataSourceBuilder.MapEnum<CallDirection>("switchboard.call_direction");
		dataSourceBuilder.MapEnum<CallOutcome>("switchboard.call_outcome");
		dataSourceBuilder.MapEnum<ForwardResult>("switchboard.forward_result");
		dataSourceBuilder.MapEnum<MenuActionType>("switchboard.menu_action_type");
		dataSourceBuilder.MapEnum<WaRecipientMode>("switchboard.wa_recipient_mode");
		dataSourceBuilder.MapEnum<NumberProvisioningState>("switchboard.number_provisioning_state");
		dataSourceBuilder.MapEnum<ItCustomerType>("switchboard.it_customer_type");
		dataSourceBuilder.MapEnum<IdentityDocType>("switchboard.identity_doc_type");
		dataSourceBuilder.MapEnum<SoftphonePushPlatform>("switchboard.softphone_push_platform");

		// Tetto del pool: ogni modulo ha il PROPRIO NpgsqlDataSource, quindi l'host ne ha
		// uno per modulo. Senza limite sarebbero 100 ciascuno (default Npgsql) contro un
		// max_connections che di norma e' 100 in tutto. Vedi ModuleConnectionStrings.
		ModuleConnectionStrings.ApplyModulePoolLimit(dataSourceBuilder, configuration);

		var dataSource = dataSourceBuilder.Build();

		services.AddDbContext<DbContext>((_, optionsBuilder) =>
			optionsBuilder.UseNpgsql(dataSource, npgsql => npgsql.MigrationsHistoryTable(ModuleConnectionStrings.MigrationsHistoryTable, DbContext.Schema))
			.UseSnakeCaseNamingConvention());

		// Job di retention del modulo: applica le policy di privacy_retention_policies che
		// riguardano QUESTO schema, sulla connessione di questo modulo. Chi orchestra risolve
		// IEnumerable<IModuleRetentionJob> e non conosce nessun DbContext.
		services.AddModuleRetention<DbContext>(DbContext.Schema);

		return services;
	}
}
