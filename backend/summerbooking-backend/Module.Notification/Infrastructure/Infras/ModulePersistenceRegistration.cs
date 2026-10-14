using BuildingBlocks.Infrastructure.Persistence;
using BuildingBlocks.Infrastructure.Retention;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Module.Notification.Domain.Enums;
using Npgsql;
using Shared.Infrastructure;

namespace Module.Notification.Infrastructure.Infras;

/// <summary>
/// Registrazione DI della persistenza del modulo Notification, richiamata da Module.AddNotificationModule().
/// Stessa costruzione (data source + enum mapping) della DesignTimeDbContextFactory, ma con
/// AddDbContext (Scoped) per l'uso a runtime dentro Boot.
/// </summary>
public static class ModulePersistenceRegistration
{
	public static IServiceCollection AddModuleNotificationPersistence(this IServiceCollection services, IConfiguration configuration)
	{
		services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

		var dataSourceBuilder = new NpgsqlDataSourceBuilder(ModuleConnectionStrings.Resolve(configuration));

		// Mappatura degli enum nativi PostgreSQL del modulo (dataSourceBuilder.MapEnum<TEnum>("notification...."))
		// va qui, in coppia con le modelBuilder.HasPostgresEnum<TEnum> di DbContext.OnModelCreating.
			// Stessa lista identica di Infrastructure/DesignTimeDbContextFactory.cs.
			dataSourceBuilder.MapEnum<NotificationChannel>("notification.channel");
			dataSourceBuilder.MapEnum<DeliveryKind>("notification.delivery_kind");
			dataSourceBuilder.MapEnum<NotificationSubjectType>("notification.subject_type");
			dataSourceBuilder.MapEnum<RecipientType>("notification.recipient_type");
			dataSourceBuilder.MapEnum<DeliveryStatus>("notification.delivery_status");
			dataSourceBuilder.MapEnum<DeliveryProvider>("notification.delivery_provider");
			dataSourceBuilder.MapEnum<SkipReason>("notification.skip_reason");
			dataSourceBuilder.MapEnum<SmtpSecurity>("notification.smtp_security");
			dataSourceBuilder.MapEnum<ClaimStatus>("notification.claim_status");
			dataSourceBuilder.MapEnum<ClaimSourceChannel>("notification.claim_source_channel");
			dataSourceBuilder.MapEnum<AutomationTriggerKind>("notification.automation_trigger_kind");
			dataSourceBuilder.MapEnum<AutomationKind>("notification.automation_kind");
			dataSourceBuilder.MapEnum<AutomationState>("notification.automation_state");
			dataSourceBuilder.MapEnum<AutomationScheduleType>("notification.automation_schedule_type");
			dataSourceBuilder.MapEnum<MarketingParentKind>("notification.marketing_parent_kind");
			dataSourceBuilder.MapEnum<RecipientStatus>("notification.recipient_status");
			dataSourceBuilder.MapEnum<RecipientSkipReason>("notification.recipient_skip_reason");
			dataSourceBuilder.MapEnum<ListMemberSource>("notification.list_member_source");
			dataSourceBuilder.MapEnum<ContactListOrigin>("notification.contact_list_origin");
			dataSourceBuilder.MapEnum<MarketingConsentStatus>("notification.marketing_consent_status");
			dataSourceBuilder.MapEnum<ConsentSource>("notification.consent_source");
			dataSourceBuilder.MapEnum<CampaignState>("notification.campaign_state");
			dataSourceBuilder.MapEnum<EventReminderOffset>("notification.event_reminder_offset");
			dataSourceBuilder.MapEnum<ReminderStatus>("notification.reminder_status");
			dataSourceBuilder.MapEnum<EventRepeatType>("notification.event_repeat_type");
			dataSourceBuilder.MapEnum<EventRepeatUnit>("notification.event_repeat_unit");
			dataSourceBuilder.MapEnum<NewsletterStatus>("notification.newsletter_status");
			dataSourceBuilder.MapEnum<NewsletterSource>("notification.newsletter_source");
			dataSourceBuilder.MapEnum<SuppressionReason>("notification.suppression_reason");
			dataSourceBuilder.MapEnum<ChannelOrAll>("notification.channel_or_all");
			dataSourceBuilder.MapEnum<NotificationMediaOrigin>("notification.media_origin");
			dataSourceBuilder.MapEnum<NotificationMediaKind>("notification.media_kind");
			dataSourceBuilder.MapEnum<PushPlatform>("notification.push_platform");
			dataSourceBuilder.MapEnum<TemplateKind>("notification.template_kind");
			dataSourceBuilder.MapEnum<TemplateChannel>("notification.template_channel");
			dataSourceBuilder.MapEnum<InAppKind>("notification.inapp_kind");
			dataSourceBuilder.MapEnum<InAppSubjectKind>("notification.inapp_subject_kind");
			dataSourceBuilder.MapEnum<InAppVisibility>("notification.inapp_visibility");
			dataSourceBuilder.MapEnum<StaffChannel>("notification.staff_channel");
			dataSourceBuilder.MapEnum<NotificationKind>("notification.notification_kind");

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
