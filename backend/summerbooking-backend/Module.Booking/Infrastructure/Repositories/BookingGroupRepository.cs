using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Module.Booking.Application.Abstractions;
using Module.Booking.Domain.Entities;

namespace Module.Booking.Infrastructure.Repositories;

public sealed class BookingGroupRepository : IBookingGroupRepository
{
	private readonly DbContext _db;

	public BookingGroupRepository(DbContext db) => _db = db;

	private IQueryable<BookingGroup> WithMembers()
		=> _db.BookingGroups
			.Include(g => g.Members.Where(m => m.DeletedAt == null))
				.ThenInclude(m => m.Reservation)
					.ThenInclude(r => r.Lines.Where(l => l.DeletedAt == null))
			.AsSplitQuery();

	public Task<BookingGroup?> GetAsync(Guid propertyId, Guid publicId, CancellationToken ct = default)
		=> WithMembers().FirstOrDefaultAsync(g => g.PropertyId == propertyId && g.PublicId == publicId, ct);

	public async Task<IReadOnlyList<BookingGroup>> GetManyAsync(Guid propertyId, IReadOnlyCollection<Guid> publicIds, CancellationToken ct = default)
		=> await WithMembers().Where(g => g.PropertyId == propertyId && publicIds.Contains(g.PublicId)).ToListAsync(ct);

	public async Task<(IReadOnlyList<BookingGroup> Items, int Total)> ListAsync(
		Guid propertyId, string? search, string sortBy, bool descending, int page, int pageSize, CancellationToken ct = default)
	{
		var query = _db.BookingGroups.Where(g => g.PropertyId == propertyId);
		if (!string.IsNullOrWhiteSpace(search))
			query = query.Where(g => EF.Functions.ILike(g.Name, LikePattern.Contains(search.Trim())));
		var total = await query.CountAsync(ct);
		query = (sortBy, descending) switch
		{
			("name", false) => query.OrderBy(g => g.Name).ThenBy(g => g.Id),
			("name", true) => query.OrderByDescending(g => g.Name).ThenByDescending(g => g.Id),
			("reservations", false) => query.OrderBy(g => g.Members.Count(m => m.DeletedAt == null)).ThenBy(g => g.Id),
			("reservations", true) => query.OrderByDescending(g => g.Members.Count(m => m.DeletedAt == null)).ThenByDescending(g => g.Id),
			(_, false) => query.OrderBy(g => g.CreatedAt).ThenBy(g => g.Id),
			(_, true) => query.OrderByDescending(g => g.CreatedAt).ThenByDescending(g => g.Id),
		};
		var ids = await query.Skip((page - 1) * pageSize).Take(pageSize).Select(g => g.Id).ToListAsync(ct);
		var items = (await WithMembers().AsNoTracking().Where(g => ids.Contains(g.Id)).ToListAsync(ct)).ToDictionary(g => g.Id);
		return (ids.Select(id => items[id]).ToList(), total);
	}

	public Task<bool> NameExistsAsync(Guid propertyId, string name, long? exceptId, CancellationToken ct = default)
	{
		var lower = name.Trim().ToLowerInvariant();
		return _db.BookingGroups.AnyAsync(g => g.PropertyId == propertyId && g.Name.ToLower() == lower && (exceptId == null || g.Id != exceptId), ct);
	}

	public async Task<IReadOnlyDictionary<long, GroupInfo>> GroupsOfAsync(IReadOnlyCollection<long> reservationIds, CancellationToken ct = default)
	{
		if (reservationIds.Count == 0)
			return new Dictionary<long, GroupInfo>();
		var rows = await _db.Set<BookingGroupMember>()
			.Where(m => m.DeletedAt == null && reservationIds.Contains(m.ReservationId) && m.Group.DeletedAt == null)
			.Select(m => new
			{
				m.ReservationId,
				Info = new GroupInfo(m.Group.Id, m.Group.PublicId, m.Group.Name, m.Group.Members.Count(x => x.DeletedAt == null)),
			})
			.ToListAsync(ct);
		return rows.ToDictionary(r => r.ReservationId, r => r.Info);
	}

	public void Add(BookingGroup group) => _db.BookingGroups.Add(group);

	public void RemoveMember(BookingGroupMember member) => _db.Set<BookingGroupMember>().Remove(member);

	public void Remove(BookingGroup group)
	{
		foreach (var member in group.LiveMembers.ToList())
			_db.Set<BookingGroupMember>().Remove(member);
		_db.BookingGroups.Remove(group);
	}

	public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
