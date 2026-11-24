using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Module.Booking.Application.Abstractions;
using Module.Booking.Domain.Entities;
using Module.Booking.Domain;
using Module.Booking.Domain.Enums;
using Npgsql;
using Shared.Contracts.Errors;

namespace Module.Booking.Infrastructure.Repositories;

/// <remarks>
/// Cestino = cancellate logicamente <b>dal nuovo sistema</b> (con <c>cancelled_at</c>): le prenotazioni eliminate nel vecchio
/// gestionale arrivano dalla migrazione con <c>deleted_at</c> ma senza <c>cancelled_at</c>, non si ripristinano e non le
/// tocca il job del cestino (le governa la retention).
/// </remarks>
public sealed class ReservationRepository : IReservationRepository
{
	private readonly DbContext _db;

	public ReservationRepository(DbContext db) => _db = db;

	private IQueryable<Reservation> WithChildren(bool includeTrashed)
	{
		var query = _db.Reservations
			.Include(r => r.Lines.Where(l => l.DeletedAt == null))
			.Include(r => r.AdditionalItems.Where(a => a.DeletedAt == null))
			.Include(r => r.AgreementLines.Where(a => a.DeletedAt == null))
			.Include(r => r.Discounts.Where(d => d.DeletedAt == null))
			.Include(r => r.Guests.Where(g => g.DeletedAt == null))
			.Include(r => r.ServiceLines.Where(s => s.DeletedAt == null)).ThenInclude(s => s.TimeSlots.Where(t => t.DeletedAt == null))
			.Include(r => r.TableLines.Where(t => t.DeletedAt == null))
			.AsSplitQuery();
		return includeTrashed ? query.IgnoreQueryFilters() : query;
	}

	public Task<Reservation?> GetAsync(Guid propertyId, Guid publicId, bool includeTrashed = false, CancellationToken ct = default)
		=> WithChildren(includeTrashed).FirstOrDefaultAsync(r => r.PropertyId == propertyId && r.PublicId == publicId, ct);

	public Task<Reservation?> FindByLineAsync(Guid propertyId, Guid linePublicId, CancellationToken ct = default)
		=> WithChildren(false).FirstOrDefaultAsync(r => r.PropertyId == propertyId && r.Lines.Any(l => l.PublicId == linePublicId && l.DeletedAt == null), ct);

	public async Task<IReadOnlyList<Reservation>> GetManyAsync(Guid propertyId, IReadOnlyCollection<Guid> publicIds, bool trashed, CancellationToken ct = default)
		=> await WithChildren(trashed)
			.Where(r => r.PropertyId == propertyId && publicIds.Contains(r.PublicId)
				&& (trashed ? r.DeletedAt != null && r.CancelledAt != null : r.DeletedAt == null))
			.ToListAsync(ct);

	public async Task<(IReadOnlyList<Reservation> Items, int Total)> ListAsync(ReservationFilter f, CancellationToken ct = default)
	{
		IQueryable<Reservation> query = f.Trashed
			? _db.Reservations.IgnoreQueryFilters().Where(r => r.DeletedAt != null && r.CancelledAt != null)
			: _db.Reservations;
		query = query.Where(r => r.PropertyId == f.PropertyId);

		if (f.From is { } from)
			query = query.Where(r => r.StayTo == null || r.StayTo >= from);
		if (f.To is { } to)
			query = query.Where(r => r.StayFrom == null || r.StayFrom <= to);
		if (f.Status is { } status)
			query = query.Where(r => r.Status == status);
		if (f.Channel is { } channel)
			query = query.Where(r => r.Channel == channel);
		if (f.Seasonal is { } seasonal)
			query = query.Where(r => r.HasSeasonalPass == seasonal);
		if (f.AgreementPartnerId is { } partner)
			query = query.Where(r => r.AgreementLines.Any(a => a.AgreementPartnerId == partner && a.DeletedAt == null) || r.AgreementPartnerId == partner);
		query = f.PaymentStatus switch
		{
			PaymentStatuses.Paid => query.Where(r => r.BalanceDue <= 0),
			PaymentStatuses.Partial => query.Where(r => r.Paid > 0 && r.BalanceDue > 0),
			PaymentStatuses.Unpaid => query.Where(r => r.Paid <= 0 && r.Total > 0),
			_ => query,
		};
		if (f.SpotIds is { Count: > 0 } spots)
			query = query.Where(r => r.Lines.Any(l => l.DeletedAt == null
				&& ((l.ResourceId != null && spots.Contains(l.ResourceId.Value)) || (l.AreaResourceId != null && spots.Contains(l.AreaResourceId.Value)))));
		if (f.NumberSearch is { } number && f.CustomerIds is { } customers)
		{
			var pattern = LikePattern.Contains(number);
			var bySpot = f.SearchSpotIds ?? [];
			query = query.Where(r => EF.Functions.ILike(r.BookingNumber, pattern) || (r.CustomerId != null && customers.Contains(r.CustomerId.Value))
				|| r.Lines.Any(l => l.DeletedAt == null
					&& ((l.ResourceId != null && bySpot.Contains(l.ResourceId.Value)) || (l.AreaResourceId != null && bySpot.Contains(l.AreaResourceId.Value)))));
		}

		var total = await query.CountAsync(ct);
		query = (f.SortBy, f.Descending) switch
		{
			("bookingNumber", false) => query.OrderBy(r => r.BookingNumber),
			("bookingNumber", true) => query.OrderByDescending(r => r.BookingNumber),
			("total", false) => query.OrderBy(r => r.Total).ThenBy(r => r.Id),
			("total", true) => query.OrderByDescending(r => r.Total).ThenByDescending(r => r.Id),
			("balanceDue", false) => query.OrderBy(r => r.BalanceDue).ThenBy(r => r.Id),
			("balanceDue", true) => query.OrderByDescending(r => r.BalanceDue).ThenByDescending(r => r.Id),
			("createdAt", false) => query.OrderBy(r => r.CreatedAt),
			("createdAt", true) => query.OrderByDescending(r => r.CreatedAt),
			("deletedAt", false) => query.OrderBy(r => r.DeletedAt),
			("deletedAt", true) => query.OrderByDescending(r => r.DeletedAt),
			(_, false) => query.OrderBy(r => r.StayFrom).ThenBy(r => r.Id),
			(_, true) => query.OrderByDescending(r => r.StayFrom).ThenByDescending(r => r.Id),
		};
		var items = await query
			.Include(r => r.Lines.Where(l => l.DeletedAt == null))
			.Include(r => r.AgreementLines.Where(a => a.DeletedAt == null))
			.AsSplitQuery()
			.Skip((f.Page - 1) * f.PageSize).Take(f.PageSize)
			.ToListAsync(ct);
		return (items, total);
	}

	public async Task<IReadOnlyList<BookingLine>> ListLinesAsync(Guid propertyId, DateOnly from, DateOnly to, CancellationToken ct = default)
		=> await _db.BookingLines
			.Include(l => l.Reservation)
			.Where(l => l.PropertyId == propertyId && l.StartDate <= to && l.EndDate >= from
				&& l.Reservation.DeletedAt == null && l.Reservation.Status != ReservationStatus.Cancelled)
			.AsNoTracking()
			.ToListAsync(ct);

	public async Task<IReadOnlyList<OccupiedDay>> ListOccupiedAsync(
		Guid propertyId, IReadOnlyCollection<Guid> spotIds, DateOnly from, DateOnly to, CancellationToken ct = default)
		=> spotIds.Count == 0
			? []
			: await _db.ResourceConsumptions
				.Where(c => c.PropertyId == propertyId && c.ResourceId != null && spotIds.Contains(c.ResourceId.Value)
					&& c.ConsumptionDate >= from && c.ConsumptionDate <= to && c.State != ConsumptionState.Released)
				.Select(c => new OccupiedDay(c.ResourceId!.Value, c.ConsumptionDate, c.BookingPeriod, c.SourceKind, c.SourceId, c.Quantity))
				.ToListAsync(ct);

	public async Task<IReadOnlyList<ResourceConsumption>> ListLineConsumptionsAsync(IReadOnlyCollection<long> lineIds, CancellationToken ct = default)
		=> lineIds.Count == 0
			? []
			: await _db.ResourceConsumptions
				.Where(c => c.SourceKind == ConsumptionSource.ReservationLine && c.SourceId != null && lineIds.Contains(c.SourceId.Value))
				.ToListAsync(ct);

	public async Task<IReadOnlyList<Reservation>> ListForPropertyAsync(Guid propertyId, DateOnly? endingFrom, CancellationToken ct = default)
		=> await _db.Reservations
			.Include(r => r.Lines.Where(l => l.DeletedAt == null))
			.Where(r => r.PropertyId == propertyId && (endingFrom == null || r.StayTo == null || r.StayTo >= endingFrom))
			.AsSplitQuery()
			.ToListAsync(ct);

	public async Task<IReadOnlyList<BookingEntrance>> ListEntrancesAsync(Guid propertyId, DateOnly day, CancellationToken ct = default)
		=> await _db.BookingEntrances.Where(e => e.PropertyId == propertyId && e.EntryDate == day).ToListAsync(ct);

	public async Task<string> NextBookingNumberAsync(Guid propertyId, string prefix, CancellationToken ct = default)
	{
		// Riga del contatore creata alla prima prenotazione; l'incremento è atomico anche con richieste contemporanee.
		var next = await _db.Database.SqlQuery<NumberRow>($"""
			INSERT INTO booking.number_sequences AS s (property_id, number_prefix, last_sequence)
			VALUES ({propertyId}, {prefix}, 1)
			ON CONFLICT (property_id) DO UPDATE SET last_sequence = s.last_sequence + 1, updated_at = now()
			RETURNING s.number_prefix AS "Prefix", s.last_sequence AS "Sequence"
			""").ToListAsync(ct);
		return $"{next[0].Prefix}-{next[0].Sequence}";
	}

	private sealed record NumberRow(string Prefix, long Sequence);

	public Task<bool> ReservationCodeExistsAsync(string code, CancellationToken ct = default)
		=> _db.Reservations.IgnoreQueryFilters().AnyAsync(r => r.ReservationCode == code, ct);

	public void Add(Reservation reservation) => _db.Reservations.Add(reservation);

	public void AddConsumptions(IEnumerable<ResourceConsumption> consumptions) => _db.ResourceConsumptions.AddRange(consumptions);

	public void AddEntrance(BookingEntrance entrance) => _db.BookingEntrances.Add(entrance);

	public void RemoveEntrance(BookingEntrance entrance) => _db.BookingEntrances.Remove(entrance);

	public void RemoveLine(BookingLine line) => _db.BookingLines.Remove(line);

	public void RemoveChildren(Reservation reservation, BookingLine? onlyLine = null)
	{
		_db.BookingLineAdditionalItems.RemoveRange(reservation.AdditionalItems.Where(a => onlyLine is null || a.BookingLineId == onlyLine.Id));
		_db.BookingAgreementLines.RemoveRange(reservation.AgreementLines.Where(a => onlyLine is null || a.BookingLineId == onlyLine.Id));
		if (onlyLine is null)
			_db.BookingDiscounts.RemoveRange(reservation.Discounts);
	}

	public void Trash(Reservation reservation, DateTime now) => _db.Entry(reservation).Property(r => r.DeletedAt).CurrentValue = now;

	/// <summary>
	/// Ripristino dal cestino: tornano vive le righe figlie tolte insieme alla prenotazione (cancellate al massimo 5 secondi
	/// prima di lei), non quelle tolte prima con una modifica.
	/// </summary>
	public async Task ReviveTrashedChildrenAsync(Guid propertyId, IReadOnlyCollection<Guid> reservationIds, CancellationToken ct = default)
	{
		if (reservationIds.Count == 0)
			return;
		var ids = reservationIds.ToArray();
		await _db.BookingLines.IgnoreQueryFilters()
			.Where(c => c.DeletedAt != null && c.Reservation.PropertyId == propertyId && ids.Contains(c.Reservation.PublicId)
				&& c.Reservation.DeletedAt != null && c.Reservation.CancelledAt != null && c.DeletedAt >= c.Reservation.DeletedAt.Value.AddSeconds(-5))
			.ExecuteUpdateAsync(u => u.SetProperty(c => c.DeletedAt, (DateTime?)null), ct);
		await _db.BookingLineAdditionalItems.IgnoreQueryFilters()
			.Where(c => c.DeletedAt != null && c.Reservation.PropertyId == propertyId && ids.Contains(c.Reservation.PublicId)
				&& c.Reservation.DeletedAt != null && c.Reservation.CancelledAt != null && c.DeletedAt >= c.Reservation.DeletedAt.Value.AddSeconds(-5))
			.ExecuteUpdateAsync(u => u.SetProperty(c => c.DeletedAt, (DateTime?)null), ct);
		await _db.BookingAgreementLines.IgnoreQueryFilters()
			.Where(c => c.DeletedAt != null && c.Reservation.PropertyId == propertyId && ids.Contains(c.Reservation.PublicId)
				&& c.Reservation.DeletedAt != null && c.Reservation.CancelledAt != null && c.DeletedAt >= c.Reservation.DeletedAt.Value.AddSeconds(-5))
			.ExecuteUpdateAsync(u => u.SetProperty(c => c.DeletedAt, (DateTime?)null), ct);
		await _db.BookingDiscounts.IgnoreQueryFilters()
			.Where(c => c.DeletedAt != null && c.Reservation.PropertyId == propertyId && ids.Contains(c.Reservation.PublicId)
				&& c.Reservation.DeletedAt != null && c.Reservation.CancelledAt != null && c.DeletedAt >= c.Reservation.DeletedAt.Value.AddSeconds(-5))
			.ExecuteUpdateAsync(u => u.SetProperty(c => c.DeletedAt, (DateTime?)null), ct);
		await _db.BookingGuests.IgnoreQueryFilters()
			.Where(c => c.DeletedAt != null && c.Reservation.PropertyId == propertyId && ids.Contains(c.Reservation.PublicId)
				&& c.Reservation.DeletedAt != null && c.Reservation.CancelledAt != null && c.DeletedAt >= c.Reservation.DeletedAt.Value.AddSeconds(-5))
			.ExecuteUpdateAsync(u => u.SetProperty(c => c.DeletedAt, (DateTime?)null), ct);
	}

	public void Untrash(Reservation reservation) => _db.Entry(reservation).Property(r => r.DeletedAt).CurrentValue = null;

	public async Task<IReadOnlyList<(Guid PropertyId, Guid ReservationId, string BookingNumber)>> ListTrashedBeforeAsync(
		DateTime cutoff, int max, CancellationToken ct = default)
		=> (await _db.Reservations.IgnoreQueryFilters()
				.Where(r => r.DeletedAt != null && r.CancelledAt != null && r.DeletedAt < cutoff)
				.OrderBy(r => r.DeletedAt)
				.Select(r => new { r.PropertyId, r.PublicId, r.BookingNumber })
				.Take(max)
				.ToListAsync(ct))
			.Select(r => (r.PropertyId, r.PublicId, r.BookingNumber))
			.ToList();

	public async Task<int> PurgeAsync(Guid propertyId, IReadOnlyCollection<Guid> reservationIds, CancellationToken ct = default)
	{
		if (reservationIds.Count == 0)
			return 0;
		var ids = await _db.Reservations.IgnoreQueryFilters()
			.Where(r => r.PropertyId == propertyId && reservationIds.Contains(r.PublicId) && r.DeletedAt != null && r.CancelledAt != null)
			.Select(r => r.Id).ToListAsync(ct);
		if (ids.Count == 0)
			return 0;
		var lineIds = await _db.BookingLines.IgnoreQueryFilters().Where(l => ids.Contains(l.ReservationId)).Select(l => l.Id).ToListAsync(ct);
		var serviceLineIds = await _db.BookingServiceLines.IgnoreQueryFilters().Where(l => ids.Contains(l.ReservationId)).Select(l => l.Id).ToListAsync(ct);

		// I consumi non hanno una chiave esterna verso le righe: si tolgono prima. Il resto segue la testata (ON DELETE CASCADE).
		await _db.ResourceConsumptions.IgnoreQueryFilters()
			.Where(c => c.SourceId != null && ((c.SourceKind == ConsumptionSource.ReservationLine && lineIds.Contains(c.SourceId.Value))
				|| (c.SourceKind == ConsumptionSource.ServiceLine && serviceLineIds.Contains(c.SourceId.Value))))
			.ExecuteDeleteAsync(ct);
		return await _db.Reservations.IgnoreQueryFilters().Where(r => ids.Contains(r.Id)).ExecuteDeleteAsync(ct);
	}

	public async Task<IReadOnlyList<ResourceConsumption>> ListServiceConsumptionsAsync(IReadOnlyCollection<long> serviceLineIds, CancellationToken ct = default)
		=> serviceLineIds.Count == 0
			? []
			: await _db.ResourceConsumptions
				.Where(c => c.SourceKind == ConsumptionSource.ServiceLine && c.SourceId != null && serviceLineIds.Contains(c.SourceId.Value))
				.ToListAsync(ct);

	public async Task<IReadOnlyDictionary<Guid, DateTime>> LastServiceOrdersAsync(
		Guid propertyId, Guid customerId, IReadOnlyCollection<Guid> serviceIds, DateTime sinceUtc, long excludeReservationId, CancellationToken ct = default)
		=> serviceIds.Count == 0
			? new Dictionary<Guid, DateTime>()
			: await _db.BookingServiceLines
				.Where(s => s.PropertyId == propertyId && s.SaleItemId != null && serviceIds.Contains(s.SaleItemId.Value) && s.DeletedAt == null
					&& s.CreatedAt >= sinceUtc && s.ReservationId != excludeReservationId && s.Reservation.CustomerId == customerId
					&& s.Reservation.DeletedAt == null && s.Reservation.Status != ReservationStatus.Cancelled)
				.GroupBy(s => s.SaleItemId!.Value)
				.Select(g => new { g.Key, Last = g.Max(s => s.CreatedAt) })
				.ToDictionaryAsync(x => x.Key, x => x.Last, ct);

	public async Task<IReadOnlyList<ServiceUsage>> ListServiceUsageAsync(
		Guid propertyId, IReadOnlyCollection<Guid> serviceIds, DateOnly from, DateOnly to, long excludeReservationId, CancellationToken ct = default)
		=> serviceIds.Count == 0
			? []
			: await _db.ResourceConsumptions
				.Where(c => c.PropertyId == propertyId && c.SourceKind == ConsumptionSource.ServiceLine && c.ResourceId != null
					&& serviceIds.Contains(c.ResourceId.Value) && c.ConsumptionDate >= from && c.ConsumptionDate <= to && c.State != ConsumptionState.Released
					&& !_db.BookingServiceLines.Any(s => s.Id == c.SourceId && s.ReservationId == excludeReservationId))
				.Select(c => new ServiceUsage(c.ResourceId!.Value, c.ConsumptionDate, c.SlotStart, c.SlotEnd, c.Quantity))
				.ToListAsync(ct);

	public async Task<IReadOnlyList<BookingTableLine>> ListTableLinesAsync(
		Guid propertyId, IReadOnlyCollection<Guid> tableServiceIds, DateOnly from, DateOnly to, long excludeReservationId, CancellationToken ct = default)
		=> await _db.BookingTableLines.Include(t => t.Reservation)
			.Where(t => t.PropertyId == propertyId && tableServiceIds.Contains(t.TableServiceId) && t.ServiceDate >= from && t.ServiceDate <= to
				&& t.ReservationId != excludeReservationId && t.Reservation.DeletedAt == null && t.Reservation.Status != ReservationStatus.Cancelled)
			.AsNoTracking().ToListAsync(ct);

	public async Task<IReadOnlyList<BookingServiceTimeSlot>> ListServiceSlotsAsync(Guid propertyId, DateOnly day, CancellationToken ct = default)
		=> await _db.BookingServiceTimeSlots.Include(t => t.ServiceLine).Include(t => t.Reservation)
			.Where(t => t.PropertyId == propertyId && t.ServiceDate == day && t.ServiceLine.DeletedAt == null
				&& t.Reservation.DeletedAt == null && t.Reservation.Status != ReservationStatus.Cancelled)
			.AsNoTracking().ToListAsync(ct);

	public async Task<IReadOnlyList<ItemUsage>> ListItemUsageAsync(
		Guid propertyId, IReadOnlyCollection<Guid> itemIds, DateOnly from, DateOnly to, long excludeReservationId, CancellationToken ct = default)
		=> itemIds.Count == 0
			? []
			: await _db.ResourceConsumptions
				.Where(c => c.PropertyId == propertyId && c.ResourceType == ResourceType.InventoryItem && c.ResourceId != null
					&& itemIds.Contains(c.ResourceId.Value) && c.ConsumptionDate >= from && c.ConsumptionDate <= to && c.State != ConsumptionState.Released
					&& !(c.SourceKind == ConsumptionSource.ReservationLine && _db.BookingLines.Any(l => l.Id == c.SourceId && l.ReservationId == excludeReservationId)))
				.GroupBy(c => new { c.ResourceId, c.ConsumptionDate })
				.Select(g => new ItemUsage(g.Key.ResourceId!.Value, g.Key.ConsumptionDate, g.Sum(c => c.Quantity)))
				.ToListAsync(ct);

	public void RemoveConsumption(ResourceConsumption consumption) => _db.ResourceConsumptions.Remove(consumption);

	public void RemoveServiceLine(BookingServiceLine line) => _db.BookingServiceLines.Remove(line);

	public void RemoveTableLine(BookingTableLine line) => _db.BookingTableLines.Remove(line);

	public async Task<IReadOnlyList<Reservation>> ListByCustomersAsync(IReadOnlyCollection<Guid> customerIds, CancellationToken ct = default)
		=> customerIds.Count == 0
			? []
			: await WithChildren(false).Where(r => r.CustomerId != null && customerIds.Contains(r.CustomerId.Value))
				.OrderByDescending(r => r.StayFrom).ThenByDescending(r => r.Id).Take(500).ToListAsync(ct);

	public async Task<IReadOnlyList<Reservation>> ListOnlineExpiredAsync(DateTime now, int max, CancellationToken ct = default)
		=> await WithChildren(false)
			.Where(r => r.Status == ReservationStatus.Pending
				&& ((r.PaymentHoldUntil != null && r.PaymentHoldUntil < now) || (r.ConfirmationExpiresAt != null && r.ConfirmationExpiresAt < now)))
			.OrderBy(r => r.Id).Take(max).ToListAsync(ct);

	public async Task<IReadOnlyDictionary<DateOnly, int>> CountOnlineSpotsPerDayAsync(
		Guid propertyId, Guid customerId, DateOnly from, DateOnly to, CancellationToken ct = default)
	{
		var lines = await _db.BookingLines
			.Where(l => l.PropertyId == propertyId && l.StartDate <= to && l.EndDate >= from && l.Reservation.CustomerId == customerId
				&& l.Reservation.Channel == BookingChannel.Customer && l.Reservation.Status != ReservationStatus.Cancelled && l.Reservation.DeletedAt == null)
			.Select(l => new { l.StartDate, l.EndDate }).ToListAsync(ct);
		var result = new Dictionary<DateOnly, int>();
		foreach (var line in lines)
			for (var d = line.StartDate < from ? from : line.StartDate; d <= line.EndDate && d <= to; d = d.AddDays(1))
				result[d] = result.GetValueOrDefault(d) + 1;
		return result;
	}

	public async Task<IReadOnlyList<ResourceConsumption>> ListAllConsumptionsAsync(Reservation reservation, CancellationToken ct = default)
		=> (await ListLineConsumptionsAsync(reservation.Lines.Where(l => l.Id != 0).Select(l => l.Id).ToList(), ct))
			.Concat(await ListServiceConsumptionsAsync(reservation.ServiceLines.Where(s => s.Id != 0).Select(s => s.Id).ToList(), ct)).ToList();

	public async Task<SyncChanges> ListChangesAsync(Guid propertyId, long since, DateTime olderThan, int max, CancellationToken ct = default)
	{
		var reservations = await _db.Reservations.IgnoreQueryFilters()
			.Include(r => r.Lines.Where(l => l.DeletedAt == null))
			.Where(r => r.PropertyId == propertyId && r.SyncSeq > since && r.UpdatedAt < olderThan)
			.OrderBy(r => r.SyncSeq).Take(max).AsSplitQuery().AsNoTracking().ToListAsync(ct);
		var holds = await _db.SpotHolds.IgnoreQueryFilters()
			.Where(h => h.PropertyId == propertyId && h.SyncSeq > since && h.UpdatedAt < olderThan)
			.OrderBy(h => h.SyncSeq).Take(max).AsNoTracking().ToListAsync(ct);
		return new SyncChanges(reservations, holds);
	}

	public Task<bool> HasRenewalAsync(long reservationId, CancellationToken ct = default)
		=> _db.Reservations.AnyAsync(r => r.RenewedFromReservationId == reservationId, ct);

	public void DiscardChanges() => _db.ChangeTracker.Clear();

	/// <summary>La postazione presa nel frattempo da un'altra prenotazione (vincolo di esclusione) diventa un 409.</summary>
	public async Task SaveChangesAsync(CancellationToken ct = default)
	{
		try
		{
			await _db.SaveChangesAsync(ct);
		}
		catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.ExclusionViolation })
		{
			throw DomainException.Conflict(BookingErrors.SpotUnavailable);
		}
		catch (DbUpdateConcurrencyException)
		{
			throw DomainException.Conflict(BookingErrors.VersionConflict);
		}
	}

	public async Task<IBookingTransaction> BeginTransactionAsync(CancellationToken ct = default)
		=> new Transaction(await _db.Database.BeginTransactionAsync(ct));

	private sealed class Transaction : IBookingTransaction
	{
		private readonly IDbContextTransaction _inner;

		public Transaction(IDbContextTransaction inner) => _inner = inner;

		public Task CommitAsync(CancellationToken ct = default) => _inner.CommitAsync(ct);

		public ValueTask DisposeAsync() => _inner.DisposeAsync();
	}
}

public sealed class AttachmentRepository : IAttachmentRepository
{
	private readonly DbContext _db;

	public AttachmentRepository(DbContext db) => _db = db;

	public async Task<IReadOnlyList<BookingAttachment>> ListAsync(long reservationId, CancellationToken ct = default)
		=> await _db.BookingAttachments.Where(a => a.ReservationId == reservationId).OrderBy(a => a.CreatedAt).ToListAsync(ct);

	public async Task<IReadOnlyList<BookingAttachment>> ListWithFilesBeforeAsync(DateTime cutoff, int max, CancellationToken ct = default)
		=> await _db.BookingAttachments.IgnoreQueryFilters().Where(a => a.StoragePath != null && a.CreatedAt < cutoff).OrderBy(a => a.Id).Take(max).ToListAsync(ct);

	public void Add(BookingAttachment attachment) => _db.BookingAttachments.Add(attachment);

	public void Remove(BookingAttachment attachment) => _db.BookingAttachments.Remove(attachment);

	public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}

public sealed class ProcessedCommandRepository : IProcessedCommandRepository
{
	private readonly DbContext _db;

	public ProcessedCommandRepository(DbContext db) => _db = db;

	public Task<ProcessedCommand?> FindAsync(Guid propertyId, string key, CancellationToken ct = default)
		=> _db.ProcessedCommands.AsNoTracking().FirstOrDefaultAsync(c => c.PropertyId == propertyId && c.IdempotencyKey == key, ct);

	public async Task TryAddAsync(ProcessedCommand command, CancellationToken ct = default)
	{
		_db.ProcessedCommands.Add(command);
		try
		{
			await _db.SaveChangesAsync(ct);
		}
		catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
		{
			// stessa chiave ricordata nel frattempo da una richiesta contemporanea: vale la sua
		}
		finally
		{
			_db.Entry(command).State = EntityState.Detached;
		}
	}

	public Task<int> PurgeBeforeAsync(DateTime cutoff, CancellationToken ct = default)
		=> _db.ProcessedCommands.Where(c => c.CreatedAt < cutoff).ExecuteDeleteAsync(ct);
}

public sealed class SpotHoldRepository : ISpotHoldRepository
{
	private readonly DbContext _db;

	public SpotHoldRepository(DbContext db) => _db = db;

	public Task<SpotHold?> GetAsync(Guid propertyId, Guid publicId, CancellationToken ct = default)
		=> _db.SpotHolds.FirstOrDefaultAsync(h => h.PropertyId == propertyId && h.PublicId == publicId, ct);

	public async Task<IReadOnlyList<SpotHold>> ListActiveAsync(
		Guid propertyId, DateOnly from, DateOnly to, IReadOnlyCollection<Guid>? spotIds = null, CancellationToken ct = default)
	{
		var query = _db.SpotHolds.Where(h => h.PropertyId == propertyId && h.HoldStatus == HoldStatus.Active && h.HoldKind == HoldKind.Operational
			&& h.StartDate <= to && h.EndDate >= from);
		if (spotIds is not null)
			query = query.Where(h => spotIds.Contains(h.ResourceId));
		return await query.ToListAsync(ct);
	}

	public async Task<IReadOnlyList<ResourceConsumption>> ListConsumptionsAsync(IReadOnlyCollection<long> holdIds, CancellationToken ct = default)
		=> holdIds.Count == 0
			? []
			: await _db.ResourceConsumptions
				.Where(c => c.SourceKind == ConsumptionSource.ManualHold && c.SourceId != null && holdIds.Contains(c.SourceId.Value))
				.ToListAsync(ct);

	public void Add(SpotHold hold) => _db.SpotHolds.Add(hold);
}
