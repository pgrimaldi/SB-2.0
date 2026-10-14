using Shared.Contracts;

namespace Module.Identity.Domain.Entities;

/// <summary>
/// Etichetta multilingua (nome commerciale + descrizione) di un piano di abbonamento.
/// </summary>
public sealed class LicensePlanTranslation : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public long LicensePlanId { get; private set; }
	public LicensePlan LicensePlan { get; private set; } = null!;
	public string Culture { get; private set; } = null!;
	public string Label { get; private set; } = null!;
	public string? Description { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private LicensePlanTranslation() { }

	internal static LicensePlanTranslation Create(LicensePlan plan, string culture, string label, string? description) => new()
	{
		LicensePlan = plan,
		Culture = culture,
		Label = label,
		Description = description,
	};

	internal void Update(string label, string? description)
	{
		Label = label;
		Description = description;
	}
}
