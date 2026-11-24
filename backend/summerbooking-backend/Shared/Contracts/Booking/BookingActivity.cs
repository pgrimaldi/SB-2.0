using Shared.Contracts.Events;

namespace Shared.Contracts.Booking;

/// <summary>Cosa è successo, per la campanella del gestionale.</summary>
public static class BookingActivityKinds
{
	public const string ReservationCreated = "reservation_created";
	public const string OnlineReservationCreated = "online_reservation_created";
	public const string OnlineAwaitingConfirmation = "online_awaiting_confirmation";
	public const string PaymentReceived = "payment_received";
	public const string ReservationCancelledByCustomer = "reservation_cancelled_by_customer";
	public const string InventoryLow = "inventory_low";
}

/// <summary>
/// Attività di Booking da mostrare nella campanella del gestionale: nuove prenotazioni dal gestionale e online,
/// richieste online da confermare, pagamenti ricevuti, annullamenti del cliente, magazzino in esaurimento. Le trasforma in
/// notifiche Notification (quando avrà le sue API).
/// </summary>
/// <param name="Kind">Uno dei codici di <see cref="BookingActivityKinds"/>.</param>
public sealed record BookingActivity(
	Guid EventId, DateTime OccurredAt, Guid PropertyId, string Kind, Guid? ReservationId, string? BookingNumber, Guid? ActorUserId,
	IReadOnlyDictionary<string, object?>? Details) : IIntegrationEvent;
