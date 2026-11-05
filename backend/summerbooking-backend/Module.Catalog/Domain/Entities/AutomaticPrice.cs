using Module.Catalog.Domain.Enums;
using Shared.Contracts;
using Shared.Localization;

namespace Module.Catalog.Domain.Entities;

/// <summary>
/// Prezzo automatico (es. «Coperto»): si aggiunge da solo agli ordini dei servizi indicati, nei periodi, giorni e fasce
/// orarie di validità; una volta per ordine oppure per ogni servizio.
/// </summary>
public sealed class AutomaticPrice : IAuditable, ISoftDeletable
{
	public const int NameMaxLength = 255;

	public long Id { get; private set; }
	public Guid PublicId { get; private set; }
	public long PropertyId { get; private set; }
	public string Name { get; private set; } = null!;
	public string? Description { get; private set; }
	public decimal Price { get; private set; }
	public decimal? VatRate { get; private set; }
	public VatNature? VatNature { get; private set; }
	public int? IconId { get; private set; }
	public long? IconAssetId { get; private set; }
	public bool IsPurchasable { get; private set; }
	/// <summary>true = si applica a ogni servizio dell'ordine; false = una volta per ordine.</summary>
	public bool AppliesPerService { get; private set; }
	/// <summary>true = vale per tutti i servizi; false = solo per quelli di <see cref="Services"/>.</summary>
	public bool AppliesToAllServices { get; private set; }
	public bool IsSectorRestricted { get; private set; }
	/// <summary>Giorni di validità; vuoto = tutti.</summary>
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	public ICollection<AutomaticPriceTranslation> Translations { get; private set; } = new List<AutomaticPriceTranslation>();
	public ICollection<AutomaticPriceService> Services { get; private set; } = new List<AutomaticPriceService>();
	public ICollection<AutomaticPricePeriod> Periods { get; private set; } = new List<AutomaticPricePeriod>();
	public ICollection<AutomaticPriceSector> Sectors { get; private set; } = new List<AutomaticPriceSector>();

	private AutomaticPrice() { }

	public static AutomaticPrice Create(long propertyId) => new() { PublicId = Guid.CreateVersion7(), PropertyId = propertyId };

	public void Update(AutomaticPriceData d)
	{
		if (d.VatRate is null == d.VatNature is null)
			throw new ArgumentException("Un'aliquota oppure la natura dell'esenzione.", nameof(d));
		Name = d.Name.Trim();
		Description = d.Description;
		Price = d.Price;
		VatRate = d.VatRate;
		VatNature = d.VatNature;
		IconId = d.IconId;
		IconAssetId = d.IconAssetId;
		IsPurchasable = d.IsPurchasable;
		AppliesPerService = d.AppliesPerService;
		AppliesToAllServices = d.AppliesToAllServices;
		IsSectorRestricted = d.IsSectorRestricted;
	}

	public void SetTranslations(IReadOnlyDictionary<AppLanguage, (string Name, string? Description)> translations)
	{
		foreach (var removed in Translations.Where(t => !translations.ContainsKey(t.Language)).ToList())
			Translations.Remove(removed);
		foreach (var (language, (name, description)) in translations)
		{
			var current = Translations.FirstOrDefault(t => t.Language == language);
			if (current is null)
				Translations.Add(AutomaticPriceTranslation.Create(language, name, description));
			else
				current.Update(name, description);
		}
	}

	public void SetServices(IReadOnlyCollection<long> saleItemIds)
	{
		foreach (var removed in Services.Where(s => !saleItemIds.Contains(s.SaleItemId)).ToList())
			Services.Remove(removed);
		foreach (var added in saleItemIds.Where(id => Services.All(s => s.SaleItemId != id)))
			Services.Add(AutomaticPriceService.Create(added));
	}

	public void SetSectors(IReadOnlyCollection<long> sectorIds)
	{
		foreach (var removed in Sectors.Where(s => !sectorIds.Contains(s.SectorId)).ToList())
			Sectors.Remove(removed);
		foreach (var added in sectorIds.Where(id => Sectors.All(s => s.SectorId != id)))
			Sectors.Add(AutomaticPriceSector.Create(added));
	}

	/// <summary>Periodi di validità, ciascuno con i suoi giorni e le sue fasce orarie.</summary>
	public void ReplacePeriods(IEnumerable<AutomaticPricePeriodData> periods)
	{
		Periods.Clear();
		foreach (var period in periods)
			Periods.Add(AutomaticPricePeriod.Create(period));
	}
}

public sealed record AutomaticPriceData(
	string Name, string? Description, decimal Price, decimal? VatRate, VatNature? VatNature, int? IconId, long? IconAssetId,
	bool IsPurchasable, bool AppliesPerService, bool AppliesToAllServices, bool IsSectorRestricted);
