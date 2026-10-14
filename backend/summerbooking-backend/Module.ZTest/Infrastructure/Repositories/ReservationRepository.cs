using System.Collections.Concurrent;
using Module.ZTest.Application.Abstractions;
using Module.ZTest.Domain.Entities;
using Module.ZTest.Domain.ValueObjects;

namespace Module.ZTest.Infrastructure.Repositories;

/// <summary>
/// Implementazione di esempio del repository (in-memory).
/// In un modulo reale userebbe il DbContext EF Core e le query su DbSet.
/// </summary>
public sealed class ReservationRepository : IReservationRepository
{
	private readonly ConcurrentDictionary<Guid, Reservation> _store = new();

	public Task<Reservation?> GetByIdAsync(Guid id, CancellationToken ct = default)
		=> Task.FromResult(_store.TryGetValue(id, out var reservation) ? reservation : null);

	public Task AddAsync(Reservation reservation, CancellationToken ct = default)
	{
		_store[reservation.Id] = reservation;
		return Task.CompletedTask;
	}

	public Task<bool> HasOverlapAsync(Guid guestId, DateRange period, CancellationToken ct = default)
	{
		var overlap = _store.Values.Any(r => r.GuestId == guestId && r.Period.Overlaps(period));
		return Task.FromResult(overlap);
	}

	// In-memory: nessuna unit-of-work da confermare. Con EF Core delegherebbe a DbContext.SaveChangesAsync.
	public Task SaveChangesAsync(CancellationToken ct = default) => Task.CompletedTask;
}
