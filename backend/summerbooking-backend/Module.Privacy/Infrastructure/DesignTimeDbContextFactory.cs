using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Module.Privacy.Domain.Enums;
using Npgsql;
using Shared.Infrastructure;

namespace Module.Privacy.Infrastructure;

/// <summary>
/// Factory usata da "dotnet ef" per creare il DbContext a design-time (migration add/update/...)
/// senza passare dal progetto Boot: il modulo Privacy e auto-sufficiente per le migration.
/// La connection string arriva da Boot/appsettings.json/appsettings.{Environment}.json,
/// sovrascrivibile con la variabile d'ambiente ConnectionStrings__SummerBooking (BuildingBlocks.Infrastructure.Persistence.ModuleConnectionStrings).
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<DbContext>
{
	public DbContext CreateDbContext(string[] args)
	{
		var dataSourceBuilder = new NpgsqlDataSourceBuilder(ModuleConnectionStrings.ResolveForDesignTime());

		// Mappatura degli enum nativi PostgreSQL del modulo, in coppia con le
		// modelBuilder.HasPostgresEnum<TEnum> di DbContext.OnModelCreating.
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

		var dataSource = dataSourceBuilder.Build();

		var optionsBuilder = new DbContextOptionsBuilder<DbContext>()
			.UseNpgsql(dataSource, npgsql => npgsql
				.MigrationsHistoryTable(ModuleConnectionStrings.MigrationsHistoryTable, DbContext.Schema)
				.CommandTimeout(ModuleConnectionStrings.DesignTimeCommandTimeoutSeconds))
			.UseSnakeCaseNamingConvention();

		return new DbContext(optionsBuilder.Options, new SystemDateTimeProvider());
	}
}
