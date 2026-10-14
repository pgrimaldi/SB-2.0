using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Module.FiscalPrint.Domain.Entities;
using Module.FiscalPrint.Domain.Enums;
using Shared.Infrastructure;

namespace Module.FiscalPrint.Infrastructure;

/// <summary>
/// Contesto di persistenza del modulo FiscalPrint, schema PostgreSQL "fiscalprint".
/// Nome della classe = solo "DbContext": il namespace la disambigua dagli altri moduli.
/// Mai referenziato fuori da Module.FiscalPrint.Infrastructure.
/// </summary>
public sealed class DbContext : Microsoft.EntityFrameworkCore.DbContext
{
	public const string Schema = "fiscalprint";

	private readonly IDateTimeProvider _dateTimeProvider;

	public DbContext(DbContextOptions<DbContext> options, IDateTimeProvider dateTimeProvider) : base(options)
	{
		_dateTimeProvider = dateTimeProvider;
	}

	// DbSet<TEntity> di ogni aggregato/entita del modulo — popolati
	// dall'implementazione del modulo, uno per tabella con IEntityTypeConfiguration corrispondente.
	public DbSet<PrinterConfig> PrinterConfigs => Set<PrinterConfig>();
	public DbSet<PrinterDepartment> PrinterDepartments => Set<PrinterDepartment>();
	public DbSet<FiscalPrintReceipt> Receipts => Set<FiscalPrintReceipt>();
	public DbSet<FiscalPrintNumberSequence> SerialNumbers => Set<FiscalPrintNumberSequence>();

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
		modelBuilder.HasPostgresEnum<PrinterModel>(Schema, "printer_model");
		modelBuilder.HasPostgresEnum<PrinterMode>(Schema, "printer_mode");
		modelBuilder.HasPostgresEnum<ConnectionKind>(Schema, "connection_kind");

		modelBuilder.ApplyConfigurationsFromAssembly(typeof(DbContext).Assembly);

		ModuleDbContextConventions.ApplySoftDeleteQueryFilter(modelBuilder);
	}
}
