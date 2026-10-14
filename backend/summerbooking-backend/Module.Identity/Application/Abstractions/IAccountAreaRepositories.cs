using Module.Identity.Domain.Entities;

namespace Module.Identity.Application.Abstractions;

public interface IBathhouseLikeRepository
{
	Task<BathhouseLike?> FindAsync(long userId, Guid bathhouseId, CancellationToken ct = default);
	Task<(IReadOnlyList<BathhouseLike> Items, int Total)> ListByUserAsync(long userId, int page, int pageSize, CancellationToken ct = default);
	Task<IReadOnlyList<BathhouseLike>> ListAllByUserAsync(long userId, CancellationToken ct = default);
	void Add(BathhouseLike like);
	void Remove(BathhouseLike like);
}

public interface ISubscriptionRepository
{
	/// <summary>Abbonamento con piano e periodi.</summary>
	Task<Subscription?> GetByPropertyAsync(Guid propertyId, CancellationToken ct = default);
	void Add(Subscription subscription);
}

public interface ILicensePlanRepository
{
	Task<IReadOnlyList<LicensePlan>> ListAsync(bool onlyActive, CancellationToken ct = default);
	Task<LicensePlan?> GetByCodeAsync(string code, CancellationToken ct = default);
	void Add(LicensePlan plan);
}

public interface IContractRepository
{
	Task<IReadOnlyList<ContractDocument>> ListDocumentsAsync(CancellationToken ct = default);
	Task<ContractDocument?> GetDocumentAsync(Guid publicId, CancellationToken ct = default);
	Task<ContractDocument?> GetPublishedAsync(CancellationToken ct = default);
	Task<bool> VersionExistsAsync(string versionCode, CancellationToken ct = default);
	Task<ContractAcceptanceEvent?> GetLastEventAsync(Guid propertyId, long documentId, CancellationToken ct = default);
	void AddDocument(ContractDocument document);
	void AddEvent(ContractAcceptanceEvent acceptanceEvent);
}
