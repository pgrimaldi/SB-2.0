using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Module.Audit.Application.Abstractions;
using Module.Audit.Domain.Entities;
using Module.Audit.Domain.Enums;

namespace Module.Audit.Infrastructure.Repositories;

public sealed class AuditEntryRepository : IAuditEntryRepository
{
	private readonly DbContext _db;

	public AuditEntryRepository(DbContext db) => _db = db;

	public async Task<(IReadOnlyList<AuditTrailEntry> Items, int Total)> ListAsync(AuditEntryFilter filter, int page, int pageSize, CancellationToken ct = default)
	{
		var filtered = Filter(filter);
		var total = await filtered.CountAsync(ct);
		var items = await Sort(filtered, filter.Descending)
			.Skip((page - 1) * pageSize)
			.Take(pageSize)
			.ToListAsync(ct);
		return (items, total);
	}

	public Task<int> CountAsync(AuditEntryFilter filter, CancellationToken ct = default)
		=> Filter(filter).CountAsync(ct);

	public async Task<IReadOnlyList<AuditTrailEntry>> ListAllAsync(AuditEntryFilter filter, CancellationToken ct = default)
		=> await Sort(Filter(filter), filter.Descending).ToListAsync(ct);

	public async Task<IReadOnlyList<AuditActor>> ListActorsAsync(Guid propertyId, IReadOnlyCollection<EntryCategory> categories, CancellationToken ct = default)
		=> await _db.TrailEntries.AsNoTracking()
			.Where(e => e.PropertyId == propertyId && categories.Contains(e.Category) && e.ActorUserId != null)
			.GroupBy(e => e.ActorUserId!.Value)
			.Select(g => g
				.OrderByDescending(e => e.OccurredAt)
				.Select(e => new AuditActor(g.Key, e.ActorNameSnapshot, e.ActorRoleSnapshot))
				.First())
			.ToListAsync(ct);

	public async Task AppendAsync(AuditTrailEntry e, CancellationToken ct = default)
		=> await _db.Database.ExecuteSqlInterpolatedAsync($"""
			INSERT INTO audit.trail_entries
			  (source_event_id, occurred_at, category, source_module, action_code, outcome, property_id, actor_kind,
			   actor_user_id, actor_name_snapshot, actor_role_snapshot, impersonator_user_id, target_type, target_id,
			   booking_number_snapshot, spot_label_snapshot, metadata, client_ip, client_user_agent, trace_id)
			VALUES
			  ({e.SourceEventId}, {e.OccurredAt}, {e.Category}, {e.SourceModule}, {e.ActionCode}, {e.Outcome}, {e.PropertyId}, {e.ActorKind},
			   {e.ActorUserId}, {e.ActorNameSnapshot}, {e.ActorRoleSnapshot}, {e.ImpersonatorUserId}, {e.TargetType}, {e.TargetId},
			   {e.BookingNumberSnapshot}, {e.SpotLabelSnapshot}, {e.Metadata}::jsonb, {e.ClientIp}, {e.ClientUserAgent}, {e.TraceId})
			ON CONFLICT (source_event_id, occurred_at) DO NOTHING
			""", ct);

	private IQueryable<AuditTrailEntry> Filter(AuditEntryFilter filter)
	{
		var entries = _db.TrailEntries.AsNoTracking().Where(e => filter.Categories.Contains(e.Category));

		if (filter.PropertyId is { } propertyId)
			entries = entries.Where(e => e.PropertyId == propertyId);
		if (filter.From is { } from)
			entries = entries.Where(e => e.OccurredAt >= from);
		if (filter.To is { } to)
			entries = entries.Where(e => e.OccurredAt < to);
		if (filter.ActionCodes.Count > 0)
			entries = entries.Where(e => filter.ActionCodes.Contains(e.ActionCode));
		if (filter.ActorUserId is { } actorUserId)
			entries = entries.Where(e => e.ActorUserId == actorUserId);

		if (filter.Search is { } search)
		{
			var pattern = LikePattern.Contains(search);
			entries = entries.Where(e =>
				EF.Functions.ILike(e.ActorNameSnapshot ?? "", pattern)
				|| EF.Functions.ILike(e.BookingNumberSnapshot ?? "", pattern)
				|| EF.Functions.ILike(e.SpotLabelSnapshot ?? "", pattern));
		}

		return entries;
	}

	private static IQueryable<AuditTrailEntry> Sort(IQueryable<AuditTrailEntry> entries, bool descending)
		=> descending
			? entries.OrderByDescending(e => e.OccurredAt).ThenByDescending(e => e.Id)
			: entries.OrderBy(e => e.OccurredAt).ThenBy(e => e.Id);
}
