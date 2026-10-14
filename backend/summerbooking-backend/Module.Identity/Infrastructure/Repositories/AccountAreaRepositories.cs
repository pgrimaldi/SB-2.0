using Microsoft.EntityFrameworkCore;
using Module.Identity.Application.Abstractions;
using Module.Identity.Domain.Entities;
using Module.Identity.Domain.Enums;

namespace Module.Identity.Infrastructure.Repositories;

public sealed class BathhouseLikeRepository : IBathhouseLikeRepository
{
	private readonly DbContext _db;

	public BathhouseLikeRepository(DbContext db) => _db = db;

	public Task<BathhouseLike?> FindAsync(long userId, Guid bathhouseId, CancellationToken ct = default)
		=> _db.BathhouseLikes.FirstOrDefaultAsync(l => l.UserId == userId && l.BathhouseId == bathhouseId, ct);

	public async Task<(IReadOnlyList<BathhouseLike> Items, int Total)> ListByUserAsync(long userId, int page, int pageSize, CancellationToken ct = default)
	{
		var likes = _db.BathhouseLikes.Where(l => l.UserId == userId);
		var total = await likes.CountAsync(ct);
		var items = await likes.OrderByDescending(l => l.LikedAt).ThenByDescending(l => l.Id)
			.Skip((page - 1) * pageSize).Take(pageSize).AsNoTracking().ToListAsync(ct);
		return (items, total);
	}

	public async Task<IReadOnlyList<BathhouseLike>> ListAllByUserAsync(long userId, CancellationToken ct = default)
		=> await _db.BathhouseLikes.Where(l => l.UserId == userId).ToListAsync(ct);

	public void Add(BathhouseLike like) => _db.BathhouseLikes.Add(like);

	public void Remove(BathhouseLike like) => _db.BathhouseLikes.Remove(like);
}

public sealed class SubscriptionRepository : ISubscriptionRepository
{
	private readonly DbContext _db;

	public SubscriptionRepository(DbContext db) => _db = db;

	public Task<Subscription?> GetByPropertyAsync(Guid propertyId, CancellationToken ct = default)
		=> _db.Subscriptions
			.Include(s => s.LicensePlan)
			.Include(s => s.Periods)
			.AsSplitQuery()
			.FirstOrDefaultAsync(s => s.PropertyId == propertyId, ct);

	public void Add(Subscription subscription) => _db.Subscriptions.Add(subscription);
}

public sealed class LicensePlanRepository : ILicensePlanRepository
{
	private readonly DbContext _db;

	public LicensePlanRepository(DbContext db) => _db = db;

	public async Task<IReadOnlyList<LicensePlan>> ListAsync(bool onlyActive, CancellationToken ct = default)
		=> await _db.LicensePlans
			.Include(p => p.Translations)
			.Where(p => !onlyActive || p.IsActive)
			.OrderBy(p => p.SortOrder).ThenBy(p => p.Code)
			.AsNoTracking()
			.ToListAsync(ct);

	public Task<LicensePlan?> GetByCodeAsync(string code, CancellationToken ct = default)
		=> _db.LicensePlans.Include(p => p.Translations).FirstOrDefaultAsync(p => p.Code == code, ct);

	public void Add(LicensePlan plan) => _db.LicensePlans.Add(plan);
}

public sealed class ContractRepository : IContractRepository
{
	private readonly DbContext _db;

	public ContractRepository(DbContext db) => _db = db;

	public async Task<IReadOnlyList<ContractDocument>> ListDocumentsAsync(CancellationToken ct = default)
		=> await _db.ContractDocuments.OrderByDescending(d => d.CreatedAt).AsNoTracking().ToListAsync(ct);

	public Task<ContractDocument?> GetDocumentAsync(Guid publicId, CancellationToken ct = default)
		=> _db.ContractDocuments.FirstOrDefaultAsync(d => d.PublicId == publicId, ct);

	public Task<ContractDocument?> GetPublishedAsync(CancellationToken ct = default)
		=> _db.ContractDocuments
			.Where(d => d.Status == ContractDocumentStatus.Published)
			.OrderByDescending(d => d.PublishedAt)
			.FirstOrDefaultAsync(ct);

	public Task<bool> VersionExistsAsync(string versionCode, CancellationToken ct = default)
		=> _db.ContractDocuments.AnyAsync(d => d.VersionCode == versionCode, ct);

	public Task<ContractAcceptanceEvent?> GetLastEventAsync(Guid propertyId, long documentId, CancellationToken ct = default)
		=> _db.ContractAcceptanceEvents
			.Where(e => e.PropertyId == propertyId && e.ContractDocumentId == documentId)
			.OrderByDescending(e => e.OccurredAt).ThenByDescending(e => e.Id)
			.FirstOrDefaultAsync(ct);

	public void AddDocument(ContractDocument document) => _db.ContractDocuments.Add(document);

	public void AddEvent(ContractAcceptanceEvent acceptanceEvent) => _db.ContractAcceptanceEvents.Add(acceptanceEvent);
}
