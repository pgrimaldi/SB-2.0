using Module.Audit.Domain.Enums;
using Npgsql;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;

namespace Module.Audit.Infrastructure.Infras;

/// <summary>
/// Enum nativi del modulo, un solo elenco per runtime e design-time: sul data source e nelle opzioni EF
/// (senza la seconda EF scrive l'enum come intero). In coppia con
/// <c>HasPostgresEnum</c> in <see cref="DbContext"/>.
/// </summary>
internal static class AuditEnumMappings
{
	public static void Apply(NpgsqlDataSourceBuilder builder)
	{
		builder.MapEnum<EntryCategory>("audit.entry_category");
		builder.MapEnum<SourceModule>("audit.source_module");
		builder.MapEnum<EntryOutcome>("audit.entry_outcome");
		builder.MapEnum<ActorKind>("audit.actor_kind");
	}

	public static NpgsqlDbContextOptionsBuilder Apply(NpgsqlDbContextOptionsBuilder options)
	{
		const string schema = DbContext.Schema;
		options.MapEnum<EntryCategory>("entry_category", schema);
		options.MapEnum<SourceModule>("source_module", schema);
		options.MapEnum<EntryOutcome>("entry_outcome", schema);
		options.MapEnum<ActorKind>("actor_kind", schema);
		return options;
	}
}
