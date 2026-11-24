using Module.Booking.Domain.Enums;
using Shared.Contracts;

namespace Module.Booking.Domain.Entities;

/// <summary>
/// Articolo aggiuntivo (lettino extra, cabina, tassa di soggiorno, ...) acquistato su una riga o sull'intera prenotazione.
/// </summary>
public sealed class BookingLineAdditionalItem : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public long ReservationId { get; private set; }
	public long? BookingLineId { get; private set; }
	public Guid PropertyId { get; private set; }
	public AdditionalItemSource ItemSource { get; private set; }
	public Guid? CatalogItemId { get; private set; }
	public string NameSnapshot { get; private set; } = null!;
	public AdditionalItemDateScope DateScope { get; private set; }
	public List<DateOnly> Dates { get; private set; } = new();
	public short Quantity { get; private set; }
	public decimal UnitPrice { get; private set; }
	public decimal LineTotal { get; private set; }

	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	public Reservation Reservation { get; private set; } = null!;
	public BookingLine? BookingLine { get; private set; }

	private BookingLineAdditionalItem() { }

	/// <summary>Articolo aggiuntivo di listino sulla riga; senza date vale per tutto il soggiorno della riga.</summary>
	public static BookingLineAdditionalItem FromPriceList(
		Reservation reservation, BookingLine line, Guid priceListId, string name, short quantity, IReadOnlyList<DateOnly>? dates, decimal lineTotal)
	{
		if (quantity < 1)
			throw new ArgumentOutOfRangeException(nameof(quantity));
		var days = (dates ?? []).Distinct().Order().ToList();
		return new BookingLineAdditionalItem
		{
			Reservation = reservation,
			BookingLine = line,
			PropertyId = reservation.PropertyId,
			ItemSource = Enums.AdditionalItemSource.PriceList,
			CatalogItemId = priceListId,
			NameSnapshot = name,
			DateScope = days.Count == 0 ? Enums.AdditionalItemDateScope.WholeStay : Enums.AdditionalItemDateScope.SpecificDates,
			Dates = days,
			Quantity = quantity,
			LineTotal = lineTotal,
			UnitPrice = decimal.Round(lineTotal / quantity, 2, MidpointRounding.AwayFromZero),
		};
	}
}
