using Module.Catalog.Application.Abstractions;
using Module.Catalog.Application.Dtos;
using Module.Catalog.Domain;
using Module.Catalog.Domain.Entities;
using Module.Catalog.Domain.Enums;
using Shared.Contracts.Errors;
using Shared.Localization;
using Shared.Utils;

namespace Module.Catalog.Application.Services;

/// <summary>Aliquote ammesse per lo stabilimento: quelle del suo Paese.</summary>
public sealed class VatPolicy
{
	private readonly ILookupRepository _lookups;
	private IReadOnlyList<decimal>? _rates;

	public VatPolicy(ILookupRepository lookups) => _lookups = lookups;

	public async Task<(decimal? Rate, VatNature? Nature)> ResolveAsync(long propertyId, decimal? rate, string? nature, CancellationToken ct)
	{
		if (rate is null)
			return (null, Codes.TryParse<VatNature>(nature, out var n) ? n : VatRules.DefaultExemptNature);

		_rates ??= await RatesAsync(propertyId, ct);
		if (!_rates.Contains(rate.Value))
			throw DomainException.Unprocessable(CatalogErrors.VatRateNotAllowed, new Dictionary<string, object?> { ["rates"] = _rates });
		return (rate, null);
	}

	private async Task<IReadOnlyList<decimal>> RatesAsync(long propertyId, CancellationToken ct)
	{
		var country = await _lookups.PropertyCountryCodeAsync(propertyId, ct) ?? LookupService.DefaultCountryCode;
		var rates = await _lookups.ListVatRatesAsync(country, ct);
		return rates.Count > 0 ? rates : await _lookups.ListVatRatesAsync(LookupService.DefaultCountryCode, ct);
	}
}

internal static class ContentMapping
{
	public static IReadOnlyDictionary<AppLanguage, (string Name, string? Description)> Translations(IReadOnlyList<TranslationDto>? items)
		=> (items ?? []).ToDictionary(t => SupportedLanguages.ParseOrDefault(t.Language), t => (t.Name.Trim(), Clean(t.Description)));

	public static TranslationDto Dto(AppLanguage language, string name, string? description) => new(SupportedLanguages.Code(language), name, description);

	public static IReadOnlyList<Weekday> Weekdays(IReadOnlyList<string>? codes)
		=> (codes ?? []).Select(c => Codes.TryParse<Weekday>(c, out var d) ? d : default).Distinct().ToList();

	public static IReadOnlyList<string> CodesOf<T>(IEnumerable<T> values) where T : struct, Enum => values.Select(v => Codes.Of(v)).ToList();

	public static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

	/// <summary>File/settori dello stabilimento dagli id pubblici; uno sconosciuto dà 404.</summary>
	public static async Task<IReadOnlyList<long>> SectorIdsAsync(ISectorRepository sectors, long propertyId, IReadOnlyList<Guid>? publicIds, CancellationToken ct)
	{
		if (publicIds is not { Count: > 0 })
			return [];
		var all = (await sectors.ListAsync(propertyId, ct)).ToDictionary(s => s.PublicId, s => s.Id);
		return publicIds.Distinct().Select(id => all.TryGetValue(id, out var sectorId) ? sectorId : throw DomainException.NotFound(CatalogErrors.SectorNotFound)).ToList();
	}

	public static async Task<IReadOnlyDictionary<long, Guid>> SectorPublicIdsAsync(ISectorRepository sectors, long propertyId, CancellationToken ct)
		=> (await sectors.ListAsync(propertyId, ct)).ToDictionary(s => s.Id, s => s.PublicId);
}
