using BuildingBlocks.Infrastructure.Persistence;
using BuildingBlocks.Infrastructure.Retention;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Module.Payment.Domain.Enums;
using Npgsql;
using Shared.Infrastructure;

namespace Module.Payment.Infrastructure.Infras;

/// <summary>
/// Registrazione DI della persistenza del modulo Payment, richiamata da Module.AddPaymentModule().
/// Stessa costruzione (data source + enum mapping) della DesignTimeDbContextFactory, ma con
/// AddDbContext (Scoped) per l'uso a runtime dentro Boot.
/// </summary>
public static class ModulePersistenceRegistration
{
	public static IServiceCollection AddModulePaymentPersistence(this IServiceCollection services, IConfiguration configuration)
	{
		services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

		var dataSourceBuilder = new NpgsqlDataSourceBuilder(ModuleConnectionStrings.Resolve(configuration));

		// Mappatura degli enum nativi PostgreSQL del modulo, in coppia con le
		// modelBuilder.HasPostgresEnum<TEnum> di DbContext.OnModelCreating (stessa lista di
		// DesignTimeDbContextFactory).
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
