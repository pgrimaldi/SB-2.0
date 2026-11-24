namespace Module.Identity.Domain.Enums;

/// <summary>
/// Ambito per cui l'utente ha attivo il segnale acustico di notifica
/// (<see cref="Entities.Account.EnabledNotificationSounds"/>).
/// </summary>
/// <remarks>
/// Array di enum invece di colonne booleane parallele sullo stesso argomento, come altrove nello
/// schema (enabled_payment_methods): aggiungere un ambito e' un valore di enum, non una colonna.
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
