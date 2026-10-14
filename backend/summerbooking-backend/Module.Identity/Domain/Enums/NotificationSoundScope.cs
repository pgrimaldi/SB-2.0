namespace Module.Identity.Domain.Enums;

/// <summary>
/// Ambito per cui l'utente ha attivo il segnale acustico di notifica
/// (<see cref="Entities.Account.EnabledNotificationSounds"/>).
/// </summary>
/// <remarks>
/// Sostituisce quattro colonne booleane parallele sullo stesso argomento.
/// Lo schema usava gia' questo pattern altrove
/// (enabled_payment_methods): qui non era stato applicato, ed era l'incoerenza di disegno da
/// chiudere. Aggiungere un quinto ambito domani sara' un valore di enum, non una quinta colonna.
/// </remarks>
public enum NotificationSoundScope
{
	/// <summary>Prenotazione ombrellone creata dal gestionale.</summary>
	ManagementBooking = 0,

	/// <summary>Prenotazione ombrellone arrivata dal canale online.</summary>
	OnlineBooking = 1,

	/// <summary>Prenotazione di servizi creata dal gestionale.</summary>
	ManagementServiceBooking = 2,

	/// <summary>Prenotazione di servizi arrivata dal canale online.</summary>
	OnlineServiceBooking = 3
}
