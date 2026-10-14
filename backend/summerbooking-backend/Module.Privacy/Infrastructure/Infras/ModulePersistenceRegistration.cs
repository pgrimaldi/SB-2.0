using BuildingBlocks.Infrastructure.Persistence;
using BuildingBlocks.Infrastructure.Retention;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Module.Privacy.Domain.Enums;
using Npgsql;
using Shared.Infrastructure;

namespace Module.Privacy.Infrastructure.Infras;

/// <summary>
/// Registrazione DI della persistenza del modulo Privacy, richiamata da Module.AddPrivacyModule().
/// Stessa costruzione (data source + enum mapping) della DesignTimeDbContextFactory, ma con
/// AddDbContext (Scoped) per l'uso a runtime dentro Boot.
/// </summary>
public static class ModulePersistenceRegistration
{
	public static IServiceCollection AddModulePrivacyPersistence(this IServiceCollection services, IConfiguration configuration)
	{
		services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

		var dataSourceBuilder = new NpgsqlDataSourceBuilder(ModuleConnectionStrings.Resolve(configuration));

		// Mappatura degli enum nativi PostgreSQL del modulo, in coppia con le
		// modelBuilder.HasPostgresEnum<TEnum> di DbContext.OnModelCreating (stessa lista di
		// DesignTimeDbContextFactory).
		dataSourceBuilder.MapEnum<RequestKind>("privacy.request_kind");
		dataSourceBuilder.MapEnum<SubjectKind>("privacy.subject_kind");
		dataSourceBuilder.MapEnum<RequestStatus>("privacy.request_status");
		dataSourceBuilder.MapEnum<RequesterKind>("privacy.requester_kind");
		dataSourceBuilder.MapEnum<ExportArtifactOrigin>("privacy.export_artifact_origin");
		dataSourceBuilder.MapEnum<ModuleCode>("privacy.module_code");
		dataSourceBuilder.MapEnum<ModuleOutcomeResult>("privacy.module_outcome_result");
		dataSourceBuilder.MapEnum<RetentionDurationUnit>("privacy.retention_duration_unit");
		dataSourceBuilder.MapEnum<RetentionAction>("privacy.retention_action");
		dataSourceBuilder.MapEnum<RetentionBasisSource>("privacy.retention_basis_source");
		dataSourceBuilder.MapEnum<ActorKind>("privacy.actor_kind");
		dataSourceBuilder.MapEnum<AuditAction>("privacy.audit_action");
		dataSourceBuilder.MapEnum<RetentionReferenceEvent>("privacy.retention_reference_event");
		dataSourceBuilder.MapEnum<RetentionStorageDateSource>("privacy.retention_storage_date_source");
		dataSourceBuilder.MapEnum<RetentionRunStatus>("privacy.retention_run_status");
		dataSourceBuilder.MapEnum<ArchiveObjectKind>("privacy.archive_object_kind");

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

		// Il job che svuota la coda delle cancellazioni su storage. Registrato SOLO qui: la coda
		// privacy_file_deletions e' una, e Privacy e' il modulo che la possiede.
		var deletion = new StorageDeletionOptions();
		configuration.GetSection("Retention:Deletion").Bind(deletion);
		services.AddFileDeletionQueue<DbContext>(deletion);

		return services;
	}
}
