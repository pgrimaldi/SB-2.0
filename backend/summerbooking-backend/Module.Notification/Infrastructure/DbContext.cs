using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Module.Notification.Domain.Entities;
using Module.Notification.Domain.Enums;
using Shared.Infrastructure;

namespace Module.Notification.Infrastructure;

/// <summary>
/// Contesto di persistenza del modulo Notification, schema PostgreSQL "notification".
/// Nome della classe = solo "DbContext": il namespace la disambigua dagli altri moduli.
/// Mai referenziato fuori da Module.Notification.Infrastructure.
/// </summary>
public sealed class DbContext : Microsoft.EntityFrameworkCore.DbContext
{
	public const string Schema = "notification";

	private readonly IDateTimeProvider _dateTimeProvider;

	public DbContext(DbContextOptions<DbContext> options, IDateTimeProvider dateTimeProvider) : base(options)
	{
		_dateTimeProvider = dateTimeProvider;
	}

	// DbSet<TEntity> di ogni aggregato/entita del modulo — popolati
	// dall'implementazione del modulo, uno per tabella con IEntityTypeConfiguration corrispondente.
	public DbSet<Delivery> Deliveries => Set<Delivery>();
	public DbSet<EmailChannelSettings> EmailChannelSettings => Set<EmailChannelSettings>();
	public DbSet<EstablishmentClaimRequest> EstablishmentClaimRequests => Set<EstablishmentClaimRequest>();
	public DbSet<InAppMessage> InAppMessages => Set<InAppMessage>();
	public DbSet<InAppMessageRead> InAppMessageReads => Set<InAppMessageRead>();
	public DbSet<AutomationTrigger> AutomationTriggers => Set<AutomationTrigger>();
	public DbSet<MarketingAutomation> MarketingAutomations => Set<MarketingAutomation>();
	public DbSet<CampaignRecipient> CampaignRecipients => Set<CampaignRecipient>();
	public DbSet<ContactListMembership> ContactListMemberships => Set<ContactListMembership>();
	public DbSet<ContactList> ContactLists => Set<ContactList>();
	public DbSet<MarketingContact> MarketingContacts => Set<MarketingContact>();
	public DbSet<EmailCampaign> EmailCampaigns => Set<EmailCampaign>();
	public DbSet<EventReminder> EventReminders => Set<EventReminder>();
	public DbSet<MarketingEvent> MarketingEvents => Set<MarketingEvent>();
	public DbSet<NewsletterSubscription> NewsletterSubscriptions => Set<NewsletterSubscription>();
	public DbSet<MarketingSuppression> MarketingSuppressions => Set<MarketingSuppression>();
	public DbSet<MediaAsset> MediaAssets => Set<MediaAsset>();
	public DbSet<PushDeviceToken> PushDeviceTokens => Set<PushDeviceToken>();
	public DbSet<TemplateAttachment> TemplateAttachments => Set<TemplateAttachment>();
	public DbSet<MessageTemplate> Templates => Set<MessageTemplate>();
	public DbSet<UserChannelPreferences> UserChannelPreferences => Set<UserChannelPreferences>();

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
		// PlaceholderKey NON è qui: resta un vocabolario di codice (text[] su notification_templates),
		// non un ENUM Postgres nativo.
		modelBuilder.HasPostgresEnum<NotificationChannel>(Schema, "channel");
		modelBuilder.HasPostgresEnum<DeliveryKind>(Schema, "delivery_kind");
		modelBuilder.HasPostgresEnum<NotificationSubjectType>(Schema, "subject_type");
		modelBuilder.HasPostgresEnum<RecipientType>(Schema, "recipient_type");
		modelBuilder.HasPostgresEnum<DeliveryStatus>(Schema, "delivery_status");
		modelBuilder.HasPostgresEnum<DeliveryProvider>(Schema, "delivery_provider");
		modelBuilder.HasPostgresEnum<SkipReason>(Schema, "skip_reason");
		modelBuilder.HasPostgresEnum<SmtpSecurity>(Schema, "smtp_security");
		modelBuilder.HasPostgresEnum<ClaimStatus>(Schema, "claim_status");
		modelBuilder.HasPostgresEnum<ClaimSourceChannel>(Schema, "claim_source_channel");
		modelBuilder.HasPostgresEnum<AutomationTriggerKind>(Schema, "automation_trigger_kind");
		modelBuilder.HasPostgresEnum<AutomationKind>(Schema, "automation_kind");
		modelBuilder.HasPostgresEnum<AutomationState>(Schema, "automation_state");
		modelBuilder.HasPostgresEnum<AutomationScheduleType>(Schema, "automation_schedule_type");
		modelBuilder.HasPostgresEnum<MarketingParentKind>(Schema, "marketing_parent_kind");
		modelBuilder.HasPostgresEnum<RecipientStatus>(Schema, "recipient_status");
		modelBuilder.HasPostgresEnum<RecipientSkipReason>(Schema, "recipient_skip_reason");
		modelBuilder.HasPostgresEnum<ListMemberSource>(Schema, "list_member_source");
		modelBuilder.HasPostgresEnum<ContactListOrigin>(Schema, "contact_list_origin");
		modelBuilder.HasPostgresEnum<MarketingConsentStatus>(Schema, "marketing_consent_status");
		modelBuilder.HasPostgresEnum<ConsentSource>(Schema, "consent_source");
		modelBuilder.HasPostgresEnum<CampaignState>(Schema, "campaign_state");
		modelBuilder.HasPostgresEnum<EventReminderOffset>(Schema, "event_reminder_offset");
		modelBuilder.HasPostgresEnum<ReminderStatus>(Schema, "reminder_status");
		modelBuilder.HasPostgresEnum<EventRepeatType>(Schema, "event_repeat_type");
		modelBuilder.HasPostgresEnum<EventRepeatUnit>(Schema, "event_repeat_unit");
		modelBuilder.HasPostgresEnum<NewsletterStatus>(Schema, "newsletter_status");
		modelBuilder.HasPostgresEnum<NewsletterSource>(Schema, "newsletter_source");
		modelBuilder.HasPostgresEnum<SuppressionReason>(Schema, "suppression_reason");
		modelBuilder.HasPostgresEnum<ChannelOrAll>(Schema, "channel_or_all");
		modelBuilder.HasPostgresEnum<NotificationMediaOrigin>(Schema, "media_origin");
		modelBuilder.HasPostgresEnum<NotificationMediaKind>(Schema, "media_kind");
		modelBuilder.HasPostgresEnum<PushPlatform>(Schema, "push_platform");
		modelBuilder.HasPostgresEnum<TemplateKind>(Schema, "template_kind");
		modelBuilder.HasPostgresEnum<TemplateChannel>(Schema, "template_channel");
		modelBuilder.HasPostgresEnum<InAppKind>(Schema, "inapp_kind");
		modelBuilder.HasPostgresEnum<InAppSubjectKind>(Schema, "inapp_subject_kind");
		modelBuilder.HasPostgresEnum<InAppVisibility>(Schema, "inapp_visibility");
		modelBuilder.HasPostgresEnum<StaffChannel>(Schema, "staff_channel");
		modelBuilder.HasPostgresEnum<NotificationKind>(Schema, "notification_kind");

		modelBuilder.ApplyConfigurationsFromAssembly(typeof(DbContext).Assembly);

		ModuleDbContextConventions.ApplySoftDeleteQueryFilter(modelBuilder);
	}
}
