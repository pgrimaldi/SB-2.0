namespace Shared.Contracts.Events;

/// <summary>
/// Evento pubblicato da un modulo verso gli altri. Contratto stabile: si aggiungono campi, non si
/// rinominano. <see cref="EventId"/> permette ai consumatori di deduplicare.
/// </summary>
public interface IIntegrationEvent
{
	Guid EventId { get; }
	DateTime OccurredAt { get; }
}
