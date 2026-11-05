using Module.Catalog.Domain.Entities;

namespace Module.Catalog.Application.Abstractions;

public interface IPropertyInfoRepository
{
	Task<Property?> FindPropertyAsync(long propertyId, CancellationToken ct = default);
	void AddProperty(Property property);

	/// <summary>Scheda pubblica dello stabilimento (la prima, se per errore ce n'è più d'una), con foto e dotazioni.</summary>
	Task<Bathhouse?> FindBathhouseAsync(long propertyId, CancellationToken ct = default);
	void AddBathhouse(Bathhouse bathhouse);
	Task<bool> SlugExistsAsync(string slug, long? exceptBathhouseId, CancellationToken ct = default);

	Task<short?> FindCountryIdAsync(string code, CancellationToken ct = default);
	Task<string?> FindCountryCodeAsync(short id, CancellationToken ct = default);
	Task<IReadOnlyDictionary<string, int>> FindAmenityIdsAsync(IReadOnlyCollection<string> codes, CancellationToken ct = default);
	Task<IReadOnlyList<string>> FindAmenityCodesAsync(IReadOnlyCollection<int> ids, CancellationToken ct = default);
}
