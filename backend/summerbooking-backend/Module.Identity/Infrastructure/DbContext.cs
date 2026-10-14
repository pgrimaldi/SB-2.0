using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Module.Identity.Domain.Entities;
using Module.Identity.Domain.Enums;
using Shared.Infrastructure;
using Shared.Localization;

namespace Module.Identity.Infrastructure;

/// <summary>
/// Contesto di persistenza del modulo Identity, schema PostgreSQL "identity".
/// Nome della classe = solo "DbContext": il namespace la disambigua dagli altri moduli.
/// Mai referenziato fuori da Module.Identity.Infrastructure.
/// </summary>
public sealed class DbContext : Microsoft.EntityFrameworkCore.DbContext
{
	public const string Schema = "identity";

	private readonly IDateTimeProvider _dateTimeProvider;

	public DbContext(DbContextOptions<DbContext> options, IDateTimeProvider dateTimeProvider) : base(options)
	{
		_dateTimeProvider = dateTimeProvider;
	}

	// DbSet<TEntity> di ogni aggregato/entita del modulo — popolati
	// dall'implementazione del modulo, uno per tabella con IEntityTypeConfiguration corrispondente.
	public DbSet<Account> Accounts => Set<Account>();
	public DbSet<Role> Roles => Set<Role>();
	public DbSet<PropertySecuritySettings> PropertySecuritySettings => Set<PropertySecuritySettings>();
	public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
	public DbSet<AuthChallenge> AuthChallenges => Set<AuthChallenge>();
	public DbSet<ContractAcceptanceEvent> ContractAcceptanceEvents => Set<ContractAcceptanceEvent>();
	public DbSet<ContractDocument> ContractDocuments => Set<ContractDocument>();
	public DbSet<LicensePlanTranslation> LicensePlanTranslations => Set<LicensePlanTranslation>();
	public DbSet<LicensePlan> LicensePlans => Set<LicensePlan>();
	public DbSet<SubscriptionPeriod> SubscriptionPeriods => Set<SubscriptionPeriod>();
	public DbSet<Subscription> Subscriptions => Set<Subscription>();
	public DbSet<BathhouseLike> BathhouseLikes => Set<BathhouseLike>();
	public DbSet<Customer> Customers => Set<Customer>();

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
		modelBuilder.HasPostgresEnum<LoginMethod>(Schema, "login_method");
		modelBuilder.HasPostgresEnum<AppLanguage>(Schema, "ui_language");
		// storage_origin: usato da access_users e account_contract_documents; resta locale a "identity"
		// (non un tipo davvero cross-schema) — registrato una sola volta qui.
		modelBuilder.HasPostgresEnum<StorageOrigin>(Schema, "storage_origin");
		modelBuilder.HasPostgresEnum<SecondFactorChannel>(Schema, "second_factor_channel");
		modelBuilder.HasPostgresEnum<RefreshRevokeReason>(Schema, "refresh_revoke_reason");
		modelBuilder.HasPostgresEnum<AuthChallengePurpose>(Schema, "auth_challenge_purpose");
		modelBuilder.HasPostgresEnum<AuthChallengeChannel>(Schema, "auth_challenge_channel");
		modelBuilder.HasPostgresEnum<AuthChallengeInvalidation>(Schema, "auth_challenge_invalidation");
		modelBuilder.HasPostgresEnum<ContractAcceptanceEventType>(Schema, "contract_acceptance_event");
		modelBuilder.HasPostgresEnum<ContractDocumentStatus>(Schema, "contract_document_status");
		// subscription_term_type: condiviso fra account_subscriptions e account_subscription_periods.
		modelBuilder.HasPostgresEnum<SubscriptionTermType>(Schema, "subscription_term_type");
		modelBuilder.HasPostgresEnum<SubscriptionStatus>(Schema, "subscription_status");
		modelBuilder.HasPostgresEnum<SubscriptionPeriodKind>(Schema, "subscription_period_kind");
		modelBuilder.HasPostgresEnum<SubscriptionPeriodCloseReason>(Schema, "subscription_period_close_reason");
		modelBuilder.HasPostgresEnum<CustomerSource>(Schema, "customer_source");
		modelBuilder.HasPostgresEnum<NotificationSoundScope>(Schema, "notification_sound_scope");

		modelBuilder.ApplyConfigurationsFromAssembly(typeof(DbContext).Assembly);

		ModuleDbContextConventions.ApplySoftDeleteQueryFilter(modelBuilder);
	}
}
