using BuildingBlocks.Infrastructure.Persistence;
using BuildingBlocks.Infrastructure.Retention;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Module.Identity.Domain.Enums;
using Npgsql;
using Shared.Infrastructure;

namespace Module.Identity.Infrastructure.Infras;

/// <summary>
/// Registrazione DI della persistenza del modulo Identity, richiamata da Module.AddIdentityModule().
/// Stessa costruzione (data source + enum mapping) della DesignTimeDbContextFactory, ma con
/// AddDbContext (Scoped) per l'uso a runtime dentro Boot.
/// </summary>
public static class ModulePersistenceRegistration
{
	public static IServiceCollection AddModuleIdentityPersistence(this IServiceCollection services, IConfiguration configuration)
	{
		services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

		var dataSourceBuilder = new NpgsqlDataSourceBuilder(ModuleConnectionStrings.Resolve(configuration));

		// Enum nativi: elenco unico in IdentityEnumMappings, applicato qui e nelle opzioni EF sotto.
		IdentityEnumMappings.Apply(dataSourceBuilder);

		// Tetto del pool: ogni modulo ha il PROPRIO NpgsqlDataSource, quindi l'host ne ha
		// uno per modulo. Senza limite sarebbero 100 ciascuno (default Npgsql) contro un
		// max_connections che di norma e' 100 in tutto. Vedi ModuleConnectionStrings.
		ModuleConnectionStrings.ApplyModulePoolLimit(dataSourceBuilder, configuration);

		var dataSource = dataSourceBuilder.Build();

		services.AddDbContext<DbContext>((_, optionsBuilder) =>
			optionsBuilder.UseNpgsql(dataSource, npgsql => IdentityEnumMappings.Apply(npgsql).MigrationsHistoryTable(ModuleConnectionStrings.MigrationsHistoryTable, DbContext.Schema))
			.UseSnakeCaseNamingConvention());

		// Job di retention del modulo: applica le policy di privacy_retention_policies che
		// riguardano QUESTO schema, sulla connessione di questo modulo. Chi orchestra risolve
		// IEnumerable<IModuleRetentionJob> e non conosce nessun DbContext.
		services.AddModuleRetention<DbContext>(DbContext.Schema);

		return services;
	}
}
