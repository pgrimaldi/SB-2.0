using Module.Booking.Application.Abstractions;
using Module.Booking.Domain.Enums;
using Shared.Contracts.Events;
using Shared.Contracts.Payment;
using Shared.Utils;

namespace Module.Booking.Application.Services;

/// <summary>
/// I movimenti di una prenotazione sono cambiati in Payment: pagato, acconto, residuo e metodo prevalente si aggiornano
/// sulla testata (anche nel cestino, per i rimborsi dell'eliminazione).
/// </summary>
public sealed class ReservationPaymentsChangedHandler : IIntegrationEventHandler<ReservationPaymentsChanged>
{
	private readonly IReservationRepository _reservations;
	private readonly BookingAudit _audit;

	public ReservationPaymentsChangedHandler(IReservationRepository reservations, BookingAudit audit)
	{
		_reservations = reservations;
		_audit = audit;
	}

	public async Task HandleAsync(ReservationPaymentsChanged @event, CancellationToken ct = default)
	{
		var reservation = await _reservations.GetAsync(@event.PropertyId, @event.ReservationId, includeTrashed: true, ct);
		if (reservation is null)
			return;
		PaymentMode? mode = Codes.TryParse<PaymentMode>(@event.PrevalentMode, out var m) ? m : null;
		var received = @event.Paid - reservation.Paid;
		reservation.ApplyPayments(@event.Paid, @event.Deposit, mode);
		await _reservations.SaveChangesAsync(ct);
		if (received > 0)
			await _audit.ActivityAsync(Shared.Contracts.Booking.BookingActivityKinds.PaymentReceived, reservation,
				new Dictionary<string, object?> { ["amount"] = received }, ct);
	}
}
