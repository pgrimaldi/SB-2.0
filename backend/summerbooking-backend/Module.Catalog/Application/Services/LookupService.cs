using Module.Catalog.Application.Abstractions;
using Module.Catalog.Application.Dtos;
using Module.Catalog.Domain;
using Module.Catalog.Domain.Enums;
using Shared.Contracts.Errors;
using Shared.Localization;
using Shared.Utils;

namespace Module.Catalog.Application.Services;

/// <summary>Elenchi di piattaforma, tradotti nella lingua della richiesta.</summary>
public sealed class LookupService
{
	public const string DefaultCountryCode = "IT";

	private readonly ILookupRepository _lookups;
	private readonly CatalogMedia _media;
	private readonly IRequestLanguage _language;
	private readonly CurrentProperty _property;

	public LookupService(ILookupRepository lookups, CatalogMedia media, IRequestLanguage language, CurrentProperty property)
	{
		_lookups = lookups;
		_media = media;
		_language = language;
		_property = property;
	}

	public async Task<IReadOnlyList<CountryDto>> ListCountriesAsync(CancellationToken ct = default)
		=> (await _lookups.ListCountriesAsync(ct))
			.Select(c => new CountryDto(c.Code, Translations.Pick(c.Translations, t => t.Culture, t => t.Name, _language.Language, c.Code)))
			.ToList();

	public async Task<IReadOnlyList<AmenityCategoryDto>> ListAmenityCategoriesAsync(CancellationToken ct = default)
	{
		var categories = await _lookups.ListAmenityCategoriesAsync(ct);
		var amenities = await _lookups.ListAmenitiesAsync(ct);
		var urls = await _media.UrlsAsync(amenities.Select(a => a.IconAssetId), ct);

		return categories
			.Select(c => new AmenityCategoryDto(c.Code, amenities
				.Where(a => a.CategoryId == c.Id)
				.Select(a => new AmenityDto(a.Code, Translations.Pick(a.Translations, t => t.Culture, t => t.Name, _language.Language, a.Name), urls.UrlOf(a.IconAssetId)))
				.ToList()))
			.ToList();
	}

	/// <param name="type">bar, normal, hourly; null = tutti.</param>
	public async Task<IReadOnlyList<ServiceIconDto>> ListServiceIconsAsync(string? type, CancellationToken ct = default)
	{
		ServiceSaleKind? kind = null;
		if (!string.IsNullOrEmpty(type))
			kind = Codes.TryParse<ServiceSaleKind>(type, out var parsed) ? parsed : throw DomainException.BadRequest(ValidationErrorCodes.Invalid);

		var icons = await _lookups.ListServiceIconsAsync(kind, ct);
		var urls = await _media.UrlsAsync(icons.Select(i => (long?)i.IconAssetId), ct);
		return icons.Select(i => new ServiceIconDto(i.PublicId, Codes.Of(i.Type), i.Name, urls.UrlOf(i.IconAssetId))).ToList();
	}

	public async Task<IReadOnlyList<string>> ListCalculationMethodsAsync(CancellationToken ct = default)
		=> (await _lookups.ListCalculationMethodsAsync(ct)).Select(m => m.Code).ToList();

	/// <summary>Aliquote del Paese dello stabilimento del token.</summary>
	public async Task<VatOptionsDto> VatOptionsAsync(CancellationToken ct = default)
	{
		var country = await _lookups.PropertyCountryCodeAsync(await _property.IdAsync(ct), ct) ?? DefaultCountryCode;
		var rates = await _lookups.ListVatRatesAsync(country, ct);
		if (rates.Count == 0)
			rates = await _lookups.ListVatRatesAsync(DefaultCountryCode, ct);
		return new VatOptionsDto(country, rates, Enum.GetValues<VatNature>().Select(n => Codes.Of(n)).ToList(), Codes.Of(VatRules.DefaultExemptNature));
	}
}
