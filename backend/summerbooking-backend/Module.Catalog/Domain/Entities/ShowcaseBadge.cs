using Shared.Contracts;
using Shared.Localization;

namespace Module.Catalog.Domain.Entities;

/// <summary>Badge della vetrina creato dal systemadmin e assegnato alle strutture.</summary>
public sealed class ShowcaseBadge : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public Guid PublicId { get; private set; }
	public string Name { get; private set; } = null!;
	/// <summary>Colore esadecimale (#RRGGBB).</summary>
	public string? Color { get; private set; }
	public short SortOrder { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	public ICollection<ShowcaseBadgeTranslation> Translations { get; private set; } = new List<ShowcaseBadgeTranslation>();

	public const int NameMaxLength = 80;

	private ShowcaseBadge() { }

	public static ShowcaseBadge Create() => new() { PublicId = Guid.CreateVersion7() };

	public void Update(string name, string? color, short sortOrder)
	{
		Name = name.Trim();
		Color = string.IsNullOrWhiteSpace(color) ? null : color.Trim().ToUpperInvariant();
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
				Translations.Add(ShowcaseBadgeTranslation.Create(language, name));
			else
				current.Rename(name);
		}
	}
}

public sealed class ShowcaseBadgeTranslation : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public long BadgeId { get; private set; }
	public AppLanguage Language { get; private set; }
	public string Name { get; private set; } = null!;
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private ShowcaseBadgeTranslation() { }

	internal static ShowcaseBadgeTranslation Create(AppLanguage language, string name) => new() { Language = language, Name = name.Trim() };

	internal void Rename(string name) => Name = name.Trim();
}

/// <summary>Badge assegnato a una struttura.</summary>
public sealed class BathhouseBadge : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public long BathhouseId { get; private set; }
	public long BadgeId { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private BathhouseBadge() { }

	internal static BathhouseBadge Create(long badgeId) => new() { BadgeId = badgeId };
}
