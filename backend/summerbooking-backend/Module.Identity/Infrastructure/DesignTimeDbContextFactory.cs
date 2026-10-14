using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Module.Identity.Domain.Enums;
using Module.Identity.Infrastructure.Infras;
using Npgsql;
using Shared.Infrastructure;

namespace Module.Identity.Infrastructure;

/// <summary>
/// Factory usata da "dotnet ef" per creare il DbContext a design-time (migration add/update/...)
/// senza passare dal progetto Boot: il modulo Identity e auto-sufficiente per le migration.
/// La connection string arriva da Boot/appsettings.json/appsettings.{Environment}.json,
/// sovrascrivibile con la variabile d'ambiente ConnectionStrings__SummerBooking (BuildingBlocks.Infrastructure.Persistence.ModuleConnectionStrings).
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<DbContext>
{
	public DbContext CreateDbContext(string[] args)
	{
		var dataSourceBuilder = new NpgsqlDataSourceBuilder(ModuleConnectionStrings.ResolveForDesignTime());

		// Enum nativi: stesso elenco del runtime (IdentityEnumMappings), sul data source e nelle opzioni EF.
		IdentityEnumMappings.Apply(dataSourceBuilder);

		var dataSource = dataSourceBuilder.Build();

		var optionsBuilder = new DbContextOptionsBuilder<DbContext>()
			.UseNpgsql(dataSource, npgsql => IdentityEnumMappings.Apply(npgsql)
				.MigrationsHistoryTable(ModuleConnectionStrings.MigrationsHistoryTable, DbContext.Schema)
				.CommandTimeout(ModuleConnectionStrings.DesignTimeCommandTimeoutSeconds))
			.UseSnakeCaseNamingConvention();

		return new DbContext(optionsBuilder.Options, new SystemDateTimeProvider());
	}
}
