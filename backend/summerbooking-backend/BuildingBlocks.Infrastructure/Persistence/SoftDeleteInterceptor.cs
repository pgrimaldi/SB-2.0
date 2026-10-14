using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Shared.Contracts;
using Shared.Infrastructure;

namespace BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// Trasforma una DELETE fisica su un'entita ISoftDeletable in un UPDATE che valorizza
/// DeletedAt, cosi le tabelle di dominio non perdono mai una riga fisicamente. Un repository che vuole cancellare chiama
/// comunque DbSet.Remove(entity): e questo interceptor a riscrivere lo stato.
/// </summary>
public sealed class SoftDeleteInterceptor : SaveChangesInterceptor
{
	private readonly IDateTimeProvider _dateTimeProvider;

	public SoftDeleteInterceptor(IDateTimeProvider dateTimeProvider)
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

		foreach (var entry in context.ChangeTracker.Entries<ISoftDeletable>())
		{
			if (entry.State != EntityState.Deleted)
				continue;

			entry.State = EntityState.Modified;
			entry.Property(nameof(ISoftDeletable.DeletedAt)).CurrentValue = now;
		}
	}
}
