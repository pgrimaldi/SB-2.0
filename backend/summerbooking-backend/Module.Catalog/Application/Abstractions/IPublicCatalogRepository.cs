using Module.Catalog.Domain.Entities;

namespace Module.Catalog.Application.Abstractions;

/// <summary>Cosa ricalcolare nella proiezione di ricerca: stabilimenti, vetrine, listini, stagioni; tutto null = tutti.</summary>
public sealed record SearchScope(
	IReadOnlyCollection<long> PropertyIds, IReadOnlyCollection<long> BathhouseIds, IReadOnlyCollection<long> PriceListIds, IReadOnlyCollection<long> SeasonIds)
{
	public static readonly SearchScope All = new([], [], [], []) { IsAll = true };
	public bool IsAll { get; private init; }
	public bool IsEmpty => !IsAll && PropertyIds.Count + BathhouseIds.Count + PriceListIds.Count + SeasonIds.Count == 0;
}

/// <summary>Proiezione di ricerca pubblica (catalog.search_bathhouses), ricalcolata in SQL per i soli stabilimenti toccati.</summary>
public interface ISearchProjection
{
	Task<int> RefreshAsync(SearchScope scope, CancellationToken ct = default);
}

/// <param name="Text">Nome, città, provincia o regione dell'indirizzo.</param>
/// <param name="Near">Località cercata: ordina e calcola la distanza.</param>
public sealed record PublicSearchFilter(
	string? Text, long? RegionId, IReadOnlyCollection<int> AmenityIds, bool OnlineBookableOnly, (double Latitude, double Longitude)? Near);

public sealed record PublicSearchRow(SearchBathhouse Row, double? DistanceKm);

/// <summary>Località proposta mentre si digita: città con le strutture pubblicate e la loro posizione media.</summary>
public sealed record PlaceSuggestion(string City, string? Province, string? Region, double? Latitude, double? Longitude, int Count);

/// <summary>Letture del sito pubblico: solo strutture pubblicate.</summary>
public interface IPublicCatalogRepository
{
	/// <summary>Righe filtrate; l'ordinamento lo decide il servizio sulle colonne della proiezione.</summary>
	IQueryable<SearchBathhouse> Published(PublicSearchFilter filter);
	Task<(IReadOnlyList<PublicSearchRow> Items, int Total)> SearchAsync(PublicSearchFilter filter, string sortBy, bool descending, int page, int pageSize,
		IReadOnlyDictionary<long, decimal>? prices, CancellationToken ct = default);
	Task<IReadOnlyDictionary<int, int>> CountAmenitiesAsync(PublicSearchFilter filter, CancellationToken ct = default);
	Task<IReadOnlyList<long>> ListPropertyIdsAsync(PublicSearchFilter filter, CancellationToken ct = default);
	/// <summary>Prezzo «da»: il più basso fra i pacchetti online nei periodi dei giorni indicati (mezza giornata se c'è).</summary>
	Task<IReadOnlyDictionary<long, decimal>> MinPricesAsync(IReadOnlyCollection<long> propertyIds, DateOnly from, DateOnly to, CancellationToken ct = default);
	Task<IReadOnlyList<SearchBathhouse>> FindPublishedAsync(IReadOnlyCollection<Guid> bathhouseIds, CancellationToken ct = default);
	Task<SearchBathhouse?> FindPublishedBySlugAsync(string slug, CancellationToken ct = default);
	Task<IReadOnlyList<PlaceSuggestion>> SuggestPlacesAsync(string text, int limit, CancellationToken ct = default);
	Task<IReadOnlyList<SearchBathhouse>> SuggestBathhousesAsync(string text, int limit, CancellationToken ct = default);
	Task<IReadOnlyDictionary<long, int>> CountByRegionAsync(CancellationToken ct = default);
	Task<IReadOnlyDictionary<int, string>> AmenityCodesAsync(CancellationToken ct = default);
	Task<IReadOnlyDictionary<string, int>> AmenityIdsAsync(CancellationToken ct = default);

	/// <summary>Servizi acquistabili online, con traduzioni e varianti.</summary>
	Task<IReadOnlyList<ServiceSaleItem>> ListOnlineServicesAsync(long propertyId, CancellationToken ct = default);
	Task<IReadOnlyList<TableService>> ListOnlineTablesAsync(long propertyId, CancellationToken ct = default);
	/// <summary>Pacchetti vendibili online, con composizione e traduzioni.</summary>
	Task<IReadOnlyList<PriceList>> ListOnlinePackagesAsync(long propertyId, CancellationToken ct = default);

	/// <summary>Servizi con varianti, prezzi per periodo e fasce.</summary>
	Task<IReadOnlyList<ServiceSaleItem>> FindServicesAsync(long propertyId, IReadOnlyCollection<Guid> publicIds, CancellationToken ct = default);
	/// <summary>Sconto con periodi, file e fasce orarie; il codice senza distinguere maiuscole e minuscole.</summary>
	Task<Discount?> FindDiscountByCodeAsync(long propertyId, string code, CancellationToken ct = default);
	Task<IReadOnlyList<Discount>> ListAutomaticDiscountsAsync(long propertyId, CancellationToken ct = default);

	Task<RevenueSettings?> FindRevenueSettingsAsync(long propertyId, CancellationToken ct = default);
	/// <summary>Una visualizzazione in più, solo se il contatore è ancora sotto il limite indicato.</summary>
	Task<bool> CountBannerViewAsync(long propertyId, short limit, CancellationToken ct = default);
	Task<int> ResetBannerCountersAsync(CancellationToken ct = default);
	Task<SuggestedPrice?> FindAppliedDiscountAsync(long propertyId, DateOnly date, CancellationToken ct = default);
}
