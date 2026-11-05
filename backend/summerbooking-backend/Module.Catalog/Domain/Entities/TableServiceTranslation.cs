using Shared.Contracts;
using Shared.Localization;

namespace Module.Catalog.Domain.Entities;

/// <summary>Nome e descrizione di un servizio tavoli in un'altra lingua.</summary>
public sealed class TableServiceTranslation : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public long TableServiceId { get; private set; }
	public AppLanguage Language { get; private set; }
	public string Name { get; private set; } = null!;
	public string? Description { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private TableServiceTranslation() { }

	internal static TableServiceTranslation Create(AppLanguage language, string name, string? description)
		=> new() { Language = language, Name = name.Trim(), Description = description };

	internal void Update(string name, string? description)
	{
		Name = name.Trim();
		Description = description;
	}
}
