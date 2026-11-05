using BuildingBlocks.Infrastructure.Persistence;
using BuildingBlocks.Infrastructure.Retention;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Shared.Infrastructure;

namespace Module.Catalog.Infrastructure.Infras;

/// <summary>
/// Registrazione DI della persistenza del modulo Catalog, richiamata da Module.AddCatalogModule().
/// Stessa costruzione (data source + enum mapping) della DesignTimeDbContextFactory, ma con
/// AddDbContext (Scoped) per l'uso a runtime dentro Boot.
/// </summary>
public static class ModulePersistenceRegistration
{
	public static IServiceCollection AddModuleCatalogPersistence(this IServiceCollection services, IConfiguration configuration)
	{
		services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

		var dataSourceBuilder = new NpgsqlDataSourceBuilder(ModuleConnectionStrings.Resolve(configuration));

		CatalogEnumMappings.Apply(dataSourceBuilder);

		// Tetto del pool: ogni modulo ha il PROPRIO NpgsqlDataSource, quindi l'host ne ha
		// uno per modulo. Senza limite sarebbero 100 ciascuno (default Npgsql) contro un
		// max_connections che di norma e' 100 in tutto. Vedi ModuleConnectionStrings.
		ModuleConnectionStrings.ApplyModulePoolLimit(dataSourceBuilder, configuration);

		var dataSource = dataSourceBuilder.Build();

		services.AddDbContext<DbContext>((_, optionsBuilder) =>
			optionsBuilder.UseNpgsql(dataSource, npgsql => CatalogEnumMappings.Apply(npgsql)
				.MigrationsHistoryTable(ModuleConnectionStrings.MigrationsHistoryTable, DbContext.Schema))
			.UseSnakeCaseNamingConvention());

		// Job di retention del modulo: applica le policy di privacy_retention_policies che
		// riguardano QUESTO schema, sulla connessione di questo modulo. Chi orchestra risolve
		// IEnumerable<IModuleRetentionJob> e non conosce nessun DbContext.
		services.AddModuleRetention<DbContext>(DbContext.Schema);

		// Job SCRITTO A MANO per l'unica policy il cui perimetro non e' dichiarabile: le foto degli
		// stabilimenti cessati, dove la regola e' «oltre le prime tre per sort_order, mai la copertina».
		// Il motore la salta di proposito (target_filter NULL), quindi non c'e' doppio lavoro.
		services.AddBespokeRetention<CeasedPropertyPhotoRetentionJob, DbContext>();

		return services;
	}
}
