using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Module.Booking.Domain.Entities;

namespace Module.Booking.Infrastructure.Infras;

/// <summary>
/// Una riga, un servizio, un tavolo, un ospite o uno sconto che cambia fa risultare cambiata anche la sua prenotazione: così
/// la testata prende un numero nuovo del cursore di sincronizzazione (trigger sulla testata) e una versione nuova, e il
/// dispositivo offline sa che deve ricaricarla.
/// </summary>
internal sealed class ReservationTouchInterceptor : SaveChangesInterceptor
{
	public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
	{
		Touch(eventData.Context);
		return base.SavingChanges(eventData, result);
	}

	public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
		DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
	{
		Touch(eventData.Context);
		return base.SavingChangesAsync(eventData, result, cancellationToken);
	}

	private static void Touch(Microsoft.EntityFrameworkCore.DbContext? context)
	{
		if (context is null)
			return;
		var parents = new List<Reservation>();
		foreach (var entry in context.ChangeTracker.Entries().Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
		{
			var parent = entry.Entity switch
			{
				BookingLine l => l.Reservation,
				BookingServiceLine s => s.Reservation,
				BookingTableLine t => t.Reservation,
				BookingLineAdditionalItem a => a.Reservation,
				BookingAgreementLine g => g.Reservation,
				BookingDiscount d => d.Reservation,
				BookingGuest g => g.Reservation,
				_ => null,
			};
			if (parent is not null)
				parents.Add(parent);
		}
		foreach (var reservation in parents.Distinct())
		{
			var entry = context.Entry(reservation);
			if (entry.State == EntityState.Unchanged)
				entry.Property(r => r.UpdatedAt).IsModified = true;
		}
	}
}
