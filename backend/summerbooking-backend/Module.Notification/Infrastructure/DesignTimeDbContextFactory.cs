using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Module.Notification.Domain.Enums;
using Npgsql;
using Shared.Infrastructure;

namespace Module.Notification.Infrastructure;

/// <summary>
/// Factory usata da "dotnet ef" per creare il DbContext a design-time (migration add/update/...)
/// senza passare dal progetto Boot: il modulo Notification e auto-sufficiente per le migration.
/// La connection string arriva da Boot/appsettings.json/appsettings.{Environment}.json,
/// sovrascrivibile con la variabile d'ambiente ConnectionStrings__SummerBooking (BuildingBlocks.Infrastructure.Persistence.ModuleConnectionStrings).
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<DbContext>
{
	public DbContext CreateDbContext(string[] args)
	{
		var dataSourceBuilder = new NpgsqlDataSourceBuilder(ModuleConnectionStrings.ResolveForDesignTime());

		// Mappatura degli enum nativi PostgreSQL del modulo (dataSourceBuilder.MapEnum<TEnum>("notification...."))
		// va qui, in coppia con le modelBuilder.HasPostgresEnum<TEnum> di DbContext.OnModelCreating.
			// Stessa lista identica di Infrastructure/Infras/ModulePersistenceRegistration.cs.
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

			var dataSource = dataSourceBuilder.Build();

		var optionsBuilder = new DbContextOptionsBuilder<DbContext>()
			.UseNpgsql(dataSource, npgsql => npgsql
				.MigrationsHistoryTable(ModuleConnectionStrings.MigrationsHistoryTable, DbContext.Schema)
				.CommandTimeout(ModuleConnectionStrings.DesignTimeCommandTimeoutSeconds))
			.UseSnakeCaseNamingConvention();

		return new DbContext(optionsBuilder.Options, new SystemDateTimeProvider());
	}
}
