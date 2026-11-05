using Module.Catalog.Domain.Entities;
using Module.Catalog.Domain.Enums;

namespace Module.Catalog.Application.Abstractions;

public sealed record AgreementPartnerFilter(long PropertyId, string? Search, string? SortBy, bool Descending);

public interface IAgreementRepository
{
	Task<(IReadOnlyList<AgreementPartner> Items, int Total)> ListPartnersAsync(AgreementPartnerFilter filter, int page, int pageSize, CancellationToken ct = default);
	Task<IReadOnlyList<AgreementPartner>> ListAllPartnersAsync(long propertyId, CancellationToken ct = default);
	Task<AgreementPartner?> FindPartnerAsync(long propertyId, Guid publicId, CancellationToken ct = default);
	Task<IReadOnlyList<AgreementPartner>> FindPartnersAsync(long propertyId, IReadOnlyCollection<Guid> publicIds, CancellationToken ct = default);
	/// <summary>Tipi di listino (regular, voucher) presenti per ogni struttura.</summary>
	Task<IReadOnlyDictionary<long, IReadOnlySet<AgreementListType>>> ListTypesAsync(IReadOnlyCollection<long> partnerIds, CancellationToken ct = default);
	void AddPartner(AgreementPartner partner);
	void RemovePartner(AgreementPartner partner);

	Task<IReadOnlyList<AgreementPriceList>> ListPriceListsAsync(long partnerId, AgreementListType? listType, PriceListKind? kind, CancellationToken ct = default);
	/// <summary>Nomi dei listini e voucher delle strutture indicate, per struttura.</summary>
	Task<IReadOnlyDictionary<long, IReadOnlyList<string>>> ListPriceListNamesAsync(IReadOnlyCollection<long> partnerIds, CancellationToken ct = default);
	/// <summary>Listino con composizione, prezzi e festivi; lo stabilimento si verifica dalla struttura.</summary>
	Task<AgreementPriceList?> FindPriceListAsync(long propertyId, Guid publicId, CancellationToken ct = default);
	Task<IReadOnlyList<AgreementPriceList>> FindPriceListsAsync(long propertyId, IReadOnlyCollection<Guid> publicIds, CancellationToken ct = default);
	Task<IReadOnlyDictionary<Guid, long>> FindPackageIdsAsync(long partnerId, AgreementListType listType, IReadOnlyCollection<Guid> publicIds, CancellationToken ct = default);
	Task<IReadOnlyDictionary<long, Guid>> FindPriceListPublicIdsAsync(IReadOnlyCollection<long> ids, CancellationToken ct = default);
	Task<IReadOnlyList<AgreementPriceList>> ListLinkedAdditionalsAsync(long packageId, CancellationToken ct = default);
	void AddPriceList(AgreementPriceList priceList);
	void RemovePriceList(AgreementPriceList priceList);

	Task<(IReadOnlyList<AgreementReport> Items, int Total)> ListReportsAsync(long propertyId, long? partnerId, AgreementReportStatus? status, int page, int pageSize, CancellationToken ct = default);
	Task<AgreementReport?> FindReportAsync(long propertyId, Guid publicId, CancellationToken ct = default);
}
