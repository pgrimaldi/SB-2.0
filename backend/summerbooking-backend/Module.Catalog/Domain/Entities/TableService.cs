using Module.Catalog.Domain.Enums;
using Shared.Contracts;
using Shared.Localization;

namespace Module.Catalog.Domain.Entities;

/// <summary>
/// Servizio tavoli (es. «Pranzo», «Cena»): disponibilità per periodo, giorni e fasce, capienza per numero di tavoli
/// (con i posti di ciascuno) o per posti totali, durata media del pasto, limiti per prenotazione e caparra. L'assegnazione
/// del tavolo e le prenotazioni sono di Booking, la caparra di Payment.
/// </summary>
public sealed class TableService : IAuditable, ISoftDeletable
{
	public const int NameMaxLength = 255;

	public long Id { get; private set; }
	public Guid PublicId { get; private set; }
	public long PropertyId { get; private set; }
	public string Name { get; private set; } = null!;
	public string? Description { get; private set; }
	public bool IsOnlineBookable { get; private set; }
	public bool IsHighlighted { get; private set; }
	public TableCapacityMode CapacityMode { get; private set; }
	/// <summary>Posti totali, con <see cref="TableCapacityMode.Seats"/>.</summary>
	public short? TotalSeats { get; private set; }
	/// <summary>Durata media del pasto: libera il tavolo per la fascia successiva.</summary>
	public short AverageMealMinutes { get; private set; }
	public short? MaxBookingsPerSlot { get; private set; }
	public short? MinSeatsPerBooking { get; private set; }
	public short? MaxSeatsPerBooking { get; private set; }
	public DepositMode DepositMode { get; private set; }
	public DepositType? DepositType { get; private set; }
	public decimal? DepositValue { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	public ICollection<TableServiceTranslation> Translations { get; private set; } = new List<TableServiceTranslation>();
	public ICollection<TableServiceTableType> TableTypes { get; private set; } = new List<TableServiceTableType>();
	public ICollection<TableServiceAvailability> Availabilities { get; private set; } = new List<TableServiceAvailability>();

	private TableService() { }

	public static TableService Create(long propertyId) => new() { PublicId = Guid.CreateVersion7(), PropertyId = propertyId };

	public void Update(TableServiceData d)
	{
		if (d.CapacityMode == TableCapacityMode.Seats && d.TotalSeats is null or < 1)
			throw new ArgumentException("Posti totali obbligatori.", nameof(d));
		if (d.DepositMode != DepositMode.None && (d.DepositType is null || d.DepositValue is null or <= 0))
			throw new ArgumentException("Caparra incompleta.", nameof(d));
		Name = d.Name.Trim();
		Description = d.Description;
		IsOnlineBookable = d.IsOnlineBookable;
		IsHighlighted = d.IsHighlighted;
		CapacityMode = d.CapacityMode;
		TotalSeats = d.CapacityMode == TableCapacityMode.Seats ? d.TotalSeats : null;
		AverageMealMinutes = d.AverageMealMinutes;
		MaxBookingsPerSlot = d.MaxBookingsPerSlot;
		MinSeatsPerBooking = d.MinSeatsPerBooking;
		MaxSeatsPerBooking = d.MaxSeatsPerBooking;
		DepositMode = d.DepositMode;
		DepositType = d.DepositMode == DepositMode.None ? null : d.DepositType;
		DepositValue = d.DepositMode == DepositMode.None ? null : d.DepositValue;
	}

	public void SetTranslations(IReadOnlyDictionary<AppLanguage, (string Name, string? Description)> translations)
	{
		foreach (var removed in Translations.Where(t => !translations.ContainsKey(t.Language)).ToList())
			Translations.Remove(removed);
		foreach (var (language, (name, description)) in translations)
		{
			var current = Translations.FirstOrDefault(t => t.Language == language);
			if (current is null)
				Translations.Add(TableServiceTranslation.Create(language, name, description));
			else
				current.Update(name, description);
		}
	}

	/// <summary>Tipi di tavolo (quanti e con quanti posti), solo con <see cref="TableCapacityMode.Tables"/>.</summary>
	public void ReplaceTableTypes(IEnumerable<(short Seats, short Count)> types)
	{
		TableTypes.Clear();
		if (CapacityMode != TableCapacityMode.Tables)
			return;
		foreach (var (seats, count) in types)
			TableTypes.Add(TableServiceTableType.Create(seats, count));
	}

	public void ReplaceAvailabilities(IEnumerable<TableServiceAvailabilityData> availabilities)
	{
		Availabilities.Clear();
		foreach (var a in availabilities)
			Availabilities.Add(TableServiceAvailability.Create(a));
	}
}

public sealed record TableServiceData(
	string Name, string? Description, bool IsOnlineBookable, bool IsHighlighted, TableCapacityMode CapacityMode, short? TotalSeats,
	short AverageMealMinutes, short? MaxBookingsPerSlot, short? MinSeatsPerBooking, short? MaxSeatsPerBooking, DepositMode DepositMode,
	DepositType? DepositType, decimal? DepositValue);

/// <param name="Slots">Fasce: <c>Weekday</c> null = stessa fascia per tutti i giorni.</param>
public sealed record TableServiceAvailabilityData(DateOnly StartsOn, DateOnly EndsOn, IReadOnlyList<(Weekday? Weekday, TimeOnly From, TimeOnly To)> Slots);
