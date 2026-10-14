using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Shared.Contracts;
using Shared.Infrastructure;

namespace BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// Valorizza CreatedAt/UpdatedAt delle entita IAuditable prima del salvataggio, leggendo
/// l'ora da IDateTimeProvider (mai DateTime.UtcNow diretto qui). Scrive tramite il
/// ChangeTracker (entry.Property(...).CurrentValue), non un setter pubblico: le entita
/// espongono solo getter su questi due campi (vedi Shared.Contracts.IAuditable).
/// </summary>
public sealed class AuditInterceptor : SaveChangesInterceptor
{
	private readonly IDateTimeProvider _dateTimeProvider;

	public AuditInterceptor(IDateTimeProvider dateTimeProvider)
	{
		_dateTimeProvider = dateTimeProvider;
	}

	public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
	{
		Apply(eventData.Context);
		return base.SavingChanges(eventData, result);
	}

	public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
		DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
	{
		Apply(eventData.Context);
		return base.SavingChangesAsync(eventData, result, cancellationToken);
	}

	private void Apply(DbContext? context)
	{
		if (context is null)
			return;

		var now = _dateTimeProvider.UtcNow;

		foreach (var entry in context.ChangeTracker.Entries<IAuditable>())
		{
			if (entry.State == EntityState.Added)
				entry.Property(nameof(IAuditable.CreatedAt)).CurrentValue = now;

			if (entry.State is EntityState.Added or EntityState.Modified)
				entry.Property(nameof(IAuditable.UpdatedAt)).CurrentValue = now;
		}
	}
}
