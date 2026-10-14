using Microsoft.EntityFrameworkCore;
using Shared.Contracts;

namespace BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// Plumbing comune richiamata dal DbContext di ogni modulo, cosi la stessa meccanica non va
/// ripetuta in ogni modulo. Non contiene nulla di specifico a un modulo (nomi tabella, enum, FK): quello
/// resta nel DbContext/Configurations del modulo.
/// </summary>
public static class ModuleDbContextConventions
{
	/// <summary>Registra il converter UTC unico per ogni proprieta DateTime del modulo. Da chiamare in ConfigureConventions.</summary>
	public static void ConfigureUtcDateTimes(ModelConfigurationBuilder configurationBuilder)
	{
		configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
	}

	/// <summary>
	/// Applica il Global Query Filter (WHERE deleted_at IS NULL) a ogni entita ISoftDeletable
	/// del modulo, senza scrivere un filtro per entita. Da chiamare in fondo a OnModelCreating,
	/// dopo ApplyConfigurationsFromAssembly.
	/// </summary>
	public static void ApplySoftDeleteQueryFilter(ModelBuilder modelBuilder)
	{
		foreach (var entityType in modelBuilder.Model.GetEntityTypes())
		{
			if (!typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
				continue;

			var parameter = System.Linq.Expressions.Expression.Parameter(entityType.ClrType, "e");
			var property = System.Linq.Expressions.Expression.Property(parameter, nameof(ISoftDeletable.DeletedAt));
			var condition = System.Linq.Expressions.Expression.Equal(property, System.Linq.Expressions.Expression.Constant(null, typeof(DateTime?)));
			var lambda = System.Linq.Expressions.Expression.Lambda(condition, parameter);

			modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
		}
	}
}
