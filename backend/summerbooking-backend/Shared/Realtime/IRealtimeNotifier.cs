namespace Shared.Realtime;

/// <summary>
/// Avvisi leggeri in tempo reale ai gestionali collegati a uno stabilimento («la prenotazione X è cambiata»): il client
/// ricarica solo quello che gli serve. Lo implementa l'host (SignalR); senza host i moduli non avvisano nessuno.
/// </summary>
public interface IRealtimeNotifier
{
	/// <param name="Topic">reservations, holds…</param>
	Task NotifyAsync(Guid propertyId, string topic, IReadOnlyCollection<Guid> ids, CancellationToken ct = default);
}
