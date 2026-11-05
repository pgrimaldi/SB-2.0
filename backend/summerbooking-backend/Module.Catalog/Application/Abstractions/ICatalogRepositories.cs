using Module.Catalog.Domain.Entities;

namespace Module.Catalog.Application.Abstractions;

public interface IUnitOfWork
{
	Task SaveChangesAsync(CancellationToken ct = default);
}

public interface IPropertyRepository
{
	/// <summary>Id interno dello stabilimento attivo con quel public_id, oppure null.</summary>
	Task<long?> FindIdAsync(Guid publicId, CancellationToken ct = default);
}

/// <summary>Filtro della lista del magazzino, già validato; lo stabilimento viene dal token.</summary>
public sealed record InventoryItemFilter(long PropertyId, string? Search, string SortBy, bool Descending);

/// <summary>Listino che usa un articolo: impedisce di eliminarlo.</summary>
public sealed record InventoryItemUsage(string PriceListName, string? AgreementPartnerName);

public interface IInventoryItemRepository
{
	Task<(IReadOnlyList<InventoryItem> Items, int Total)> ListAsync(InventoryItemFilter filter, int page, int pageSize, CancellationToken ct = default);

	Task<IReadOnlyList<InventoryItem>> ListAllAsync(long propertyId, CancellationToken ct = default);

	Task<InventoryItem?> FindAsync(long propertyId, Guid publicId, CancellationToken ct = default);

	Task<IReadOnlyList<InventoryItem>> FindManyAsync(long propertyId, IReadOnlyCollection<Guid> publicIds, CancellationToken ct = default);

	/// <summary>Nome già usato da un altro articolo dello stabilimento, senza distinguere maiuscole e minuscole.</summary>
	Task<bool> NameExistsAsync(long propertyId, string name, long? exceptId, CancellationToken ct = default);

	/// <summary>Pacchetti e articoli aggiuntivi (anche delle convenzioni) che includono l'articolo.</summary>
	Task<IReadOnlyList<InventoryItemUsage>> ListUsagesAsync(long inventoryItemId, CancellationToken ct = default);

	void Add(InventoryItem item);

	void Remove(InventoryItem item);
}

public interface ISeasonRepository
{
	/// <summary>Stagione dello stabilimento con i suoi periodi (tracciata, per le modifiche).</summary>
	Task<PricingSeason?> FindAsync(long propertyId, CancellationToken ct = default);

	void Add(PricingSeason season);

	/// <summary>Sostituisce gli intervalli di un periodo in una transazione (prima rimuove, poi inserisce).</summary>
	Task ReplacePeriodAsync(PricingSeason season, short groupKey, IReadOnlyList<TariffPeriod> ranges, CancellationToken ct = default);

	/// <summary>Listini dello stabilimento (anche delle convenzioni) che hanno prezzi per il periodo.</summary>
	Task<IReadOnlyList<string>> ListPeriodUsagesAsync(long propertyId, short groupKey, string periodName, CancellationToken ct = default);
}

/// <summary>Quante configurazioni usano un settore: impediscono di eliminarlo.</summary>
public sealed record SectorUsage(int MapSpots, int PriceLists, int StayLimits, int Discounts)
{
	public bool Any => MapSpots + PriceLists + StayLimits + Discounts > 0;
}

public interface ISectorRepository
{
	Task<IReadOnlyList<MapSector>> ListAsync(long propertyId, CancellationToken ct = default);

	Task<MapSector?> FindAsync(long propertyId, Guid publicId, CancellationToken ct = default);

	Task<bool> NameExistsAsync(long propertyId, string name, long? exceptId, CancellationToken ct = default);

	Task<short> NextSortOrderAsync(long propertyId, CancellationToken ct = default);

	Task<SectorUsage> CountUsagesAsync(long sectorId, CancellationToken ct = default);

	void Add(MapSector sector);

	void Remove(MapSector sector);
}
