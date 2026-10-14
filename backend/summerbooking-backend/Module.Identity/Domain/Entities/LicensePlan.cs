using Shared.Contracts;

namespace Module.Identity.Domain.Entities;

/// <summary>
/// Catalogo (lookup) dei piani di abbonamento della piattaforma, gestibile a runtime dal SuperAdmin.
/// </summary>
public sealed class LicensePlan : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public string Code { get; private set; } = null!;
	public short DurationMonths { get; private set; }
	public bool IsActive { get; private set; }
	public short SortOrder { get; private set; }
	public ICollection<LicensePlanTranslation> Translations { get; private set; } = new List<LicensePlanTranslation>();
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private LicensePlan() { }

	public static LicensePlan Create(string code, short durationMonths, short sortOrder) => new()
	{
		Code = code.Trim().ToLowerInvariant(),
		DurationMonths = durationMonths,
		SortOrder = sortOrder,
		IsActive = true,
	};

	public void Update(short durationMonths, short sortOrder, bool isActive)
	{
		DurationMonths = durationMonths;
		SortOrder = sortOrder;
		IsActive = isActive;
	}

	/// <summary>Aggiunge o aggiorna l'etichetta di una lingua.</summary>
	public void SetTranslation(string culture, string label, string? description)
	{
		var existing = Translations.FirstOrDefault(t => t.Culture == culture);
		if (existing is null)
			Translations.Add(LicensePlanTranslation.Create(this, culture, label, description));
		else
			existing.Update(label, description);
	}
}
