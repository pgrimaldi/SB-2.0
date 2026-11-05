using Module.Catalog.Domain.Enums;
using Shared.Contracts;

namespace Module.Catalog.Domain.Entities;

/// <summary>
/// Voce di listino di uno stabilimento: un pacchetto (Kind = Package) o un articolo aggiuntivo (Kind =
/// Additional). Radice di consistenza della gerarchia pricing (items, prezzi per settore/periodo/giorno/
/// data, finestre ridotte, prezzi suggeriti).
/// </summary>
public sealed class PriceList : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public Guid PublicId { get; private set; }
	public long PropertyId { get; private set; }
	public Property? Property { get; private set; }
	public PriceListKind Kind { get; private set; }
	public string Name { get; private set; } = null!;

	public short? MaxPeople { get; private set; }
	/// <summary>Pacchetti: numero massimo di articoli aggiuntivi.</summary>
	public short? MaxAdditionalItems { get; private set; }
	public decimal? VatRate { get; private set; }
	public VatNature? VatNature { get; private set; }
	/// <summary>Valido solo nelle file/settori con un prezzo in <see cref="SectorPrices"/> («Aree abilitate»).</summary>
	public bool IsSectorRestricted { get; private set; }
	/// <summary>Articoli aggiuntivi: «assegnabile su mappa».</summary>
	public bool IsMapAssignable { get; private set; }
	public short? CalculationMethodId { get; private set; }
	public CalculationMethod? CalculationMethod { get; private set; }
	public MonthlySetting? MonthlySetting { get; private set; }
	public WeekendSetting? WeekendSetting { get; private set; }
	public SalesChannel SalesChannel { get; private set; }
	public bool IsDefault { get; private set; }
	public bool IsDefaultOnline { get; private set; }
	public bool IsAreaPackage { get; private set; }
	public bool IsCumulative { get; private set; }
	public bool AllowsPublicHolidays { get; private set; }
	public Guid? CreatedByUserId { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	public ICollection<PriceListItem> Items { get; private set; } = new List<PriceListItem>();
	public ICollection<PriceListSectorPrice> SectorPrices { get; private set; } = new List<PriceListSectorPrice>();
	public ICollection<PriceListPeriodPrice> PeriodPrices { get; private set; } = new List<PriceListPeriodPrice>();
	public ICollection<PriceListDatePrice> DatePrices { get; private set; } = new List<PriceListDatePrice>();
	public ICollection<PriceListReducedPrice> ReducedPrices { get; private set; } = new List<PriceListReducedPrice>();
	public ICollection<PriceListTranslation> Translations { get; private set; } = new List<PriceListTranslation>();

	public const int NameMaxLength = 255;

	private PriceList() { }

	public static PriceList Create(long propertyId, PriceListKind kind, Guid? createdBy) => new()
	{
		PublicId = Guid.CreateVersion7(),
		PropertyId = propertyId,
		Kind = kind,
		CreatedByUserId = createdBy,
	};

	public void Update(PriceListData d)
	{
		if (d.VatRate is null == d.VatNature is null)
			throw new ArgumentException("Un'aliquota oppure la natura dell'esenzione.", nameof(d));
		Name = d.Name.Trim();
		MaxPeople = d.MaxPeople;
		MaxAdditionalItems = Kind == PriceListKind.Package ? d.MaxAdditionalItems : null;
		CalculationMethodId = d.CalculationMethodId;
		MonthlySetting = d.MonthlySetting;
		WeekendSetting = d.WeekendSetting;
		SalesChannel = d.SalesChannel;
		IsAreaPackage = Kind == PriceListKind.Package && d.IsAreaPackage;
		IsCumulative = d.IsCumulative;
		AllowsPublicHolidays = d.AllowsPublicHolidays;
		VatRate = d.VatRate;
		VatNature = d.VatNature;
		IsSectorRestricted = d.IsSectorRestricted;
		IsMapAssignable = Kind == PriceListKind.Additional && d.IsMapAssignable;
	}

	/// <summary>Pacchetto predefinito (gestionale e online): uno per tipo di listino, gli altri si spengono altrove.</summary>
	public void SetDefaults(bool isDefault, bool isDefaultOnline)
	{
		IsDefault = isDefault;
		IsDefaultOnline = isDefaultOnline;
	}

	public void SetTranslations(IReadOnlyDictionary<Shared.Localization.AppLanguage, string> translations)
	{
		foreach (var removed in Translations.Where(t => !translations.ContainsKey(t.Language)).ToList())
			Translations.Remove(removed);
		foreach (var (language, name) in translations)
		{
			var current = Translations.FirstOrDefault(t => t.Language == language);
			if (current is null)
				Translations.Add(PriceListTranslation.Create(language, name));
			else
				current.Rename(name);
		}
	}

	/// <summary>Composizione: articoli di magazzino, servizi inclusi, pacchetti a cui l'aggiuntivo è legato.</summary>
	public void ReplaceItems(IEnumerable<PriceListItemData> items)
	{
		Items.Clear();
		foreach (var item in items)
			Items.Add(PriceListItem.Create(item));
	}

	/// <summary>Prezzo stagionale e stagionale mezza giornata per fila; le file senza riga non sono abilitate.</summary>
	public void SetSectorPrices(IReadOnlyDictionary<long, (decimal? Seasonal, decimal? SeasonalHalfDay)> prices)
	{
		foreach (var removed in SectorPrices.Where(p => !prices.ContainsKey(p.SectorId)).ToList())
			SectorPrices.Remove(removed);
		foreach (var (sectorId, (seasonal, halfDay)) in prices)
		{
			var current = SectorPrices.FirstOrDefault(p => p.SectorId == sectorId);
			if (current is null)
				SectorPrices.Add(PriceListSectorPrice.Create(sectorId, seasonal, halfDay));
			else
				current.SetPrices(seasonal, halfDay);
		}
	}

	/// <summary>Tariffe per fila e periodo, aggiornate sul posto: la coppia (fila, periodo) è unica.</summary>
	public void SetPeriodPrices(IReadOnlyList<PeriodPriceData> prices)
	{
		var keys = prices.Select(p => (p.SectorId, p.PeriodGroup)).ToHashSet();
		foreach (var removed in PeriodPrices.Where(p => !keys.Contains((p.SectorId, p.TariffPeriodGroup))).ToList())
			PeriodPrices.Remove(removed);
		foreach (var data in prices)
		{
			var current = PeriodPrices.FirstOrDefault(p => p.SectorId == data.SectorId && p.TariffPeriodGroup == data.PeriodGroup);
			if (current is null)
			{
				current = PriceListPeriodPrice.Create(data.SectorId, data.PeriodGroup);
				PeriodPrices.Add(current);
			}
			current.SetPrices(data);
		}
	}

	public void SetReducedPrices(IReadOnlyDictionary<(long ReducedSeasonId, long SectorId), decimal> prices)
	{
		foreach (var removed in ReducedPrices.Where(p => !prices.ContainsKey((p.ReducedSeasonId, p.SectorId))).ToList())
			ReducedPrices.Remove(removed);
		foreach (var ((seasonId, sectorId), price) in prices)
		{
			var current = ReducedPrices.FirstOrDefault(p => p.ReducedSeasonId == seasonId && p.SectorId == sectorId);
			if (current is null)
				ReducedPrices.Add(PriceListReducedPrice.Create(seasonId, sectorId, price));
			else
				current.SetPrice(price);
		}
	}

	/// <summary>Festivi del pacchetto («Anche festivi»): prezzo come nel weekend.</summary>
	public void SetHolidays(IReadOnlyCollection<DateOnly> dates)
	{
		foreach (var removed in DatePrices.Where(d => d.Kind == DatePriceKind.Holiday && !dates.Contains(d.PriceDate)).ToList())
			DatePrices.Remove(removed);
		foreach (var date in dates.Where(d => DatePrices.All(x => x.Kind != DatePriceKind.Holiday || x.PriceDate != d)))
			DatePrices.Add(PriceListDatePrice.Holiday(date));
	}
}
