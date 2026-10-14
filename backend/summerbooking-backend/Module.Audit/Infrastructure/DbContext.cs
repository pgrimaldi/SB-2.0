using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Module.Audit.Domain.Entities;
using Module.Audit.Domain.Enums;
using Shared.Infrastructure;

namespace Module.Audit.Infrastructure;

/// <summary>
/// Contesto di persistenza del modulo Audit, schema PostgreSQL "audit".
/// Nome della classe = solo "DbContext": il namespace la disambigua dagli altri moduli.
/// Mai referenziato fuori da Module.Audit.Infrastructure.
/// </summary>
public sealed class DbContext : Microsoft.EntityFrameworkCore.DbContext
{
	public const string Schema = "audit";

	private readonly IDateTimeProvider _dateTimeProvider;

	public DbContext(DbContextOptions<DbContext> options, IDateTimeProvider dateTimeProvider) : base(options)
	{
		_dateTimeProvider = dateTimeProvider;
	}

	public DbSet<AuditTrailEntry> TrailEntries => Set<AuditTrailEntry>();

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

		modelBuilder.HasPostgresEnum<EntryCategory>(Schema, "entry_category");
		modelBuilder.HasPostgresEnum<SourceModule>(Schema, "source_module");
		modelBuilder.HasPostgresEnum<EntryOutcome>(Schema, "entry_outcome");
		modelBuilder.HasPostgresEnum<ActorKind>(Schema, "actor_kind");

		modelBuilder.ApplyConfigurationsFromAssembly(typeof(DbContext).Assembly);

		ModuleDbContextConventions.ApplySoftDeleteQueryFilter(modelBuilder);
	}
}
