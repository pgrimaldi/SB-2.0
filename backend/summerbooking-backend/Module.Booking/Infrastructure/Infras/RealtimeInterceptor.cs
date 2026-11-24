using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Module.Booking.Domain.Entities;
using Shared.Realtime;

namespace Module.Booking.Infrastructure.Infras;

/// <summary>
/// Dopo ogni salvataggio riuscito avvisa i gestionali dello stabilimento delle prenotazioni e delle riserve cambiate. Un
/// avviso perso non fa danni: al rientro il client ricarica la mappa.
/// </summary>
internal sealed class RealtimeInterceptor : SaveChangesInterceptor
{
	private readonly IRealtimeNotifier _notifier;
	private readonly List<(Guid Property, string Topic, Guid Id)> _pending = [];

	public RealtimeInterceptor(IRealtimeNotifier notifier) => _notifier = notifier;

	public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
		DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
	{
		Collect(eventData.Context);
		return base.SavingChangesAsync(eventData, result, cancellationToken);
	}

	public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
	{
		Collect(eventData.Context);
		return base.SavingChanges(eventData, result);
	}

	public override async ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
	{
		await FlushAsync(cancellationToken);
		return await base.SavedChangesAsync(eventData, result, cancellationToken);
	}

	public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
	{
		FlushAsync(CancellationToken.None).GetAwaiter().GetResult();
		return base.SavedChanges(eventData, result);
	}

	public override void SaveChangesFailed(DbContextErrorEventData eventData) => _pending.Clear();

	private void Collect(Microsoft.EntityFrameworkCore.DbContext? context)
	{
		if (context is null)
			return;
		foreach (var entry in context.ChangeTracker.Entries().Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
			switch (entry.Entity)
			{
				case Reservation r:
					_pending.Add((r.PropertyId, "reservations", r.PublicId));
					break;
				case SpotHold h:
					_pending.Add((h.PropertyId, "holds", h.PublicId));
					break;
			}
	}

	private async Task FlushAsync(CancellationToken ct)
	{
		var batch = _pending.Distinct().ToList();
		_pending.Clear();
		foreach (var group in batch.GroupBy(p => (p.Property, p.Topic)))
		{
			try
			{
				await _notifier.NotifyAsync(group.Key.Property, group.Key.Topic, group.Select(p => p.Id).ToList(), ct);
			}
			catch (Exception)
			{
				// l'avviso è un'ottimizzazione: un errore di rete non deve far fallire il salvataggio già fatto
			}
		}
	}
}
