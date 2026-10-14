using BuildingBlocks.Infrastructure.Persistence;
using BuildingBlocks.Infrastructure.Retention;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Module.Fiskaly.Domain.Enums;
using Npgsql;
using Shared.Infrastructure;

namespace Module.Fiskaly.Infrastructure.Infras;

/// <summary>
/// Registrazione DI della persistenza del modulo Fiskaly, richiamata da Module.AddFiskalyModule().
/// Stessa costruzione (data source + enum mapping) della DesignTimeDbContextFactory, ma con
/// AddDbContext (Scoped) per l'uso a runtime dentro Boot.
/// </summary>
public static class ModulePersistenceRegistration
{
	public static IServiceCollection AddModuleFiskalyPersistence(this IServiceCollection services, IConfiguration configuration)
	{
		services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

		var dataSourceBuilder = new NpgsqlDataSourceBuilder(ModuleConnectionStrings.Resolve(configuration));

		// Mappatura degli enum nativi PostgreSQL del modulo (dataSourceBuilder.MapEnum<TEnum>("fiskaly...."))
		// va qui, in coppia con le modelBuilder.HasPostgresEnum<TEnum> di DbContext.OnModelCreating
		// (stessa lista di DesignTimeDbContextFactory).
		dataSourceBuilder.MapEnum<FiscalSetupLevel>("fiskaly.fiscal_setup_level");
		dataSourceBuilder.MapEnum<FiskalyOrgState>("fiskaly.org_state");
		dataSourceBuilder.MapEnum<FiskalyEntityState>("fiskaly.entity_state");
		dataSourceBuilder.MapEnum<FiskalyEntityMode>("fiskaly.entity_mode");
		dataSourceBuilder.MapEnum<FiskalySystemType>("fiskaly.system_type");
		dataSourceBuilder.MapEnum<FiskalyMediaOrigin>("fiskaly.media_origin");
		dataSourceBuilder.MapEnum<FiskalyMediaKind>("fiskaly.media_kind");
		dataSourceBuilder.MapEnum<ReceiptKind>("fiskaly.receipt_kind");
		dataSourceBuilder.MapEnum<ReceiptSource>("fiskaly.receipt_source");
		dataSourceBuilder.MapEnum<ReceiptRecordState>("fiskaly.receipt_record_state");
		dataSourceBuilder.MapEnum<RecordMode>("fiskaly.record_mode");
		dataSourceBuilder.MapEnum<FiskalyPaymentMode>("fiskaly.payment_mode");
		dataSourceBuilder.MapEnum<LanguageCode>("fiskaly.language_code");
		dataSourceBuilder.MapEnum<LineItemType>("fiskaly.line_item_type");
		dataSourceBuilder.MapEnum<FiskalyEntryType>("fiskaly.entry_type");
		dataSourceBuilder.MapEnum<FiskalyEntryCategory>("fiskaly.entry_category");
		dataSourceBuilder.MapEnum<FiskalyVatKind>("fiskaly.vat_kind");
		dataSourceBuilder.MapEnum<ReceiptLogStep>("fiskaly.receipt_log_step");
		dataSourceBuilder.MapEnum<LogResult>("fiskaly.log_result");
		dataSourceBuilder.MapEnum<FiskalyRecordState>("fiskaly.fiskaly_record_state");
		dataSourceBuilder.MapEnum<FiskalyPaymentType>("fiskaly.payment_type");
		dataSourceBuilder.MapEnum<CardKind>("fiskaly.card_kind");
		dataSourceBuilder.MapEnum<VoucherKind>("fiskaly.voucher_kind");
		dataSourceBuilder.MapEnum<OutstandingConcept>("fiskaly.outstanding_concept");

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
