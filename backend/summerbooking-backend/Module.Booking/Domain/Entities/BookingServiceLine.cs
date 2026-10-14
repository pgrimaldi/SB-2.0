using Module.Booking.Domain.Enums;
using Shared.Contracts;

namespace Module.Booking.Domain.Entities;

/// <summary>
/// Riga d'ordine per un servizio (bar/normale/orario) venduto su una prenotazione, agganciata o meno a una postazione.
/// </summary>
public sealed class BookingServiceLine : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public Guid PublicId { get; private set; }
	public long ReservationId { get; private set; }
	public long? BookingLineId { get; private set; }
	public Guid PropertyId { get; private set; }
	public ServiceCategory Kind { get; private set; }
	public Guid? SaleItemId { get; private set; }
	public string NameSnapshot { get; private set; } = null!;
	public string? VariantLabel { get; private set; }
	public decimal VariantSurcharge { get; private set; }
	public decimal UnitPrice { get; private set; }
	public int Quantity { get; private set; }
	public decimal? VatRate { get; private set; }
	/// <remarks>Colonna generata (GENERATED ALWAYS ... STORED): di sola lettura, PostgreSQL rifiuta un valore esplicito.</remarks>
	public decimal LineTotal { get; private set; }
	public DateOnly? ServiceDate { get; private set; }
	public BookingChannel BookingChannel { get; private set; }
	public bool IsOrderFilled { get; private set; }
	public bool IsPaidWithWallet { get; private set; }

	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	public Reservation Reservation { get; private set; } = null!;
	public BookingLine? BookingLine { get; private set; }
	public ICollection<BookingServiceTimeSlot> TimeSlots { get; private set; } = new List<BookingServiceTimeSlot>();

	private BookingServiceLine() { }
}
