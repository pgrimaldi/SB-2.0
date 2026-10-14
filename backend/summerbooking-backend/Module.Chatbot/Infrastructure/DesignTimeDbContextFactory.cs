using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Module.Chatbot.Domain.Enums;
using Npgsql;
using Shared.Infrastructure;

namespace Module.Chatbot.Infrastructure;

/// <summary>
/// Factory usata da "dotnet ef" per creare il DbContext a design-time (migration add/update/...)
/// senza passare dal progetto Boot: il modulo Chatbot e auto-sufficiente per le migration.
/// La connection string arriva da Boot/appsettings.json/appsettings.{Environment}.json,
/// sovrascrivibile con la variabile d'ambiente ConnectionStrings__SummerBooking (BuildingBlocks.Infrastructure.Persistence.ModuleConnectionStrings).
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<DbContext>
{
	public DbContext CreateDbContext(string[] args)
	{
		var dataSourceBuilder = new NpgsqlDataSourceBuilder(ModuleConnectionStrings.ResolveForDesignTime());

		dataSourceBuilder.MapEnum<ChannelKind>("chatbot.channel_kind");
		dataSourceBuilder.MapEnum<TurnStatus>("chatbot.turn_status");

		var dataSource = dataSourceBuilder.Build();

		var optionsBuilder = new DbContextOptionsBuilder<DbContext>()
			.UseNpgsql(dataSource, npgsql => npgsql
				.MigrationsHistoryTable(ModuleConnectionStrings.MigrationsHistoryTable, DbContext.Schema)
				.CommandTimeout(ModuleConnectionStrings.DesignTimeCommandTimeoutSeconds))
			.UseSnakeCaseNamingConvention();

		return new DbContext(optionsBuilder.Options, new SystemDateTimeProvider());
	}
}
