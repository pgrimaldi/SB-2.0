using Microsoft.EntityFrameworkCore;
using Module.Booking.Application.Abstractions;
using Module.Booking.Domain.Entities;
using Module.Booking.Domain.Enums;

namespace Module.Booking.Infrastructure.Repositories;

public sealed class ResaleRepository : IResaleRepository
{
	private readonly DbContext _db;

	public ResaleRepository(DbContext db) => _db = db;

	private IQueryable<ResaleOffer> Live()
		=> _db.ResaleOffers.Where(o => o.OfferState != ResaleOfferState.Withdrawn);

	public Task<ResaleSettings?> GetSettingsAsync(Guid propertyId, CancellationToken ct = default)
		=> _db.ResaleSettings.FirstOrDefaultAsync(s => s.PropertyId == propertyId, ct);

	public void AddSettings(ResaleSettings settings) => _db.ResaleSettings.Add(settings);

	public async Task<IReadOnlyList<ResaleOffer>> ListBySourceAsync(IReadOnlyCollection<long> reservationIds, CancellationToken ct = default)
		=> reservationIds.Count == 0 ? [] : await Live().Where(o => reservationIds.Contains(o.SourceReservationId)).ToListAsync(ct);

	public async Task<IReadOnlyList<ResaleOffer>> ListAvailableAsync(
		Guid propertyId, IReadOnlyCollection<Guid> spotIds, DateOnly from, DateOnly to, CancellationToken ct = default)
		=> spotIds.Count == 0
			? []
			: await _db.ResaleOffers
				.Include(o => o.SourceReservation)
				.Where(o => o.PropertyId == propertyId && o.OfferState == ResaleOfferState.Available && spotIds.Contains(o.ResourceId)
					&& o.OfferDate >= from && o.OfferDate <= to)
				.ToListAsync(ct);

	public async Task<IReadOnlyList<ResaleOffer>> ListTakenByAsync(long reservationId, CancellationToken ct = default)
		=> await _db.ResaleOffers.Include(o => o.SourceReservation)
			.Where(o => o.TakenByReservationId == reservationId && o.OfferState == ResaleOfferState.Reassigned).ToListAsync(ct);

	public async Task<IReadOnlyList<ResaleOffer>> ListOnDayAsync(Guid propertyId, DateOnly day, CancellationToken ct = default)
		=> await Live().Include(o => o.SourceReservation).Where(o => o.PropertyId == propertyId && o.OfferDate == day).AsNoTracking().ToListAsync(ct);

	public async Task<IReadOnlyList<ResaleOffer>> GetOffersAsync(Guid propertyId, IReadOnlyCollection<Guid> publicIds, CancellationToken ct = default)
		=> await _db.ResaleOffers.Include(o => o.SourceReservation)
			.Where(o => o.PropertyId == propertyId && publicIds.Contains(o.PublicId)).ToListAsync(ct);

	public async Task<IReadOnlyList<ResaleOffer>> SearchAsync(
		Guid propertyId, DateOnly? from, DateOnly? to, IReadOnlyCollection<ResaleOrigin>? origins, int max, CancellationToken ct = default)
	{
		var query = Live().Where(o => o.PropertyId == propertyId && o.SourceReservation.DeletedAt == null);
		if (from is { } f)
			query = query.Where(o => o.OfferDate >= f);
		if (to is { } t)
			query = query.Where(o => o.OfferDate <= t);
		if (origins is { Count: > 0 })
			query = query.Where(o => origins.Contains(o.Origin));
		return await query.Include(o => o.SourceReservation).Include(o => o.BookingLine)
			.OrderByDescending(o => o.OfferDate).Take(max).AsNoTracking().ToListAsync(ct);
	}

	public async Task<(int Units, decimal Amount, bool EverEarned)> CreditOfAsync(long offerId, CancellationToken ct = default)
	{
		var rows = await _db.ResaleCreditLedgerEntries.Where(e => e.ResaleOfferId == offerId).Select(e => new { e.CreditUnits, e.Amount }).ToListAsync(ct);
		return (rows.Sum(r => r.CreditUnits), rows.Sum(r => r.Amount), rows.Count > 0);
	}

	public async Task<IReadOnlyList<ResaleCreditLedgerEntry>> ListCreditsAsync(Guid propertyId, Guid customerId, CancellationToken ct = default)
		=> await _db.ResaleCreditLedgerEntries.Include(e => e.SourceReservation)
			.Where(e => e.PropertyId == propertyId && e.CustomerId == customerId)
			.OrderByDescending(e => e.OccurredAt).AsNoTracking().ToListAsync(ct);

	public void AddOffer(ResaleOffer offer) => _db.ResaleOffers.Add(offer);

	public void AddCredit(ResaleCreditLedgerEntry entry) => _db.ResaleCreditLedgerEntries.Add(entry);

	public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
