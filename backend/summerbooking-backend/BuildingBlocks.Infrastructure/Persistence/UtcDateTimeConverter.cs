using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// Converter unico per ogni proprieta DateTime mappata su timestamptz: forza Kind = Utc in
/// lettura (Npgsql restituisce Unspecified) cosi il valore non viene mai confuso con l'ora
/// locale. Registrato una volta per modulo in ModuleDbContextConventions.ConfigureConventions,
/// mai ripetuto proprieta per proprieta.
/// </summary>
public sealed class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
{
	public UtcDateTimeConverter() : base(
		v => DateTime.SpecifyKind(v, DateTimeKind.Utc),
		v => DateTime.SpecifyKind(v, DateTimeKind.Utc))
	{
	}
}
