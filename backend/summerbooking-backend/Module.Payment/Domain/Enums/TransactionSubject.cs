namespace Module.Payment.Domain.Enums;

/// <summary>Cosa si sta pagando con una transazione online (<see cref="Entities.Charge"/>).</summary>
public enum TransactionSubject
{
	Reservation = 0,
	ServiceReservation = 1
}
