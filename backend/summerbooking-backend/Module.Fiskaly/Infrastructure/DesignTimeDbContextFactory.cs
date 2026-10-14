using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Module.Fiskaly.Domain.Enums;
using Npgsql;
using Shared.Infrastructure;

namespace Module.Fiskaly.Infrastructure;

/// <summary>
/// Factory usata da "dotnet ef" per creare il DbContext a design-time (migration add/update/...)
/// senza passare dal progetto Boot: il modulo Fiskaly e auto-sufficiente per le migration.
/// La connection string arriva da Boot/appsettings.json/appsettings.{Environment}.json,
/// sovrascrivibile con la variabile d'ambiente ConnectionStrings__SummerBooking (BuildingBlocks.Infrastructure.Persistence.ModuleConnectionStrings).
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<DbContext>
{
	public DbContext CreateDbContext(string[] args)
	{
		var dataSourceBuilder = new NpgsqlDataSourceBuilder(ModuleConnectionStrings.ResolveForDesignTime());

		// Mappatura degli enum nativi PostgreSQL del modulo (dataSourceBuilder.MapEnum<TEnum>("fiskaly...."))
		// va qui, in coppia con le modelBuilder.HasPostgresEnum<TEnum> di DbContext.OnModelCreating
		// (stessa lista di ModulePersistenceRegistration).
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

		var dataSource = dataSourceBuilder.Build();

		var optionsBuilder = new DbContextOptionsBuilder<DbContext>()
			.UseNpgsql(dataSource, npgsql => npgsql
				.MigrationsHistoryTable(ModuleConnectionStrings.MigrationsHistoryTable, DbContext.Schema)
				.CommandTimeout(ModuleConnectionStrings.DesignTimeCommandTimeoutSeconds))
			.UseSnakeCaseNamingConvention();

		return new DbContext(optionsBuilder.Options, new SystemDateTimeProvider());
	}
}
