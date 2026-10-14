namespace Shared.Contracts.Events;

/// <summary>
/// Pubblica un evento ai moduli interessati (in-process oggi, broker domani).
/// ⚠️ Nessun outbox ancora: l'evento parte dopo il salvataggio, non nella stessa transazione
/// (PROMEMORIA-85-7).
/// </summary>
public interface IIntegrationEventPublisher
{
	Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct = default) where TEvent : IIntegrationEvent;
}

/// <summary>Gestore di un evento, registrato in DI dal modulo che lo consuma.</summary>
public interface IIntegrationEventHandler<in TEvent> where TEvent : IIntegrationEvent
{
	Task HandleAsync(TEvent @event, CancellationToken ct = default);
}
