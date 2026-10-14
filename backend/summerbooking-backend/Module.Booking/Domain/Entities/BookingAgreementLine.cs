using Module.Booking.Domain.Enums;
using Shared.Contracts;

namespace Module.Booking.Domain.Entities;

/// <summary>
/// Voce di listino/voucher di una convenzione consumata da una riga prenotazione, con snapshot del prezzo applicato.
/// </summary>
public sealed class BookingAgreementLine : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public Guid PublicId { get; private set; }
	public long ReservationId { get; private set; }
	public long? BookingLineId { get; private set; }
	public Guid PropertyId { get; private set; }
	public Guid AgreementPartnerId { get; private set; }
	public Guid? AgreementPriceListId { get; private set; }
	public Guid? CustomerId { get; private set; }
	public AgreementLineType ListType { get; private set; }
	public short Quantity { get; private set; }
	public short? PriceCalculation { get; private set; }
	public decimal AppliedPrice { get; private set; }
	public bool IsUsedAsDiscount { get; private set; }

	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	public Reservation Reservation { get; private set; } = null!;
	public BookingLine? BookingLine { get; private set; }

	private BookingAgreementLine() { }
}
