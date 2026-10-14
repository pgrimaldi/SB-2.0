using Module.Booking.Domain.Enums;
using Shared.Contracts;

namespace Module.Booking.Domain.Entities;

/// <summary>
/// Testata unica di una prenotazione logica (un solo booking_number), spot (con postazioni) o di soli servizi.
/// </summary>
public sealed class Reservation : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public Guid PublicId { get; private set; }
	public Guid PropertyId { get; private set; }
	public Guid? BathhouseId { get; private set; }
	public string BookingNumber { get; private set; } = null!;
	public string ReservationCode { get; private set; } = null!;
	public ReservationKind Kind { get; private set; }
	public Guid? CustomerId { get; private set; }
	public BookingChannel Channel { get; private set; }
	public ReservationStatus Status { get; private set; }
	public OnlineConfirmationState OnlineConfirmationState { get; private set; }
	public Guid? OperatorUserId { get; private set; }
	public Guid? UpdatedByUserId { get; private set; }
	public Guid? AgreementPartnerId { get; private set; }
	public ListingType ListingType { get; private set; }
	public bool HasSeasonalPass { get; private set; }
	public bool IsSplit { get; private set; }
	public List<ReservationPriceFlag> PriceFlags { get; private set; } = new();
	public bool PayNow { get; private set; }
	public bool IsOrderFilled { get; private set; }
	public string? Note { get; private set; }
	public List<string> CustomerTags { get; private set; } = new();
	public DateOnly? StayFrom { get; private set; }
	public DateOnly? StayTo { get; private set; }
	public string Currency { get; private set; } = null!;
	public decimal Subtotal { get; private set; }
	public decimal Discount { get; private set; }
	public decimal Surcharge { get; private set; }
	public decimal Total { get; private set; }
	public decimal? PriceOverride { get; private set; }
	public decimal Deposit { get; private set; }
	public decimal Paid { get; private set; }
	public decimal BalanceDue { get; private set; }
	public PaymentMode? PrevalentPaymentMode { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	public ICollection<BookingLine> Lines { get; private set; } = new List<BookingLine>();
	public ICollection<BookingLineAdditionalItem> AdditionalItems { get; private set; } = new List<BookingLineAdditionalItem>();
	public ICollection<BookingServiceLine> ServiceLines { get; private set; } = new List<BookingServiceLine>();
	public ICollection<BookingDiscount> Discounts { get; private set; } = new List<BookingDiscount>();
	public ICollection<BookingGuest> Guests { get; private set; } = new List<BookingGuest>();
	public ICollection<BookingParticipant> Participants { get; private set; } = new List<BookingParticipant>();
	public ICollection<BookingAgreementLine> AgreementLines { get; private set; } = new List<BookingAgreementLine>();
	public ICollection<BookingQuickActionDate> QuickActionDates { get; private set; } = new List<BookingQuickActionDate>();

	private Reservation() { }
}
