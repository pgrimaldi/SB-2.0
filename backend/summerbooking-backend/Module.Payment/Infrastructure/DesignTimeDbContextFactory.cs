using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Module.Payment.Domain.Enums;
using Npgsql;
using Shared.Infrastructure;

namespace Module.Payment.Infrastructure;

/// <summary>
/// Factory usata da "dotnet ef" per creare il DbContext a design-time (migration add/update/...)
/// senza passare dal progetto Boot: il modulo Payment e auto-sufficiente per le migration.
/// La connection string arriva da Boot/appsettings.json/appsettings.{Environment}.json,
/// sovrascrivibile con la variabile d'ambiente ConnectionStrings__SummerBooking (BuildingBlocks.Infrastructure.Persistence.ModuleConnectionStrings).
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<DbContext>
{
	public DbContext CreateDbContext(string[] args)
	{
		var dataSourceBuilder = new NpgsqlDataSourceBuilder(ModuleConnectionStrings.ResolveForDesignTime());

		// Mappatura degli enum nativi PostgreSQL del modulo, in coppia con le
		// modelBuilder.HasPostgresEnum<TEnum> di DbContext.OnModelCreating (stessa lista di
		// ModulePersistenceRegistration).
		dataSourceBuilder.MapEnum<TransactionSubject>("payment.transaction_subject");
		dataSourceBuilder.MapEnum<OnlinePaymentType>("payment.online_payment_type");
		dataSourceBuilder.MapEnum<SalesChannel>("payment.sales_channel");
		dataSourceBuilder.MapEnum<ChargeStatus>("payment.charge_status");
		dataSourceBuilder.MapEnum<ConnectOnboardingState>("payment.connect_onboarding_state");
		dataSourceBuilder.MapEnum<CommissionExemption>("payment.commission_exemption");
		dataSourceBuilder.MapEnum<LedgerEntryType>("payment.ledger_entry_type");
		dataSourceBuilder.MapEnum<LedgerDirection>("payment.ledger_direction");
		dataSourceBuilder.MapEnum<PaymentMode>("payment.payment_mode");
		dataSourceBuilder.MapEnum<LedgerSource>("payment.ledger_source");
		dataSourceBuilder.MapEnum<LedgerLineKind>("payment.ledger_line_kind");
		dataSourceBuilder.MapEnum<PaymentRequestStatus>("payment.payment_request_status");
		dataSourceBuilder.MapEnum<PaymentRequestOrigin>("payment.payment_request_origin");
		dataSourceBuilder.MapEnum<PosSystem>("payment.pos_system");
		dataSourceBuilder.MapEnum<PosChargeStatus>("payment.pos_charge_status");
		dataSourceBuilder.MapEnum<RefundStatus>("payment.refund_status");
		dataSourceBuilder.MapEnum<RefundReason>("payment.refund_reason");
		dataSourceBuilder.MapEnum<WalletKind>("payment.wallet_kind");
		dataSourceBuilder.MapEnum<WalletStatus>("payment.wallet_status");
		dataSourceBuilder.MapEnum<WalletMovementType>("payment.wallet_movement_type");
		dataSourceBuilder.MapEnum<WalletMovementSource>("payment.wallet_movement_source");
		dataSourceBuilder.MapEnum<WebhookProvider>("payment.webhook_provider");
		dataSourceBuilder.MapEnum<WebhookProcessingStatus>("payment.webhook_processing_status");

		var dataSource = dataSourceBuilder.Build();

		var optionsBuilder = new DbContextOptionsBuilder<DbContext>()
			.UseNpgsql(dataSource, npgsql => npgsql
				.MigrationsHistoryTable(ModuleConnectionStrings.MigrationsHistoryTable, DbContext.Schema)
				.CommandTimeout(ModuleConnectionStrings.DesignTimeCommandTimeoutSeconds))
			.UseSnakeCaseNamingConvention();

		return new DbContext(optionsBuilder.Options, new SystemDateTimeProvider());
	}
}
