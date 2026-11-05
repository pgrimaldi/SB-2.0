namespace Module.Payment.Domain.Enums;

/// <summary>Esito dell'elaborazione di un evento webhook (<see cref="Entities.WebhookEvent"/>).</summary>
public enum WebhookProcessingStatus
{
	/// <summary>Ricevuto e registrato, non ancora elaborato.</summary>
	Received = 0,

	/// <summary>Elaborato con successo: gli effetti sul dominio sono stati applicati.</summary>
	Processed = 1,

	/// <summary>
	/// Riconosciuto ma volutamente non gestito: resta registrato, cosi' "non lo gestiamo" e
	/// "lo abbiamo perso" sono due stati distinguibili.
	/// </summary>
	Ignored = 2,

	/// <summary>Elaborazione fallita: da riprovare o da esaminare a mano.</summary>
	Failed = 3
}
