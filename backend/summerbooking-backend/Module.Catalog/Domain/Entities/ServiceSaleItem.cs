using Module.Catalog.Domain.Enums;
using Shared.Contracts;
using Shared.Localization;

namespace Module.Catalog.Domain.Entities;

/// <summary>
/// Servizio (bar/normale/a fascia oraria) venduto da uno stabilimento a corredo della prenotazione.
/// Aggregate root del cluster "servizi venduti": varianti, traduzioni, opzioni di prenotazione, prezzi per periodo,
/// file/settori e fasce orarie si scrivono sempre insieme al servizio.
/// </summary>
public sealed class ServiceSaleItem : IAuditable, ISoftDeletable
{
	public const int NameMaxLength = 255;
	public const int MaxVariants = 50;
	public const int MaxBookingOptions = 20;

	public long Id { get; private set; }
	public Guid PublicId { get; private set; }
	public long PropertyId { get; private set; }
	public ServiceSaleKind Kind { get; private set; }
	public string Name { get; private set; } = null!;
	public string? Description { get; private set; }
	public BarCategory? Category { get; private set; }
	public decimal Price { get; private set; }
	/// <summary>Aliquota IVA in percentuale; null con <see cref="VatNature"/> = esente.</summary>
	public decimal? VatRate { get; private set; }
	public VatNature? VatNature { get; private set; }
	public int? IconId { get; private set; }
	public long? IconAssetId { get; private set; }
	/// <summary>«Abilita acquisto online».</summary>
	public bool IsPurchasable { get; private set; }
	/// <summary>«Metti in evidenza»: proposto al cliente quando prenota la spiaggia.</summary>
	public bool IsHighlighted { get; private set; }
	/// <summary>«Abilita variazioni di prezzo per periodo»: valgono i prezzi di <see cref="PeriodPrices"/>.</summary>
	public bool HasPeriodPrices { get; private set; }
	/// <summary>Acquistabile solo nelle file/settori di <see cref="Sectors"/>.</summary>
	public bool IsSectorRestricted { get; private set; }
	public Allergen[] Allergens { get; private set; } = [];
	/// <summary>Servizi orari: «Fasce orarie fisse» (le fasce di <see cref="TimeSlots"/> con inizio e fine).</summary>
	public bool HasFixedTimeSlots { get; private set; }

	/// <summary>
	/// Giorni della settimana in cui il servizio e' attivo.
	/// Sostituisce catalog_service_sale_item_weekdays: sette righe figlie con un flag booleano per
	/// rappresentare un insieme di al piu' sette valori, sempre letto e scritto per intero insieme
	/// al padre, senza FK in ingresso e senza dato di audit per riga. Dal 2026-10-25 giorni e orari si
	/// impostano nelle opzioni di prenotazione.
	/// </summary>
	public Weekday[] ActiveWeekdays { get; private set; } = [];
	public TimeOnly? HourFrom { get; private set; }
	public TimeOnly? HourTo { get; private set; }
	public Guid? CreatedByUserId { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	public ICollection<ServiceSaleItemTimeSlot> TimeSlots { get; private set; } = new List<ServiceSaleItemTimeSlot>();
	public ICollection<ServiceHourlyOption> HourlyOptions { get; private set; } = new List<ServiceHourlyOption>();
	public ICollection<ServiceSaleItemVariant> Variants { get; private set; } = new List<ServiceSaleItemVariant>();
	public ICollection<ServiceSaleItemTranslation> Translations { get; private set; } = new List<ServiceSaleItemTranslation>();
	public ICollection<ServiceBookingOption> BookingOptions { get; private set; } = new List<ServiceBookingOption>();
	public ICollection<ServiceSaleItemPeriodPrice> PeriodPrices { get; private set; } = new List<ServiceSaleItemPeriodPrice>();
	public ICollection<ServiceSaleItemSector> Sectors { get; private set; } = new List<ServiceSaleItemSector>();

	private ServiceSaleItem() { }

	public static ServiceSaleItem Create(long propertyId, ServiceSaleKind kind, Guid? createdBy) => new()
	{
		PublicId = Guid.CreateVersion7(),
		PropertyId = propertyId,
		Kind = kind,
		CreatedByUserId = createdBy,
	};

	public void Update(ServiceSaleItemData d)
	{
		if (d.VatRate is null == d.VatNature is null)
			throw new ArgumentException("Un'aliquota oppure la natura dell'esenzione.", nameof(d));
		if (d.Category is not null && Kind != ServiceSaleKind.Bar)
			throw new ArgumentException("La categoria vale solo per i servizi bar.", nameof(d));

		Name = d.Name.Trim();
		Description = d.Description;
		Category = d.Category;
		Price = d.Price;
		VatRate = d.VatRate;
		VatNature = d.VatNature;
		IconId = d.IconId;
		IconAssetId = d.IconAssetId;
		IsPurchasable = d.IsPurchasable;
		IsHighlighted = d.IsHighlighted;
		HasPeriodPrices = d.HasPeriodPrices;
		IsSectorRestricted = d.IsSectorRestricted;
		Allergens = Kind == ServiceSaleKind.Bar ? d.Allergens.Distinct().Order().ToArray() : [];
	}

	/// <summary>Varianti (solo bar), nell'ordine ricevuto.</summary>
	public void ReplaceVariants(IReadOnlyList<(string Name, decimal Price)> variants)
	{
		Variants.Clear();
		if (Kind != ServiceSaleKind.Bar)
			return;
		for (var i = 0; i < variants.Count; i++)
			Variants.Add(ServiceSaleItemVariant.Create(variants[i].Name, variants[i].Price, (short)i));
	}

	/// <summary>Traduzioni per lingua: aggiornate sul posto, tolte quelle non più presenti.</summary>
	public void SetTranslations(IReadOnlyDictionary<AppLanguage, (string Name, string? Description)> translations)
	{
		foreach (var removed in Translations.Where(t => !translations.ContainsKey(t.Language)).ToList())
			Translations.Remove(removed);
		foreach (var (language, (name, description)) in translations)
		{
			var current = Translations.FirstOrDefault(t => t.Language == language);
			if (current is null)
				Translations.Add(ServiceSaleItemTranslation.Create(language, name, description));
			else
				current.Update(name, description);
		}
	}

	/// <summary>Opzioni di prenotazione; per i servizi orari ognuna può portare la sua disponibilità (vedi <see cref="ReplaceHourly"/>).</summary>
	public void ReplaceBookingOptions(IReadOnlyList<ServiceBookingOptionData> options)
	{
		BookingOptions.Clear();
		for (var i = 0; i < options.Count; i++)
			BookingOptions.Add(ServiceBookingOption.Create(options[i], (short)i));
	}

	public void SetPeriodPrices(IReadOnlyDictionary<short, decimal> prices)
	{
		foreach (var removed in PeriodPrices.Where(p => !prices.ContainsKey(p.TariffPeriodGroup)).ToList())
			PeriodPrices.Remove(removed);
		foreach (var (period, price) in prices)
		{
			var current = PeriodPrices.FirstOrDefault(p => p.TariffPeriodGroup == period);
			if (current is null)
				PeriodPrices.Add(ServiceSaleItemPeriodPrice.Create(period, price));
			else
				current.SetPrice(price);
		}
	}

	public void SetSectors(IReadOnlyCollection<long> sectorIds)
	{
		foreach (var removed in Sectors.Where(s => !sectorIds.Contains(s.SectorId)).ToList())
			Sectors.Remove(removed);
		foreach (var added in sectorIds.Where(id => Sectors.All(s => s.SectorId != id)))
			Sectors.Add(ServiceSaleItemSector.Create(added));
	}

	/// <summary>Servizi orari: durate prenotabili con il loro prezzo e, con le fasce fisse, inizio e fine.</summary>
	/// <summary>
	/// Disponibilità e prezzi di un servizio orario: quelle dei prezzi (per periodo tariffario, o con periodo null) e quelle
	/// delle opzioni di prenotazione, da chiamare dopo <see cref="ReplaceBookingOptions"/>. Le durate si ricreano.
	/// </summary>
	public void ReplaceHourly(IReadOnlyList<(short? PeriodKey, HourlyOptionData Option)> prices, IReadOnlyList<ServiceBookingOptionData> bookingOptions)
	{
		TimeSlots.Clear();
		HourlyOptions.Clear();
		if (Kind != ServiceSaleKind.Hourly)
			return;
		short order = 0;
		foreach (var (periodKey, option) in prices)
			AddHourly(option, periodKey, null, order++);
		var booking = BookingOptions.OrderBy(o => o.SortOrder).ToList();
		for (var i = 0; i < bookingOptions.Count && i < booking.Count; i++)
			if (bookingOptions[i].Hourly is { } option)
				AddHourly(option, null, booking[i], order++);
		HasFixedTimeSlots = HourlyOptions.Any(o => o.HasFixedTimeSlots);
	}

	private void AddHourly(HourlyOptionData data, short? periodKey, ServiceBookingOption? bookingOption, short sortOrder)
	{
		var option = ServiceHourlyOption.Create(data, periodKey, bookingOption, sortOrder);
		HourlyOptions.Add(option);
		var slots = data.Slots.Select(s => (Slot: s, IsCustom: false)).ToList();
		if (data.Custom is { } custom)
			slots.Add((custom, true));
		foreach (var (slot, isCustom) in slots)
		{
			var row = ServiceSaleItemTimeSlot.Create(slot, isCustom, (short)TimeSlots.Count);
			option.Slots.Add(row);
			TimeSlots.Add(row);
		}
	}
}

public sealed record ServiceSaleItemData(
	string Name, string? Description, BarCategory? Category, decimal Price, decimal? VatRate, VatNature? VatNature, int? IconId,
	long? IconAssetId, bool IsPurchasable, bool IsHighlighted, bool HasPeriodPrices, bool IsSectorRestricted,
	IReadOnlyList<Allergen> Allergens);
