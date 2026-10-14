using Module.Booking.Domain.Enums;
using Shared.Contracts;

namespace Module.Booking.Domain.Entities;

/// <summary>
/// Riga dell'aggregato Prenotazione: una postazione per un intervallo di date contiguo e una fascia oraria.
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
	public Guid? SectorId { get; private set; }
	public string? SectorLabelSnapshot { get; private set; }
	public Guid? PriceListId { get; private set; }
	public string? PackageNameSnapshot { get; private set; }
	public DateOnly StartDate { get; private set; }
	public DateOnly EndDate { get; private set; }
	public DayPeriod BookingPeriod { get; private set; }
	public short Quantity { get; private set; }
	public decimal LinePrice { get; private set; }
	public bool IsSplit { get; private set; }
	public bool IsArea { get; private set; }
	public Guid? AreaResourceId { get; private set; }
	public short? AreaSeatCount { get; private set; }
	public DateTime? CheckedInAt { get; private set; }
	public DateTime? CheckedOutAt { get; private set; }
	public DateTime? CheckoutEffectiveAt { get; private set; }

	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	public Reservation Reservation { get; private set; } = null!;
	public ICollection<BookingLineAdditionalItem> AdditionalItems { get; private set; } = new List<BookingLineAdditionalItem>();
	public ICollection<BookingServiceLine> ServiceLines { get; private set; } = new List<BookingServiceLine>();

	private BookingLine() { }
}
