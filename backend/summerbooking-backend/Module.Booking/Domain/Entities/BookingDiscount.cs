using Module.Booking.Domain.Enums;
using Shared.Contracts;

namespace Module.Booking.Domain.Entities;

/// <summary>
/// Snapshot di un voucher/sconto applicato a una prenotazione o a una sua riga.
/// </summary>
public sealed class BookingDiscount : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public long ReservationId { get; private set; }
	public long? BookingLineId { get; private set; }
	public Guid PropertyId { get; private set; }
	public Guid? DiscountId { get; private set; }
	public DiscountOrigin Origin { get; private set; }
	public string CodeSnapshot { get; private set; } = null!;
	public DiscountType DiscountType { get; private set; }
	public decimal Value { get; private set; }
	public DiscountScope Scope { get; private set; }
	public DiscountChannel AppliedChannel { get; private set; }
	public List<ServiceCategory> AppliesToCategories { get; private set; } = new();
	public bool IsRoundup { get; private set; }
	public decimal AppliedAmount { get; private set; }
	public bool ConsumedUsage { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	public Reservation Reservation { get; private set; } = null!;
	public BookingLine? BookingLine { get; private set; }

	private BookingDiscount() { }
}
