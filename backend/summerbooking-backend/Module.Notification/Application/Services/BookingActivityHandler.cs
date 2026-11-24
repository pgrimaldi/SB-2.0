using Shared.Contracts.Booking;
using Shared.Contracts.Events;

namespace Module.Notification.Application.Services;

/// <summary>
/// Attività di Booking per la campanella del gestionale: da fare, quando Notification avrà le notifiche in-app (chi le
/// riceve fra gli utenti dello stabilimento, lette/non lette, API della campanella). Oggi l'evento arriva e non produce nulla.
/// </summary>
public sealed class BookingActivityHandler : IIntegrationEventHandler<BookingActivity>
{
	public Task HandleAsync(BookingActivity @event, CancellationToken ct = default) => Task.CompletedTask;
}
