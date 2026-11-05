using Shared.Contracts;
using Shared.Localization;

namespace Module.Catalog.Domain.Entities;

/// <summary>Regione della vetrina, gestita dal systemadmin; si assegna alle strutture dalla regione dell'indirizzo.</summary>
public sealed class ShowcaseRegion : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public Guid PublicId { get; private set; }
	public short? CountryId { get; private set; }
	public string Name { get; private set; } = null!;
	public short SortOrder { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	public ICollection<ShowcaseRegionTranslation> Translations { get; private set; } = new List<ShowcaseRegionTranslation>();

	public const int NameMaxLength = 120;

	private ShowcaseRegion() { }

	public static ShowcaseRegion Create() => new() { PublicId = Guid.CreateVersion7() };

	public void Update(short? countryId, string name, short sortOrder)
	{
		CountryId = countryId;
		Name = name.Trim();
		SortOrder = sortOrder;
	}

	public void SetTranslations(IReadOnlyDictionary<AppLanguage, string> translations)
	{
		foreach (var removed in Translations.Where(t => !translations.ContainsKey(t.Language)).ToList())
			Translations.Remove(removed);
		foreach (var (language, name) in translations)
		{
			var current = Translations.FirstOrDefault(t => t.Language == language);
			if (current is null)
				Translations.Add(ShowcaseRegionTranslation.Create(language, name));
			else
				current.Rename(name);
		}
	}

	/// <summary>Il testo dell'indirizzo indica questa regione: nome o traduzione, contando solo le lettere.</summary>
	public bool Matches(string? region)
	{
		var wanted = Normalize(region);
		return wanted.Length > 0 && (Normalize(Name) == wanted || Translations.Any(t => Normalize(t.Name) == wanted));
	}

	public static string Normalize(string? text)
		=> new((text ?? "").Where(char.IsLetter).Select(char.ToLowerInvariant).ToArray());
}

public sealed class ShowcaseRegionTranslation : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public long RegionId { get; private set; }
	public AppLanguage Language { get; private set; }
	public string Name { get; private set; } = null!;
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private ShowcaseRegionTranslation() { }

	internal static ShowcaseRegionTranslation Create(AppLanguage language, string name) => new() { Language = language, Name = name.Trim() };

	internal void Rename(string name) => Name = name.Trim();
}
