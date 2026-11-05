using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Module.Catalog.Application.Abstractions;
using Module.Catalog.Application.Dtos;
using Module.Catalog.Domain.Entities;
using Module.Catalog.Domain.Enums;

namespace Module.Catalog.Infrastructure.Repositories;

public sealed class AgreementRepository : IAgreementRepository
{
	private readonly DbContext _db;

	public AgreementRepository(DbContext db) => _db = db;

	public async Task<(IReadOnlyList<AgreementPartner> Items, int Total)> ListPartnersAsync(AgreementPartnerFilter filter, int page, int pageSize, CancellationToken ct = default)
	{
		var partners = _db.AgreementPartners.AsNoTracking().Where(p => p.PropertyId == filter.PropertyId);
		if (filter.Search is { } search)
			partners = partners.Where(p => EF.Functions.ILike(p.Name, LikePattern.Contains(search)) || p.City != null && EF.Functions.ILike(p.City, LikePattern.Contains(search)));
		var total = await partners.CountAsync(ct);
		var sorted = (filter.SortBy, filter.Descending) switch
		{
			(AgreementPartnerSortFields.City, false) => partners.OrderBy(p => p.City),
			(AgreementPartnerSortFields.City, true) => partners.OrderByDescending(p => p.City),
			(_, true) => partners.OrderByDescending(p => p.Name),
			_ => partners.OrderBy(p => p.Name),
		};
		return (await sorted.ThenBy(p => p.Id).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct), total);
	}

	public async Task<IReadOnlyList<AgreementPartner>> ListAllPartnersAsync(long propertyId, CancellationToken ct = default)
		=> await _db.AgreementPartners.AsNoTracking().Where(p => p.PropertyId == propertyId).OrderBy(p => p.Name).ToListAsync(ct);

	public Task<AgreementPartner?> FindPartnerAsync(long propertyId, Guid publicId, CancellationToken ct = default)
		=> _db.AgreementPartners.FirstOrDefaultAsync(p => p.PropertyId == propertyId && p.PublicId == publicId, ct);

	public async Task<IReadOnlyList<AgreementPartner>> FindPartnersAsync(long propertyId, IReadOnlyCollection<Guid> publicIds, CancellationToken ct = default)
		=> await _db.AgreementPartners.Where(p => p.PropertyId == propertyId && publicIds.Contains(p.PublicId)).ToListAsync(ct);

	public async Task<IReadOnlyDictionary<long, IReadOnlySet<AgreementListType>>> ListTypesAsync(IReadOnlyCollection<long> partnerIds, CancellationToken ct = default)
		=> (await _db.AgreementPriceLists.AsNoTracking().Where(l => partnerIds.Contains(l.PartnerId)).Select(l => new { l.PartnerId, l.ListType }).Distinct().ToListAsync(ct))
			.GroupBy(x => x.PartnerId).ToDictionary(g => g.Key, g => (IReadOnlySet<AgreementListType>)g.Select(x => x.ListType).ToHashSet());

	public void AddPartner(AgreementPartner partner) => _db.AgreementPartners.Add(partner);

	public void RemovePartner(AgreementPartner partner) => _db.AgreementPartners.Remove(partner);

	public async Task<IReadOnlyList<AgreementPriceList>> ListPriceListsAsync(long partnerId, AgreementListType? listType, PriceListKind? kind, CancellationToken ct = default)
		=> await Full().Where(l => l.PartnerId == partnerId && (listType == null || l.ListType == listType) && (kind == null || l.Kind == kind))
			.OrderBy(l => l.ListType).ThenBy(l => l.Kind).ThenBy(l => l.Name).ToListAsync(ct);

	public async Task<IReadOnlyDictionary<long, IReadOnlyList<string>>> ListPriceListNamesAsync(IReadOnlyCollection<long> partnerIds, CancellationToken ct = default)
		=> (await _db.AgreementPriceLists.AsNoTracking().Where(l => partnerIds.Contains(l.PartnerId)).Select(l => new { l.PartnerId, l.Name }).ToListAsync(ct))
			.GroupBy(l => l.PartnerId).ToDictionary(g => g.Key, g => (IReadOnlyList<string>)g.Select(l => l.Name).ToList());

	public Task<AgreementPriceList?> FindPriceListAsync(long propertyId, Guid publicId, CancellationToken ct = default)
		=> Full().FirstOrDefaultAsync(l => l.PublicId == publicId && l.Partner!.PropertyId == propertyId, ct);

	public async Task<IReadOnlyList<AgreementPriceList>> FindPriceListsAsync(long propertyId, IReadOnlyCollection<Guid> publicIds, CancellationToken ct = default)
		=> await Full().Where(l => publicIds.Contains(l.PublicId) && l.Partner!.PropertyId == propertyId).ToListAsync(ct);

	public async Task<IReadOnlyDictionary<Guid, long>> FindPackageIdsAsync(long partnerId, AgreementListType listType, IReadOnlyCollection<Guid> publicIds, CancellationToken ct = default)
		=> publicIds.Count == 0
			? new Dictionary<Guid, long>()
			: await _db.AgreementPriceLists.AsNoTracking()
				.Where(l => l.PartnerId == partnerId && l.ListType == listType && l.Kind == PriceListKind.Package && publicIds.Contains(l.PublicId))
				.ToDictionaryAsync(l => l.PublicId, l => l.Id, ct);

	public async Task<IReadOnlyDictionary<long, Guid>> FindPriceListPublicIdsAsync(IReadOnlyCollection<long> ids, CancellationToken ct = default)
		=> ids.Count == 0
			? new Dictionary<long, Guid>()
			: await _db.AgreementPriceLists.AsNoTracking().Where(l => ids.Contains(l.Id)).ToDictionaryAsync(l => l.Id, l => l.PublicId, ct);

	public async Task<IReadOnlyList<AgreementPriceList>> ListLinkedAdditionalsAsync(long packageId, CancellationToken ct = default)
		=> await _db.AgreementPriceLists.Include(l => l.Items)
			.Where(l => l.Items.Any(i => i.ItemKind == PackageItemKind.LinkedPackage && i.LinkedPriceListId == packageId)).ToListAsync(ct);

	public void AddPriceList(AgreementPriceList priceList) => _db.AgreementPriceLists.Add(priceList);

	public void RemovePriceList(AgreementPriceList priceList) => _db.AgreementPriceLists.Remove(priceList);

	public async Task<(IReadOnlyList<AgreementReport> Items, int Total)> ListReportsAsync(
		long propertyId, long? partnerId, AgreementReportStatus? status, int page, int pageSize, CancellationToken ct = default)
	{
		var reports = _db.AgreementReports.AsNoTracking().Include(r => r.Partner)
			.Where(r => r.Partner!.PropertyId == propertyId && (partnerId == null || r.PartnerId == partnerId) && (status == null || r.Status == status));
		var total = await reports.CountAsync(ct);
		return (await reports.OrderByDescending(r => r.PeriodStart).ThenByDescending(r => r.Id).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct), total);
	}

	public Task<AgreementReport?> FindReportAsync(long propertyId, Guid publicId, CancellationToken ct = default)
		=> _db.AgreementReports.Include(r => r.Partner).FirstOrDefaultAsync(r => r.PublicId == publicId && r.Partner!.PropertyId == propertyId, ct);

	private IQueryable<AgreementPriceList> Full()
		=> _db.AgreementPriceLists
			.Include(l => l.Partner)
			.Include(l => l.Items).Include(l => l.SectorPrices)
			.Include(l => l.PeriodPrices).ThenInclude(p => p.StayPriceOverrides)
			.Include(l => l.DatePrices)
			.AsSplitQuery();
}
