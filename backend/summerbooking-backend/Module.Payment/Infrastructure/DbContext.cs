using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Module.Payment.Domain.Entities;
using Module.Payment.Domain.Enums;
using Shared.Infrastructure;

namespace Module.Payment.Infrastructure;

/// <summary>
/// Contesto di persistenza del modulo Payment, schema PostgreSQL "payment".
/// Nome della classe = solo "DbContext": il namespace la disambigua dagli altri moduli.
/// Mai referenziato fuori da Module.Payment.Infrastructure.
/// </summary>
public sealed class DbContext : Microsoft.EntityFrameworkCore.DbContext
{
	public const string Schema = "payment";

	private readonly IDateTimeProvider _dateTimeProvider;

	public DbContext(DbContextOptions<DbContext> options, IDateTimeProvider dateTimeProvider) : base(options)
	{
		_dateTimeProvider = dateTimeProvider;
	}

	// DbSet<TEntity> di ogni aggregato/entita del modulo — popolati
	// dall'implementazione del modulo, uno per tabella con IEntityTypeConfiguration corrispondente.
	// LedgerEntryLine, PosChargeItem e WalletTransactionItem sono owned type (OwnsMany dentro la
	// Configuration del genitore): nessun DbSet proprio.
	public DbSet<Charge> Charges => Set<Charge>();
	public DbSet<ConnectedAccount> ConnectedAccounts => Set<ConnectedAccount>();
	public DbSet<CustomerGatewayProfile> CustomerGatewayProfiles => Set<CustomerGatewayProfile>();
	public DbSet<GatewayCommissionSetting> GatewayCommissionSettings => Set<GatewayCommissionSetting>();
	public DbSet<LedgerEntry> LedgerEntries => Set<LedgerEntry>();
	public DbSet<PaymentRequestLink> PaymentRequestLinks => Set<PaymentRequestLink>();
	public DbSet<PosCharge> PosCharges => Set<PosCharge>();
	public DbSet<Refund> Refunds => Set<Refund>();
	public DbSet<WalletAccount> WalletAccounts => Set<WalletAccount>();
	public DbSet<WalletTransaction> WalletTransactions => Set<WalletTransaction>();
	public DbSet<WebhookEvent> WebhookEvents => Set<WebhookEvent>();

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
		modelBuilder.HasPostgresEnum<TransactionSubject>(Schema, "transaction_subject");
		modelBuilder.HasPostgresEnum<OnlinePaymentType>(Schema, "online_payment_type");
		modelBuilder.HasPostgresEnum<SalesChannel>(Schema, "sales_channel");
		modelBuilder.HasPostgresEnum<ChargeStatus>(Schema, "charge_status");
		modelBuilder.HasPostgresEnum<ConnectOnboardingState>(Schema, "connect_onboarding_state");
		modelBuilder.HasPostgresEnum<CommissionExemption>(Schema, "commission_exemption");
		modelBuilder.HasPostgresEnum<LedgerEntryType>(Schema, "ledger_entry_type");
		modelBuilder.HasPostgresEnum<LedgerDirection>(Schema, "ledger_direction");
		modelBuilder.HasPostgresEnum<PaymentMode>(Schema, "payment_mode");
		modelBuilder.HasPostgresEnum<LedgerSource>(Schema, "ledger_source");
		modelBuilder.HasPostgresEnum<LedgerLineKind>(Schema, "ledger_line_kind");
		modelBuilder.HasPostgresEnum<PaymentRequestStatus>(Schema, "payment_request_status");
		modelBuilder.HasPostgresEnum<PaymentRequestOrigin>(Schema, "payment_request_origin");
		modelBuilder.HasPostgresEnum<PosSystem>(Schema, "pos_system");
		modelBuilder.HasPostgresEnum<PosChargeStatus>(Schema, "pos_charge_status");
		modelBuilder.HasPostgresEnum<RefundStatus>(Schema, "refund_status");
		modelBuilder.HasPostgresEnum<RefundReason>(Schema, "refund_reason");
		modelBuilder.HasPostgresEnum<WalletKind>(Schema, "wallet_kind");
		modelBuilder.HasPostgresEnum<WalletStatus>(Schema, "wallet_status");
		modelBuilder.HasPostgresEnum<WalletMovementType>(Schema, "wallet_movement_type");
		modelBuilder.HasPostgresEnum<WalletMovementSource>(Schema, "wallet_movement_source");
		modelBuilder.HasPostgresEnum<WebhookProvider>(Schema, "webhook_provider");
		modelBuilder.HasPostgresEnum<WebhookProcessingStatus>(Schema, "webhook_processing_status");

		modelBuilder.ApplyConfigurationsFromAssembly(typeof(DbContext).Assembly);

		ModuleDbContextConventions.ApplySoftDeleteQueryFilter(modelBuilder);
	}
}
