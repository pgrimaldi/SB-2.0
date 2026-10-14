namespace Module.ZTest.Domain.Events;

/// <summary>
/// Marker comune per gli eventi di dominio del modulo.
/// </summary>
public interface IDomainEvent
{
	DateTime OccurredOnUtc { get; }
}

/// <summary>
/// Evento di dominio sollevato quando viene creata una nuova prenotazione.
/// Puo essere pubblicato dopo il commit per notificare altri moduli.
/// </summary>
public sealed record ReservationCreatedEvent(Guid ReservationId, Guid GuestId) : IDomainEvent
{
	public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
