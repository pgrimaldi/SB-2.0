namespace Module.Payment.Domain.Enums;

/// <summary>
/// Gateway che invia il webhook (<see cref="Entities.WebhookEvent"/>). Oggi solo Stripe: il valore
/// esiste comunque come enum perche' la chiave di idempotenza e' (provider, external_event_id) e un
/// secondo gateway non deve poter collidere con gli id evento del primo.
/// </summary>
public enum WebhookProvider
{
	Stripe = 0
}
