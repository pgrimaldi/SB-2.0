namespace Module.Catalog.Application.Dtos;

/// <param name="Status">pending, approved, rejected, imported, hidden.</param>
/// <param name="Trashed">true = il cestino.</param>
public sealed record AdminPropertyQuery(string? Status, string? Search, bool Trashed = false, int Page = 1, int PageSize = 50);

/// <summary>Stabilimento per il systemadmin, con i dati della vetrina se c'è.</summary>
/// <param name="AdminComment">Ultimo motivo di approvazione o rifiuto.</param>
/// <param name="DeletedAt">Spostato nel cestino.</param>
public sealed record AdminPropertyDto(
	Guid Id, string Name, string Status, string? AdminComment, string? City, string? ContactFullName, string? ContactEmail, string? ContactPhone,
	Guid? OwnerUserId, bool IsOnlineSalesSuspended, bool IsSubscriptionActive, Guid? BathhouseId, string? Slug, string? AddressRegion,
	Guid? RegionId, bool IsRegionManual, IReadOnlyList<Guid> BadgeIds, int LikeCount, DateTime CreatedAt, DateTime? DeletedAt);

/// <param name="Status">approved, rejected, pending, hidden.</param>
public sealed record ChangePropertyStatusRequest(string Status, string? Comment);

/// <param name="RegionId">null = torna all'assegnazione automatica dall'indirizzo.</param>
public sealed record SetPropertyRegionRequest(Guid? RegionId);

public sealed record SetPropertyBadgesRequest(IReadOnlyList<Guid> BadgeIds);

/// <param name="State">occupied, free.</param>
public sealed record AdminMapIconDto(Guid Id, string Category, string State, string Name, string? Url, bool IsActive, bool IsDefault);

/// <summary>Una sola icona predefinita per categoria e stato: la nuova predefinita sostituisce la precedente.</summary>
public sealed record UpdateMapIconRequest(string Name, bool IsActive, bool IsDefault);

/// <param name="UsageCount">Postazioni che lo usano sulle mappe.</param>
public sealed record AdminBeachElementDto(Guid Id, string Name, string? IconUrl, short SortOrder, bool IsActive, int UsageCount);

public sealed record UpdateBeachElementRequest(string Name, short SortOrder, bool IsActive);

/// <param name="Color">#RRGGBB.</param>
public sealed record BadgeDto(Guid Id, string Name, string? Color, short SortOrder, IReadOnlyList<NameTranslationDto> Translations);

public sealed record SaveBadgeRequest(string Name, string? Color, short SortOrder, IReadOnlyList<NameTranslationDto>? Translations);

/// <param name="CountryCode">ISO 3166-1 alpha-2.</param>
public sealed record RegionDto(Guid Id, string? CountryCode, string Name, short SortOrder, IReadOnlyList<NameTranslationDto> Translations);

public sealed record SaveRegionRequest(string? CountryCode, string Name, short SortOrder, IReadOnlyList<NameTranslationDto>? Translations);

/// <summary>Struttura in una sezione della vetrina, nell'ordine scelto.</summary>
public sealed record ShowcaseEntryDto(Guid PropertyId, Guid BathhouseId, string? Name, string? City);

/// <summary>Elenco completo e ordinato degli stabilimenti della sezione: sostituisce quello esistente.</summary>
public sealed record SaveShowcaseRequest(IReadOnlyList<Guid> PropertyIds);
