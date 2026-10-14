using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Module.Privacy.Domain.Entities;
using Module.Privacy.Domain.Enums;
using Shared.Infrastructure;

namespace Module.Privacy.Infrastructure;

/// <summary>
/// Contesto di persistenza del modulo Privacy, schema PostgreSQL "privacy".
/// Nome della classe = solo "DbContext": il namespace la disambigua dagli altri moduli.
/// Mai referenziato fuori da Module.Privacy.Infrastructure.
/// </summary>
public sealed class DbContext : Microsoft.EntityFrameworkCore.DbContext
{
	public const string Schema = "privacy";

	private readonly IDateTimeProvider _dateTimeProvider;

	public DbContext(DbContextOptions<DbContext> options, IDateTimeProvider dateTimeProvider) : base(options)
	{
		_dateTimeProvider = dateTimeProvider;
	}

	// DbSet<TEntity> di ogni aggregato/entita del modulo — popolati
	// dall'implementazione del modulo, uno per tabella con IEntityTypeConfiguration corrispondente.
	public DbSet<DataSubjectRequest> DataSubjectRequests => Set<DataSubjectRequest>();
	public DbSet<ModuleOutcome> ModuleOutcomes => Set<ModuleOutcome>();
	public DbSet<LegalHold> LegalHolds => Set<LegalHold>();
	public DbSet<RetentionPolicy> RetentionPolicies => Set<RetentionPolicy>();
	public DbSet<ComplianceAuditEntry> ComplianceAuditEntries => Set<ComplianceAuditEntry>();
	public DbSet<FileDeletion> FileDeletions => Set<FileDeletion>();
	public DbSet<RetentionRun> RetentionRuns => Set<RetentionRun>();
	public DbSet<ArchiveManifest> ArchiveManifests => Set<ArchiveManifest>();

	protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
	{
		optionsBuilder.AddInterceptors(new AuditInterceptor(_dateTimeProvider), new SoftDeleteInterceptor(_dateTimeProvider));
	}

	protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
	{
		ModuleDbContextConventions.ConfigureUtcDateTimes(configurationBuilder);
	}

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.HasDefaultSchema(Schema);

		// Enum nativi PostgreSQL del modulo (modelBuilder.HasPostgresEnum<TEnum>(Schema, "..."))
		// vanno qui.
		modelBuilder.HasPostgresEnum<RequestKind>(Schema, "request_kind");
		modelBuilder.HasPostgresEnum<SubjectKind>(Schema, "subject_kind");
		modelBuilder.HasPostgresEnum<RequestStatus>(Schema, "request_status");
		modelBuilder.HasPostgresEnum<RequesterKind>(Schema, "requester_kind");
		modelBuilder.HasPostgresEnum<ExportArtifactOrigin>(Schema, "export_artifact_origin");
		modelBuilder.HasPostgresEnum<ModuleCode>(Schema, "module_code");
		modelBuilder.HasPostgresEnum<ModuleOutcomeResult>(Schema, "module_outcome_result");
		modelBuilder.HasPostgresEnum<RetentionDurationUnit>(Schema, "retention_duration_unit");
		modelBuilder.HasPostgresEnum<RetentionAction>(Schema, "retention_action");
		modelBuilder.HasPostgresEnum<RetentionBasisSource>(Schema, "retention_basis_source");
		modelBuilder.HasPostgresEnum<ActorKind>(Schema, "actor_kind");
		modelBuilder.HasPostgresEnum<AuditAction>(Schema, "audit_action");
		modelBuilder.HasPostgresEnum<RetentionReferenceEvent>(Schema, "retention_reference_event");
		modelBuilder.HasPostgresEnum<RetentionStorageDateSource>(Schema, "retention_storage_date_source");
		modelBuilder.HasPostgresEnum<RetentionRunStatus>(Schema, "retention_run_status");
		modelBuilder.HasPostgresEnum<ArchiveObjectKind>(Schema, "archive_object_kind");

		modelBuilder.ApplyConfigurationsFromAssembly(typeof(DbContext).Assembly);

		ModuleDbContextConventions.ApplySoftDeleteQueryFilter(modelBuilder);
	}
}
