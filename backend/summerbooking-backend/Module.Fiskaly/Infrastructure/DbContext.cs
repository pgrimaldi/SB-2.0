using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Module.Fiskaly.Domain.Entities;
using Module.Fiskaly.Domain.Enums;
using Shared.Infrastructure;

namespace Module.Fiskaly.Infrastructure;

/// <summary>
/// Contesto di persistenza del modulo Fiskaly, schema PostgreSQL "fiskaly".
/// Nome della classe = solo "DbContext": il namespace la disambigua dagli altri moduli.
/// Mai referenziato fuori da Module.Fiskaly.Infrastructure.
/// </summary>
public sealed class DbContext : Microsoft.EntityFrameworkCore.DbContext
{
	public const string Schema = "fiskaly";

	private readonly IDateTimeProvider _dateTimeProvider;

	public DbContext(DbContextOptions<DbContext> options, IDateTimeProvider dateTimeProvider) : base(options)
	{
		_dateTimeProvider = dateTimeProvider;
	}

	// DbSet<TEntity> di ogni aggregato/entita del modulo — popolati
	// dall'implementazione del modulo, uno per tabella con IEntityTypeConfiguration corrispondente.
	public DbSet<FiskalyAccount> FiskalyAccounts => Set<FiskalyAccount>();
	public DbSet<CashRegister> CashRegisters => Set<CashRegister>();
	public DbSet<MediaAsset> MediaAssets => Set<MediaAsset>();
	public DbSet<VatCode> VatCodes => Set<VatCode>();
	public DbSet<Receipt> Receipts => Set<Receipt>();
	public DbSet<ReceiptLine> ReceiptLines => Set<ReceiptLine>();
	public DbSet<ReceiptPayment> ReceiptPayments => Set<ReceiptPayment>();
	public DbSet<ReceiptLog> ReceiptLogs => Set<ReceiptLog>();
	public DbSet<ReceiptSequence> ReceiptSequences => Set<ReceiptSequence>();

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
		modelBuilder.HasPostgresEnum<FiscalSetupLevel>(Schema, "fiscal_setup_level");
		modelBuilder.HasPostgresEnum<FiskalyOrgState>(Schema, "org_state");
		modelBuilder.HasPostgresEnum<FiskalyEntityState>(Schema, "entity_state");
		modelBuilder.HasPostgresEnum<FiskalyEntityMode>(Schema, "entity_mode");
		modelBuilder.HasPostgresEnum<FiskalySystemType>(Schema, "system_type");
		modelBuilder.HasPostgresEnum<FiskalyMediaOrigin>(Schema, "media_origin");
		modelBuilder.HasPostgresEnum<FiskalyMediaKind>(Schema, "media_kind");
		modelBuilder.HasPostgresEnum<ReceiptKind>(Schema, "receipt_kind");
		modelBuilder.HasPostgresEnum<ReceiptSource>(Schema, "receipt_source");
		modelBuilder.HasPostgresEnum<ReceiptRecordState>(Schema, "receipt_record_state");
		modelBuilder.HasPostgresEnum<RecordMode>(Schema, "record_mode");
		modelBuilder.HasPostgresEnum<FiskalyPaymentMode>(Schema, "payment_mode");
		modelBuilder.HasPostgresEnum<LanguageCode>(Schema, "language_code");
		modelBuilder.HasPostgresEnum<LineItemType>(Schema, "line_item_type");
		modelBuilder.HasPostgresEnum<FiskalyEntryType>(Schema, "entry_type");
		modelBuilder.HasPostgresEnum<FiskalyEntryCategory>(Schema, "entry_category");
		modelBuilder.HasPostgresEnum<FiskalyVatKind>(Schema, "vat_kind");
		modelBuilder.HasPostgresEnum<ReceiptLogStep>(Schema, "receipt_log_step");
		modelBuilder.HasPostgresEnum<LogResult>(Schema, "log_result");
		modelBuilder.HasPostgresEnum<FiskalyRecordState>(Schema, "fiskaly_record_state");
		modelBuilder.HasPostgresEnum<FiskalyPaymentType>(Schema, "payment_type");
		modelBuilder.HasPostgresEnum<CardKind>(Schema, "card_kind");
		modelBuilder.HasPostgresEnum<VoucherKind>(Schema, "voucher_kind");
		modelBuilder.HasPostgresEnum<OutstandingConcept>(Schema, "outstanding_concept");

		modelBuilder.ApplyConfigurationsFromAssembly(typeof(DbContext).Assembly);

		ModuleDbContextConventions.ApplySoftDeleteQueryFilter(modelBuilder);
	}
}
