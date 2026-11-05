using Module.Payment.Domain.Enums;

namespace Module.Payment.Domain.Entities;

/// <summary>
/// Registro degli eventi webhook ricevuti dai gateway di pagamento, con la chiave di idempotenza
/// (provider, external_event_id) che impedisce di elaborare due volte lo stesso evento.
/// </summary>
/// <remarks>
/// Tabella generica per provider/evento. L'unicita' e' a schema, quindi
/// l'inserimento puo' essere un INSERT ... ON CONFLICT DO NOTHING: la corsa la arbitra PostgreSQL.
/// Eventi nuovi (rimborsi, dispute, aggiornamenti account Connect) non richiedono una tabella
/// dedicata ne' un cambio di schema.
/// </remarks>
public sealed class WebhookEvent
{
	public long Id { get; private set; }
	public WebhookProvider Provider { get; private set; }
	public string ExternalEventId { get; private set; } = null!;
	public string EventType { get; private set; } = null!;
	public Guid? PropertyId { get; private set; }
	public string Payload { get; private set; } = null!;
	public WebhookProcessingStatus ProcessingStatus { get; private set; }
	public short AttemptCount { get; private set; }
	public string? FailureReason { get; private set; }
	public DateTime ReceivedAt { get; private set; }
	public DateTime? ProcessedAt { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }

	private WebhookEvent() { }   // unico costruttore, per il materializzatore di EF Core
}
