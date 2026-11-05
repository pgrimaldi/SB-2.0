namespace Module.Catalog.Application.Dtos;

/// <summary>Card di una struttura nella home e nei risultati di ricerca.</summary>
/// <param name="FromPrice">Prezzo «da»: per le date cercate, altrimenti di oggi (fuori stagione, la stagione in arrivo).</param>
/// <param name="DistanceKm">Dalla località cercata, se indicata e se la struttura ha le coordinate.</param>
/// <param name="IsContactOnly">Struttura importata: solo contatti, niente prenotazione.</param>
public sealed record PublicCardDto(
	Guid Id, string Name, string? Slug, string? City, string? Province, string? Region, double? Latitude, double? Longitude, double? DistanceKm,
	decimal? FromPrice, int LikeCount, string? MainPhotoUrl, string? Description, IReadOnlyList<PublicBadgeDto> Badges, IReadOnlyList<string> AmenityCodes,
	bool IsOnlineBookable, bool IsContactOnly);

public sealed record PublicBadgeDto(Guid Id, string Name, string? Color);

public sealed record PublicRegionDto(Guid Id, string Name, int Count);

/// <summary>Home: «In evidenza», «Più frequentati» (scelti dal systemadmin) e regioni con il numero di strutture.</summary>
public sealed record PublicHomeDto(IReadOnlyList<PublicCardDto> Featured, IReadOnlyList<PublicCardDto> Popular, IReadOnlyList<PublicRegionDto> Regions);

/// <param name="Text">Nome, città, provincia o regione.</param>
/// <param name="Latitude">Località cercata (con <paramref name="Longitude"/>): distanza e ordinamento per distanza.</param>
/// <param name="Amenities">Codici delle dotazioni separati da virgola: tutte richieste.</param>
/// <param name="SortBy">popularity (predefinito: «mi piace», poi anzianità dell'abbonamento), price, distance, name.</param>
public sealed record PublicSearchQuery(
	string? Text, Guid? RegionId, double? Latitude, double? Longitude, DateOnly? From, DateOnly? To, string? Amenities, bool OnlineOnly = false,
	string? SortBy = null, string? SortDir = null, int Page = 1, int PageSize = 20);

/// <param name="AmenityCounts">Per ogni dotazione, le strutture fra i risultati che la hanno.</param>
public sealed record PublicSearchResultDto(int Total, int Page, int PageSize, IReadOnlyList<PublicCardDto> Items, IReadOnlyDictionary<string, int> AmenityCounts);

public sealed record PublicPlaceDto(string City, string? Province, string? Region, double? Latitude, double? Longitude, int Count);

public sealed record PublicSuggestionsDto(IReadOnlyList<PublicPlaceDto> Places, IReadOnlyList<PublicCardDto> Bathhouses);

/// <summary>Scheda pubblica. I contatti sono quelli della struttura; il titolare non compare mai.</summary>
/// <param name="Online">Regole della prenotazione online; null se la struttura non è prenotabile online.</param>
public sealed record PublicBathhouseDto(
	Guid Id, PropertyInfoDto Info, string? Region, IReadOnlyList<PublicBadgeDto> Badges, int LikeCount, decimal? FromPrice,
	bool IsOnlineBookable, bool IsContactOnly, PublicOnlineRulesDto? Online);

/// <param name="PaymentMethods">Metodi di pagamento online abilitati.</param>
public sealed record PublicOnlineRulesDto(
	bool AllowsHalfDay, TimeOnly? MorningFrom, TimeOnly? MorningTo, TimeOnly? AfternoonFrom, TimeOnly? AfternoonTo, bool AllowsSeasonal,
	bool AllowsSameDayBooking, short? BookingHorizonDays, bool RequiresManualConfirmation, bool IsOnlinePaymentBlocked,
	IReadOnlyList<string> PaymentMethods, string? GoodToKnow, string? UsefulInfo);

/// <summary>Mappa pubblica. La disponibilità delle postazioni è di Booking.</summary>
/// <param name="OnlineSpotIds">Postazioni prenotabili online; null = tutte.</param>
/// <param name="Legend">Legenda della mappa online scelta dal gestore.</param>
public sealed record PublicMapDto(MapDto Map, IReadOnlyList<Guid>? OnlineSpotIds, IReadOnlyList<LegendItemDto> Legend);

/// <param name="Kind">bar, normal, hourly.</param>
public sealed record PublicServiceDto(
	Guid Id, string Kind, string Name, string? Description, string? Category, decimal Price, string? ImageUrl, bool IsHighlighted,
	IReadOnlyList<string> Allergens, IReadOnlyList<PublicVariantDto> Variants);

public sealed record PublicVariantDto(string Name, decimal Price);

public sealed record PublicTableServiceDto(Guid Id, string Name, string? Description, bool IsHighlighted, string DepositMode);

public sealed record PublicServicesDto(IReadOnlyList<PublicServiceDto> Services, IReadOnlyList<PublicTableServiceDto> Tables);

/// <param name="FromPrice">Il più basso per le date indicate (o di oggi), mezza giornata se c'è.</param>
public sealed record PublicPackageDto(Guid Id, string Name, short? MaxPeople, bool IsDefault, decimal? FromPrice);

/// <summary>Banner sconto della pagina pubblica: una visualizzazione per fascia della giornata (10–20), al più le volte scelte dal gestore.</summary>
/// <param name="DiscountPercent">Sconto di oggi scelto dal gestore nel prezzo suggerito, se c'è.</param>
/// <param name="SpotIds">Postazioni a cui è limitato lo sconto; vuoto = tutte.</param>
public sealed record PublicDiscountBannerDto(bool IsVisible, decimal? DiscountPercent, IReadOnlyList<Guid> SpotIds);
