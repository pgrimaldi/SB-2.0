using Shared.Contracts;
using Shared.Localization;

namespace Module.Catalog.Domain.Entities;

/// <summary>Nome di un pacchetto o articolo aggiuntivo in un'altra lingua.</summary>
public sealed class PriceListTranslation : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public long PriceListId { get; private set; }
	public AppLanguage Language { get; private set; }
	public string Name { get; private set; } = null!;
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private PriceListTranslation() { }

	internal static PriceListTranslation Create(AppLanguage language, string name) => new() { Language = language, Name = name.Trim() };

	internal void Rename(string name) => Name = name.Trim();
}
