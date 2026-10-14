using Shared.Contracts.Events;

namespace Boot.Web;

/// <summary>
/// Bus in-process: consegna l'evento ai gestori registrati nello stesso scope. Un gestore che fallisce
/// non blocca l'azione già salvata: si logga. ⚠️ Senza outbox un evento si perde se il processo cade
/// fra il salvataggio e la consegna (PROMEMORIA-85-7).
/// </summary>
public sealed class InProcessEventPublisher : IIntegrationEventPublisher
{
	private readonly IServiceProvider _services;
	private readonly ILogger<InProcessEventPublisher> _logger;

	public InProcessEventPublisher(IServiceProvider services, ILogger<InProcessEventPublisher> logger)
	{
		_services = services;
		_logger = logger;
	}

	public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct = default) where TEvent : IIntegrationEvent
	{
		var handlers = _services.GetServices<IIntegrationEventHandler<TEvent>>().ToList();
		if (handlers.Count == 0)
		{
			_logger.LogDebug("Evento {Event} {EventId} senza gestori", typeof(TEvent).Name, @event.EventId);
			return;
		}

		foreach (var handler in handlers)
		{
			try
			{
				await handler.HandleAsync(@event, ct);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Gestore {Handler} fallito sull'evento {Event} {EventId}", handler.GetType().Name, typeof(TEvent).Name, @event.EventId);
			}
		}
	}
}
