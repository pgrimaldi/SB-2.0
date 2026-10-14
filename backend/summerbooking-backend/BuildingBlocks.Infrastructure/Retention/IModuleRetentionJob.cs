using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Infrastructure.Retention;

/// <summary>
/// Il job di retention di <b>un</b> modulo. Ogni modulo lo registra sul proprio
/// <c>DbContext</c>; chi orchestra risolve <c>IEnumerable&lt;IModuleRetentionJob&gt;</c> e non deve
/// conoscere nessun <c>DbContext</c>.
/// </summary>
/// <remarks>
/// È l'interfaccia che tiene in piedi la regola di architettura: nessun modulo vede lo schema di un
/// altro, e chi coordina non vede nessuno schema. Aggiungere un modulo alla retention è una riga nella
/// sua registrazione DI, non una modifica a chi orchestra.
/// </remarks>
public interface IModuleRetentionJob
{
	/// <summary>Lo schema PostgreSQL di cui questo job è proprietario.</summary>
	string Schema { get; }

	Task<SchemaRetentionReport> RunAsync(RetentionRunOptions options, CancellationToken ct);
}

/// <summary>
/// Implementazione comune: prende la connessione del <c>DbContext</c> del modulo e la passa al motore.
/// Un modulo la registra con <see cref="RetentionRegistration.AddModuleRetention{TContext}"/>.
/// </summary>
internal sealed class ModuleRetentionJob<TContext>(TContext context, string schema, RetentionEngine engine)
	: IModuleRetentionJob
	where TContext : DbContext
{
	public string Schema => schema;

	public Task<SchemaRetentionReport> RunAsync(RetentionRunOptions options, CancellationToken ct)
	{
		DbConnection connection = context.Database.GetDbConnection();
		return engine.RunAsync(connection, schema, options, ct);
	}
}

/// <summary>Registrazioni DI della retention.</summary>
public static class RetentionRegistration
{
	/// <summary>
	/// Registra il motore e l'archivio. Va chiamata <b>una volta</b> da chi compone l'applicazione.
	/// </summary>
	public static IServiceCollection AddRetention(this IServiceCollection services, ArchiveStoreOptions archive)
	{
		services.AddSingleton(archive);
		services.AddSingleton<IArchiveStore>(sp => new S3ArchiveStore(
			sp.GetRequiredService<ArchiveStoreOptions>(),
			sp.GetRequiredService<ILogger<S3ArchiveStore>>()));
		services.AddSingleton<RetentionEngine>();
		return services;
	}

	/// <summary>
	/// Registra il job di retention di un modulo. Da chiamare dentro la registrazione di persistenza
	/// del modulo, subito dopo <c>AddDbContext</c>.
	/// </summary>
	public static IServiceCollection AddModuleRetention<TContext>(this IServiceCollection services, string schema)
		where TContext : DbContext
	{
		services.AddScoped<IModuleRetentionJob>(sp => new ModuleRetentionJob<TContext>(
			sp.GetRequiredService<TContext>(), schema, sp.GetRequiredService<RetentionEngine>()));
		return services;
	}
}
