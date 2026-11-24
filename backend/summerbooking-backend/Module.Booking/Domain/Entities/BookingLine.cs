using Module.Booking.Domain.Enums;
using Shared.Contracts;

namespace Module.Booking.Domain.Entities;

/// <summary>
/// Riga dell'aggregato Prenotazione: una postazione per un intervallo di date contiguo e una fascia oraria.
/// Fila, colonna e tipo della postazione sono fotografati per ricollegare la riga dopo «Reimposta mappa».
/// </summary>
public sealed class BookingLine : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public Guid PublicId { get; private set; }
	public long ReservationId { get; private set; }
	public string BookingLineNumber { get; private set; } = null!;
	public string BookingNumber { get; private set; } = null!;
	public Guid PropertyId { get; private set; }
	public Guid? ResourceId { get; private set; }
	public string? SpotLabelSnapshot { get; private set; }
	public string? SpotTypeSnapshot { get; private set; }
	public short? SpotRowSnapshot { get; private set; }
	public short? SpotColumnSnapshot { get; private set; }
	public Guid? SectorId { get; private set; }
	public string? SectorLabelSnapshot { get; private set; }
	public Guid? PriceListId { get; private set; }
	public string? PackageNameSnapshot { get; private set; }
	/// <summary>Prezzo del pacchetto scritto a mano: cambiando le date si perde e vale di nuovo il listino.</summary>
	public decimal? ManualPrice { get; private set; }
	public DateOnly StartDate { get; private set; }
	public DateOnly EndDate { get; private set; }
	public DayPeriod BookingPeriod { get; private set; }
	public short Quantity { get; private set; }
	public decimal LinePrice { get; private set; }
	public bool IsSplit { get; private set; }
	public bool IsArea { get; private set; }
	public Guid? AreaResourceId { get; private set; }
	public short? AreaSeatCount { get; private set; }

	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	public Reservation Reservation { get; private set; } = null!;
	public ICollection<BookingLineAdditionalItem> AdditionalItems { get; private set; } = new List<BookingLineAdditionalItem>();
	public ICollection<BookingServiceLine> ServiceLines { get; private set; } = new List<BookingServiceLine>();
	public ICollection<BookingEntrance> Entrances { get; private set; } = new List<BookingEntrance>();

	private BookingLine() { }

	/// <summary>Riga di una postazione; per un'area la postazione è l'area e la quantità sono i posti occupati.</summary>
	public static BookingLine Create(
		Reservation reservation, int ordinal, SpotSnapshot spot, DateOnly from, DateOnly to, DayPeriod period,
		Guid? priceListId, string? packageName, decimal? manualPrice, short seats)
	{
		if (to < from)
			throw new ArgumentOutOfRangeException(nameof(to));
		if (seats < 1)
			throw new ArgumentOutOfRangeException(nameof(seats));
		var line = new BookingLine
		{
			PublicId = Guid.CreateVersion7(),
			Reservation = reservation,
			BookingNumber = reservation.BookingNumber,
			BookingLineNumber = $"{reservation.BookingNumber}-{ordinal}",
			PropertyId = reservation.PropertyId,
			StartDate = from,
			EndDate = to,
			BookingPeriod = period,
			PriceListId = priceListId,
			PackageNameSnapshot = packageName,
			ManualPrice = manualPrice,
		};
		line.PlaceOn(spot, seats);
		return line;
	}

	public int Days => EndDate.DayNumber - StartDate.DayNumber + 1;

	public bool Covers(DateOnly day) => day >= StartDate && day <= EndDate;

	public IEnumerable<DateOnly> EachDay()
	{
		for (var d = StartDate; d <= EndDate; d = d.AddDays(1))
			yield return d;
	}

	/// <summary>Sposta la riga su un'altra postazione (stesse date).</summary>
	public void PlaceOn(SpotSnapshot spot, short seats)
	{
		ResourceId = spot.IsArea ? null : spot.Id;
		AreaResourceId = spot.IsArea ? spot.Id : null;
		IsArea = spot.IsArea;
		AreaSeatCount = spot.IsArea ? seats : null;
		Quantity = spot.IsArea ? seats : (short)1;
		SpotLabelSnapshot = spot.Label;
		SpotTypeSnapshot = spot.SpotType;
		SpotRowSnapshot = spot.Row;
		SpotColumnSnapshot = spot.Column;
		SectorId = spot.SectorId;
		SectorLabelSnapshot = spot.SectorName;
	}

	/// <summary>Nuove date: il prezzo del pacchetto scritto a mano si perde (vale di nuovo il listino).</summary>
	public void Reschedule(DateOnly from, DateOnly to, DayPeriod period)
	{
		if (to < from)
			throw new ArgumentOutOfRangeException(nameof(to));
		if (from != StartDate || to != EndDate)
			ManualPrice = null;
		StartDate = from;
		EndDate = to;
		BookingPeriod = period;
	}

	/// <summary>Accorcia la riga a <paramref name="to"/> senza toccare il prezzo scritto a mano (taglio per spostamento).</summary>
	public void TruncateTo(DateOnly to)
	{
		if (to < StartDate || to > EndDate)
			throw new ArgumentOutOfRangeException(nameof(to));
		EndDate = to;
		IsSplit = true;
	}

	public void ChangePackage(Guid? priceListId, string? packageName, decimal? manualPrice)
	{
		PriceListId = priceListId;
		PackageNameSnapshot = packageName;
		ManualPrice = manualPrice;
	}

	public void MarkSplit() => IsSplit = true;

	/// <summary>Ricollegata a una postazione nuova dopo «Reimposta mappa»: stesse date, stessi posti.</summary>
	public void Relink(SpotSnapshot spot) => PlaceOn(spot, Quantity);

	public void SetPrice(decimal amount) => LinePrice = amount;

	/// <summary>Postazione della riga, prenotabile o area.</summary>
	public Guid? SpotId => IsArea ? AreaResourceId : ResourceId;
}

/// <summary>Dati della postazione al momento della prenotazione.</summary>
public sealed record SpotSnapshot(
	Guid Id, string? Label, string SpotType, bool IsArea, Guid? SectorId, string? SectorName, short? Row, short? Column);
