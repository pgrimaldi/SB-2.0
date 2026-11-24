using Microsoft.Extensions.Logging;
using Module.Booking.Application.Abstractions;
using Module.Booking.Domain.Enums;
using Shared.Contracts.Catalog;
using Shared.Contracts.Events;
using Shared.Contracts.Payment;
using Shared.Infrastructure;
using Shared.Localization;

namespace Module.Booking.Application.Services;

/// <summary>
/// Stabilimento spostato nel cestino o ripristinato dal systemadmin. Nel cestino le prenotazioni non ancora finite si
/// annullano (motivo property_trashed), senza rimborsi né messaggi, e le postazioni si liberano; al ripristino tornano
/// attive solo quelle, anche se nel frattempo le date sono passate. Con l'eliminazione definitiva non si fa nulla qui:
/// partono i tempi di conservazione. I pagamenti online aperti si chiudono in Payment.
/// </summary>
public sealed class PropertyTrashChangedHandler : IIntegrationEventHandler<PropertyTrashChanged>
{
	private readonly IReservationRepository _reservations;
	private readonly IDateTimeProvider _clock;
	private readonly ILogger<PropertyTrashChangedHandler> _logger;
	private readonly IOnlineCardPayments _cards;

	public PropertyTrashChangedHandler(
		IReservationRepository reservations, IDateTimeProvider clock, ILogger<PropertyTrashChangedHandler> logger, IOnlineCardPayments cards)
	{
		_cards = cards;
		_reservations = reservations;
		_clock = clock;
		_logger = logger;
	}

	public async Task HandleAsync(PropertyTrashChanged @event, CancellationToken ct = default)
	{
		var now = _clock.UtcNow;
		if (@event.Change == PropertyTrashChanges.Trashed)
		{
			var today = DateOnly.FromDateTime(ItalianTime.FromUtc(now));
			var active = (await _reservations.ListForPropertyAsync(@event.PropertyId, today, ct)).Where(r => r.Status != ReservationStatus.Cancelled).ToList();
			foreach (var c in await _reservations.ListLineConsumptionsAsync(active.SelectMany(r => r.Lines).Select(l => l.Id).ToList(), ct))
				if (c.State != ConsumptionState.Released)
					c.Release();
			var awaitingPayment = active.Where(r => r.AwaitsPayment).ToList();
			foreach (var reservation in active)
				reservation.CancelForPropertyTrash(now);
			await _reservations.SaveChangesAsync(ct);
			foreach (var reservation in awaitingPayment)
				await _cards.CancelCheckoutAsync(reservation.PropertyId, reservation.PublicId, ct);
			_logger.LogInformation("Stabilimento {Property} nel cestino: {Count} prenotazioni annullate", @event.PropertyId, active.Count);
		}
		else if (@event.Change == PropertyTrashChanges.Restored)
		{
			var restored = (await _reservations.ListForPropertyAsync(@event.PropertyId, null, ct)).Where(r => r.ReactivateAfterPropertyRestore()).ToList();
			var checkoutDays = restored.Where(r => r.CheckoutEffectiveAt is not null)
				.ToDictionary(r => r.Id, r => DateOnly.FromDateTime(ItalianTime.FromUtc(r.CheckoutEffectiveAt!.Value)));
			var lineToReservation = restored.SelectMany(r => r.Lines.Select(l => (l.Id, r.Id))).ToDictionary(x => x.Item1, x => x.Item2);
			foreach (var c in await _reservations.ListLineConsumptionsAsync(lineToReservation.Keys.ToList(), ct))
				if (c.State == ConsumptionState.Released && c.SourceId is { } line
					&& !(checkoutDays.TryGetValue(lineToReservation[line], out var since) && c.ConsumptionDate >= since))
					c.Reactivate();
			await _reservations.SaveChangesAsync(ct);
			_logger.LogInformation("Stabilimento {Property} ripristinato: {Count} prenotazioni riattivate", @event.PropertyId, restored.Count);
		}
	}
}
