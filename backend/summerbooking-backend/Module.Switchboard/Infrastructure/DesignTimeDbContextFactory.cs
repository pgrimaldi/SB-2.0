using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Module.Switchboard.Domain.Enums;
using Module.Switchboard.Domain.Enums.Messaging;
using Module.Switchboard.Domain.Enums.Voice;
using Npgsql;
using Shared.Infrastructure;

namespace Module.Switchboard.Infrastructure;

/// <summary>
/// Factory usata da "dotnet ef" per creare il DbContext a design-time (migration add/update/...)
/// senza passare dal progetto Boot: il modulo Switchboard e auto-sufficiente per le migration.
/// La connection string arriva da Boot/appsettings.json/appsettings.{Environment}.json,
/// sovrascrivibile con la variabile d'ambiente ConnectionStrings__SummerBooking (BuildingBlocks.Infrastructure.Persistence.ModuleConnectionStrings).
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<DbContext>
{
	public DbContext CreateDbContext(string[] args)
	{
		var dataSourceBuilder = new NpgsqlDataSourceBuilder(ModuleConnectionStrings.ResolveForDesignTime());

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

		var dataSource = dataSourceBuilder.Build();

		var optionsBuilder = new DbContextOptionsBuilder<DbContext>()
			.UseNpgsql(dataSource, npgsql => npgsql
				.MigrationsHistoryTable(ModuleConnectionStrings.MigrationsHistoryTable, DbContext.Schema)
				.CommandTimeout(ModuleConnectionStrings.DesignTimeCommandTimeoutSeconds))
			.UseSnakeCaseNamingConvention();

		return new DbContext(optionsBuilder.Options, new SystemDateTimeProvider());
	}
}
