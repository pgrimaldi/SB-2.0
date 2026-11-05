namespace Module.Catalog.Application.Dtos;

/// <summary>Pagina «Informazioni stabilimento»: dati della scheda pubblica, foto, PDF e dotazioni.</summary>
/// <param name="Slug">Ultima parte dell'indirizzo della pagina pubblica.</param>
/// <param name="PetsPolicy">allowed, not_allowed, on_request.</param>
/// <param name="AmenityCodes">Dotazioni dichiarate (codici di <c>GET /lookups/amenity-categories</c>).</param>
public sealed record PropertyInfoDto(
	string? Name, string? Slug, string? Description, string? Regulation, string? Address, string? City, string? Cap,
	string? Province, string? Region, string? CountryCode, decimal? Latitude, decimal? Longitude, string? Phone, string? Email,
	string? Website, string? Facebook, string? Instagram, string? Whatsapp, TimeOnly? OpenHoursFrom, TimeOnly? OpenHoursTo,
	bool IsFreeCancellation, short? CancellationNoticeHours, string PetsPolicy, string? VirtualTourUrl,
	string? MainPhotoUrl, IReadOnlyList<PhotoDto> Photos, DocumentDto? MenuPdf, DocumentDto? PriceListPdf,
	IReadOnlyList<string> AmenityCodes);

public sealed record PhotoDto(Guid Id, string? Url, short SortOrder);

public sealed record DocumentDto(string FileName, string? Url);

/// <param name="Slug">null = generato dal nome alla prima pubblicazione, poi invariato.</param>
/// <param name="CancellationNoticeHours">Ore di preavviso, solo con la cancellazione gratuita.</param>
public sealed record SavePropertyInfoRequest(
	string Name, string? Slug, string? Description, string? Regulation, string? Address, string? City, string? Cap,
	string? Province, string? Region, string? CountryCode, decimal? Latitude, decimal? Longitude, string? Phone, string? Email,
	string? Website, string? Facebook, string? Instagram, string? Whatsapp, TimeOnly? OpenHoursFrom, TimeOnly? OpenHoursTo,
	bool IsFreeCancellation, short? CancellationNoticeHours, string PetsPolicy, string? VirtualTourUrl);

public sealed record SetAmenitiesRequest(IReadOnlyList<string> Codes);

/// <summary>Nuovo ordine della galleria: tutti gli id delle foto, nell'ordine voluto.</summary>
public sealed record ReorderPhotosRequest(IReadOnlyList<Guid> Ids);

/// <summary>Form pubblico «Registra la tua struttura».</summary>
/// <param name="VerificationToken">Token del widget anti-robot.</param>
/// <param name="VerificationToken">Soluzione della sfida anti-robot (GET /api/security/human-challenge).</param>
/// <param name="Website">Campo trappola: nascosto nel form, una persona lo lascia vuoto.</param>
public sealed record RegisterPropertyRequest(
	string Name, string City, string ContactFullName, string Email, string PhoneCountryCode, string Phone, string? VerificationToken,
	string? Website = null);

public sealed record PropertyRegisteredDto(Guid Id);
