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
	public short Quantity { get; private set; }
	public decimal UnitPrice { get; private set; }
	public decimal LineTotal { get; private set; }

	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	public Reservation Reservation { get; private set; } = null!;
	public BookingLine? BookingLine { get; private set; }

	private BookingLineAdditionalItem() { }
}
