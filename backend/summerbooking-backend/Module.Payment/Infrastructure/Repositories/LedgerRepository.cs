using Microsoft.EntityFrameworkCore;
using Module.Payment.Application.Abstractions;
using Module.Payment.Domain.Entities;

namespace Module.Payment.Infrastructure.Repositories;

public sealed class LedgerRepository : ILedgerRepository
{
	private readonly DbContext _db;

	public LedgerRepository(DbContext db) => _db = db;

	public async Task<IReadOnlyList<LedgerEntry>> ListByReservationAsync(Guid propertyId, Guid reservationId, CancellationToken ct = default)
		=> await _db.LedgerEntries
			.Where(e => e.PropertyId == propertyId && e.ReservationPublicId == reservationId)
			.OrderBy(e => e.OccurredAt).ThenBy(e => e.Id)
			.ToListAsync(ct);

	public Task<LedgerEntry?> FindAsync(Guid propertyId, Guid entryId, CancellationToken ct = default)
		=> _db.LedgerEntries.FirstOrDefaultAsync(e => e.PropertyId == propertyId && e.PublicId == entryId, ct);

	public void Add(LedgerEntry entry) => _db.LedgerEntries.Add(entry);

	public void Void(LedgerEntry entry) => _db.LedgerEntries.Remove(entry);

	public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
