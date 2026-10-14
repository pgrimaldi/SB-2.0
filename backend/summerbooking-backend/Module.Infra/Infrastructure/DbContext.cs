using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Module.Infra.Domain.Entities;
using Module.Infra.Domain.Enums;
using Shared.Infrastructure;

namespace Module.Infra.Infrastructure;

/// <summary>
/// Contesto di persistenza del modulo Infra, schema PostgreSQL "infra".
/// Nome della classe = solo "DbContext": il namespace la disambigua dagli altri moduli.
/// Mai referenziato fuori da Module.Infra.Infrastructure.
/// </summary>
public sealed class DbContext : Microsoft.EntityFrameworkCore.DbContext
{
	public const string Schema = "infra";

	private readonly IDateTimeProvider _dateTimeProvider;

	public DbContext(DbContextOptions<DbContext> options, IDateTimeProvider dateTimeProvider) : base(options)
	{
		_dateTimeProvider = dateTimeProvider;
	}

	public DbSet<ApiClient> ApiClients => Set<ApiClient>();

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

		modelBuilder.HasPostgresEnum<IntegrationApp>(Schema, "integration_app");

		modelBuilder.ApplyConfigurationsFromAssembly(typeof(DbContext).Assembly);

		ModuleDbContextConventions.ApplySoftDeleteQueryFilter(modelBuilder);
	}
}
