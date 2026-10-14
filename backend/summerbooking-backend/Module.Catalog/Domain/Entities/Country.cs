using Shared.Contracts;

namespace Module.Catalog.Domain.Entities;

/// <summary>
/// Elenco (non esaustivo) delle nazioni selezionabili nei form; lookup di dominio referenziata dai moduli
/// tramite il code ISO copiato (mai una FK cross-schema).
/// </summary>
public sealed class Country : IAuditable, ISoftDeletable
{
	public short Id { get; private set; }
	public string Code { get; private set; } = null!;
	public bool IsActive { get; private set; }
	public short SortOrder { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	public ICollection<CountryTranslation> Translations { get; private set; } = new List<CountryTranslation>();

	private Country() { }
}
