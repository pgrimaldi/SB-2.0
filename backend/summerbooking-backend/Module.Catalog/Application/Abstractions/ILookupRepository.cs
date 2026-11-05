using Module.Catalog.Domain.Entities;
using Module.Catalog.Domain.Enums;

namespace Module.Catalog.Application.Abstractions;

public interface ILookupRepository
{
	Task<IReadOnlyList<Country>> ListCountriesAsync(CancellationToken ct = default);
	Task<IReadOnlyList<AmenityCategory>> ListAmenityCategoriesAsync(CancellationToken ct = default);
	Task<IReadOnlyList<Amenity>> ListAmenitiesAsync(CancellationToken ct = default);
	Task<IReadOnlyList<ServiceTypeIcon>> ListServiceIconsAsync(ServiceSaleKind? type, CancellationToken ct = default);
	Task<IReadOnlyList<CalculationMethod>> ListCalculationMethodsAsync(CancellationToken ct = default);
	Task<IReadOnlyList<decimal>> ListVatRatesAsync(string countryCode, CancellationToken ct = default);

	/// <summary>Paese dello stabilimento, altrimenti quello della sua scheda pubblica.</summary>
	Task<string?> PropertyCountryCodeAsync(long propertyId, CancellationToken ct = default);
}
