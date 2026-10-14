using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Module.Chatbot.Domain.Entities;
using Module.Chatbot.Domain.Enums;
using Shared.Infrastructure;

namespace Module.Chatbot.Infrastructure;

/// <summary>
/// Contesto di persistenza del modulo Chatbot, schema PostgreSQL "chatbot".
/// Nome della classe = solo "DbContext": il namespace la disambigua dagli altri moduli.
/// Mai referenziato fuori da Module.Chatbot.Infrastructure.
/// </summary>
public sealed class DbContext : Microsoft.EntityFrameworkCore.DbContext
{
	public const string Schema = "chatbot";

	private readonly IDateTimeProvider _dateTimeProvider;

	public DbContext(DbContextOptions<DbContext> options, IDateTimeProvider dateTimeProvider) : base(options)
	{
		_dateTimeProvider = dateTimeProvider;
	}

	public DbSet<LlmProvider> LlmProviders => Set<LlmProvider>();
	public DbSet<ChatbotManagerConfig> ManagerConfigs => Set<ChatbotManagerConfig>();
	public DbSet<ChatbotConversationTurn> ConversationTurns => Set<ChatbotConversationTurn>();

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

		modelBuilder.HasPostgresEnum<ChannelKind>(Schema, "channel_kind");
		modelBuilder.HasPostgresEnum<TurnStatus>(Schema, "turn_status");

		modelBuilder.ApplyConfigurationsFromAssembly(typeof(DbContext).Assembly);

		ModuleDbContextConventions.ApplySoftDeleteQueryFilter(modelBuilder);
	}
}
