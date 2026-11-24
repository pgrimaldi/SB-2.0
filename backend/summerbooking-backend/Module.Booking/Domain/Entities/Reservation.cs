using Module.Booking.Domain.Enums;
using Module.Booking.Domain.Pricing;
using Shared.Contracts;

namespace Module.Booking.Domain.Entities;

/// <summary>
/// Testata unica di una prenotazione logica (un solo booking_number), spot (con postazioni) o di soli servizi.
/// Il cestino è la cancellazione logica (<see cref="DeletedAt"/>): da lì si ripristina per 30 giorni.
/// </summary>
public sealed class Reservation : IAuditable, ISoftDeletable
{
	public const int NoteMaxLength = 4000;

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
	/// <summary>Riepilogo: la struttura convenzionata della prima riga; il dettaglio per riga è in <see cref="AgreementLines"/>.</summary>
	public Guid? AgreementPartnerId { get; private set; }
	public ListingType ListingType { get; private set; }
	public bool HasSeasonalPass { get; private set; }
	public List<ReservationPriceFlag> PriceFlags { get; private set; } = new();
	public bool PayNow { get; private set; }
	public bool IsOrderFilled { get; private set; }
	public string? Note { get; private set; }
	public DateTime? NoteUpdatedAt { get; private set; }
	public Guid? NoteUpdatedByUserId { get; private set; }
	public List<string> CustomerTags { get; private set; } = new();
	public DateOnly? StayFrom { get; private set; }
	public DateOnly? StayTo { get; private set; }
	public string Currency { get; private set; } = null!;
	public decimal Subtotal { get; private set; }
	public decimal Discount { get; private set; }
	public AdjustmentType? ManualDiscountType { get; private set; }
	public decimal? ManualDiscountValue { get; private set; }
	public decimal ManualDiscountAmount { get; private set; }
	public string? SurchargeLabel { get; private set; }
	public AdjustmentType? SurchargeType { get; private set; }
	public decimal? SurchargeValue { get; private set; }
	public decimal Surcharge { get; private set; }
	public decimal Total { get; private set; }
	public decimal? PriceOverride { get; private set; }
	public decimal Deposit { get; private set; }
	public decimal Paid { get; private set; }
	public decimal BalanceDue { get; private set; }
	public PaymentMode? PrevalentPaymentMode { get; private set; }
	/// <summary>Movimento di Payment creato con la spunta «Pagato»: togliendo la spunta si annulla quello.</summary>
	public Guid? PaidMarkEntryId { get; private set; }
	public DateTime? CheckedInAt { get; private set; }
	public DateTime? CheckedOutAt { get; private set; }
	public DateTime? CheckoutEffectiveAt { get; private set; }
	public CancellationReason? CancellationReason { get; private set; }
	public DateTime? CancelledAt { get; private set; }
	public DateTime? ConfirmationExpiresAt { get; private set; }
	/// <summary>Prenotazione online in attesa del pagamento: le postazioni restano tenute fino a quest'ora.</summary>
	public DateTime? PaymentHoldUntil { get; private set; }
	public Guid? ConfirmedByUserId { get; private set; }
	public DateTime? ConfirmedAt { get; private set; }
	public long? RenewedFromReservationId { get; private set; }
	public long? SplitFromReservationId { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }
	/// <summary>Versione della riga (xmin di PostgreSQL): ETag per l'If-Match e controllo delle modifiche contemporanee.</summary>
	public uint Version { get; private set; }
	/// <summary>Cursore di sincronizzazione: cresce a ogni modifica della prenotazione o delle sue righe.</summary>
	public long SyncSeq { get; private set; }

	public ICollection<BookingLine> Lines { get; private set; } = new List<BookingLine>();
	public ICollection<BookingLineAdditionalItem> AdditionalItems { get; private set; } = new List<BookingLineAdditionalItem>();
	public ICollection<BookingServiceLine> ServiceLines { get; private set; } = new List<BookingServiceLine>();
	public ICollection<BookingDiscount> Discounts { get; private set; } = new List<BookingDiscount>();
	public ICollection<BookingGuest> Guests { get; private set; } = new List<BookingGuest>();
	public ICollection<BookingParticipant> Participants { get; private set; } = new List<BookingParticipant>();
	public ICollection<BookingAgreementLine> AgreementLines { get; private set; } = new List<BookingAgreementLine>();
	public ICollection<BookingTableLine> TableLines { get; private set; } = new List<BookingTableLine>();

	private Reservation() { }

	/// <summary>Prenotazione di postazioni dal gestionale: confermata subito.</summary>
	public static Reservation CreateByManager(
		Guid propertyId, string bookingNumber, string reservationCode, Guid? customerId, Guid operatorUserId, bool seasonal, string currency)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(bookingNumber);
		ArgumentException.ThrowIfNullOrWhiteSpace(reservationCode);
		return new Reservation
		{
			PublicId = Guid.CreateVersion7(),
			PropertyId = propertyId,
			BookingNumber = bookingNumber,
			ReservationCode = reservationCode,
			Kind = ReservationKind.Spot,
			CustomerId = customerId,
			Channel = BookingChannel.Manager,
			Status = ReservationStatus.Confirmed,
			OnlineConfirmationState = OnlineConfirmationState.NotRequired,
			OperatorUserId = operatorUserId,
			UpdatedByUserId = operatorUserId,
			HasSeasonalPass = seasonal,
			ListingType = ListingType.Standard,
			Currency = currency,
		};
	}

	/// <summary>
	/// Prenotazione dal sito o dall'area cliente. Con la conferma del gestore resta «in attesa» fino alla scadenza; con il
	/// pagamento online resta in attesa del pagamento fino a <paramref name="paymentHoldUntil"/>.
	/// </summary>
	public static Reservation CreateOnline(
		Guid propertyId, string bookingNumber, string reservationCode, Guid customerId, Guid? accountId, bool seasonal, string currency,
		DateTime? confirmationExpiresAt, DateTime? paymentHoldUntil, bool payNow)
	{
		var r = CreateByManager(propertyId, bookingNumber, reservationCode, customerId, accountId ?? Guid.Empty, seasonal, currency);
		r.OperatorUserId = accountId;
		r.UpdatedByUserId = accountId;
		r.Channel = BookingChannel.Customer;
		r.PayNow = payNow;
		r.ConfirmationExpiresAt = confirmationExpiresAt;
		r.OnlineConfirmationState = confirmationExpiresAt is null ? OnlineConfirmationState.NotRequired : OnlineConfirmationState.Pending;
		r.PaymentHoldUntil = paymentHoldUntil;
		r.Status = confirmationExpiresAt is not null || paymentHoldUntil is not null ? ReservationStatus.Pending : ReservationStatus.Confirmed;
		return r;
	}

	public bool AwaitsPayment => PaymentHoldUntil is not null && Status == ReservationStatus.Pending;

	public bool AwaitsConfirmation => OnlineConfirmationState == OnlineConfirmationState.Pending && Status == ReservationStatus.Pending;

	/// <summary>Il cliente ha premuto «Paga»: la postazione resta tenuta finché la banca risponde.</summary>
	public void ExtendPaymentHold(DateTime until) => PaymentHoldUntil = until;

	/// <summary>Pagamento arrivato: resta in attesa della conferma del gestore, se la chiede, altrimenti è confermata.</summary>
	public void PaymentReceived()
	{
		PaymentHoldUntil = null;
		if (Status == ReservationStatus.Pending && !AwaitsConfirmation)
			Status = ReservationStatus.Confirmed;
	}

	/// <summary>Pagamento arrivato tardi, a prenotazione già annullata ma con la postazione ancora libera: si conferma.</summary>
	public void ReviveAfterLatePayment()
	{
		Status = OnlineConfirmationState == OnlineConfirmationState.Pending ? ReservationStatus.Pending : ReservationStatus.Confirmed;
		CancellationReason = null;
		CancelledAt = null;
		PaymentHoldUntil = null;
	}

	public void ConfirmOnline(Guid? userId, DateTime now)
	{
		OnlineConfirmationState = OnlineConfirmationState.Confirmed;
		ConfirmedByUserId = userId;
		ConfirmedAt = now;
		ConfirmationExpiresAt = null;
		if (PaymentHoldUntil is null)
			Status = ReservationStatus.Confirmed;
		Touch(userId);
	}

	/// <summary>Annullata senza passare dal cestino: rifiuto, scadenza, pagamento non riuscito, annullamento del cliente.</summary>
	public void Cancel(Enums.CancellationReason reason, Guid? userId, DateTime now)
	{
		Status = ReservationStatus.Cancelled;
		CancellationReason = reason;
		CancelledAt = now;
		PaymentHoldUntil = null;
		if (reason is Enums.CancellationReason.Rejected)
			OnlineConfirmationState = OnlineConfirmationState.Rejected;
		if (reason is Enums.CancellationReason.ConfirmationExpired)
			OnlineConfirmationState = OnlineConfirmationState.Expired;
		Touch(userId);
	}

	/// <summary>
	/// Rinnovo per la stagione successiva: nuovo numero, confermata e da pagare, stesso cliente. Convenzioni e ospiti li
	/// copia il servizio; note, sconti, incremento e prezzo scritto a mano non passano.
	/// </summary>
	public static Reservation RenewalOf(Reservation source, string bookingNumber, string reservationCode, Guid operatorUserId)
	{
		var renewal = CreateByManager(source.PropertyId, bookingNumber, reservationCode, source.CustomerId, operatorUserId, source.HasSeasonalPass, source.Currency);
		renewal.BathhouseId = source.BathhouseId;
		renewal.RenewedFromReservationId = source.Id;
		return renewal;
	}

	/// <summary>
	/// Una delle prenotazioni nate da «Dividi conto»: stesso cliente, stato, canale, note e check-in dell'originale; il
	/// prezzo lo fissa il servizio con la quota che le spetta.
	/// </summary>
	public static Reservation SplitOf(Reservation source, string bookingNumber, string reservationCode, Guid operatorUserId)
	{
		var part = CreateByManager(source.PropertyId, bookingNumber, reservationCode, source.CustomerId, operatorUserId, source.HasSeasonalPass, source.Currency);
		part.BathhouseId = source.BathhouseId;
		part.Channel = source.Channel;
		part.Status = source.Status;
		part.OnlineConfirmationState = source.OnlineConfirmationState;
		part.ConfirmationExpiresAt = source.ConfirmationExpiresAt;
		part.Note = source.Note;
		part.NoteUpdatedAt = source.NoteUpdatedAt;
		part.NoteUpdatedByUserId = source.NoteUpdatedByUserId;
		part.CustomerTags = source.CustomerTags.ToList();
		part.CheckedInAt = source.CheckedInAt;
		part.CheckedOutAt = source.CheckedOutAt;
		part.CheckoutEffectiveAt = source.CheckoutEffectiveAt;
		part.SplitFromReservationId = source.Id;
		return part;
	}

	/// <summary>
	/// Prezzo fissato da «Dividi conto»: la quota del totale di prima, senza ricalcolo (il cumulativo si ripartisce, non
	/// si perde). Sconti e incremento restano dentro la quota.
	/// </summary>
	public void FixTotal(decimal total)
	{
		var subtotal = Lines.Where(l => l.DeletedAt is null).Sum(l => l.LinePrice);
		ApplyPricing(ReservationPricing.Compute(subtotal, 0m, null, null, total, false, false));
	}

	public bool IsTrashed => DeletedAt is not null;

	public bool IsActive => Status != ReservationStatus.Cancelled && DeletedAt is null;

	public void Touch(Guid? userId) => UpdatedByUserId = userId ?? UpdatedByUserId;

	public void ChangeCustomer(Guid? customerId) => CustomerId = customerId;

	public void SetSeasonal(bool seasonal) => HasSeasonalPass = seasonal;

	/// <summary>Nota e icone del cliente; l'autore e l'ora della nota cambiano solo se la nota cambia davvero.</summary>
	public void SetNote(string? note, IEnumerable<string>? tags, Guid? userId, DateTime now)
	{
		var clean = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
		if (clean is { Length: > NoteMaxLength })
			throw new ArgumentOutOfRangeException(nameof(note));
		if (!string.Equals(clean, Note, StringComparison.Ordinal))
		{
			Note = clean;
			NoteUpdatedAt = now;
			NoteUpdatedByUserId = userId;
		}
		CustomerTags = (tags ?? []).Select(t => t.Trim()).Where(t => t.Length > 0).Distinct(StringComparer.Ordinal).ToList();
	}

	/// <summary>Riepilogo della convenzione sulla testata: quella della prima riga che ne ha una.</summary>
	public void SetAgreementSummary(Guid? partnerId, ListingType listingType)
	{
		AgreementPartnerId = partnerId;
		ListingType = partnerId is null ? ListingType.Standard : listingType;
	}

	/// <summary>Importi calcolati dal preventivo, con sconto manuale, incremento e prezzo applicato a mano.</summary>
	public void ApplyPricing(ReservationPricing pricing)
	{
		Subtotal = pricing.Subtotal;
		Discount = -(pricing.CodeDiscount + pricing.ManualDiscountAmount);
		ManualDiscountType = pricing.ManualDiscount?.Type;
		ManualDiscountValue = pricing.ManualDiscount?.Value;
		ManualDiscountAmount = pricing.ManualDiscountAmount;
		SurchargeLabel = pricing.Surcharge?.Label;
		SurchargeType = pricing.Surcharge?.Type;
		SurchargeValue = pricing.Surcharge?.Value;
		Surcharge = pricing.SurchargeAmount;
		PriceOverride = pricing.PriceOverride;
		Total = pricing.Total;
		var flags = new List<ReservationPriceFlag>();
		if (pricing.PriceOverride is not null || pricing.HasManualPackagePrice)
			flags.Add(ReservationPriceFlag.ManuallyEdited);
		if (pricing.Recalculated)
			flags.Add(ReservationPriceFlag.Recalculated);
		PriceFlags = flags;
		RefreshBalance();
	}

	/// <summary>Totali dei movimenti, calcolati da Payment.</summary>
	public void ApplyPayments(decimal paid, decimal deposit, PaymentMode? prevalentMode)
	{
		Paid = paid;
		Deposit = deposit;
		PrevalentPaymentMode = prevalentMode;
		RefreshBalance();
	}

	public void MarkPaidEntry(Guid? entryId) => PaidMarkEntryId = entryId;

	/// <summary>
	/// Date complessive, ricavate dalle righe che restano dopo la modifica; una prenotazione di soli servizi o tavoli prende
	/// le date dei servizi.
	/// </summary>
	public void SetStay(IReadOnlyCollection<BookingLine> liveLines, IReadOnlyCollection<DateOnly>? serviceDates = null)
	{
		Kind = liveLines.Count == 0 && serviceDates is { Count: > 0 } ? ReservationKind.Service : ReservationKind.Spot;
		var days = liveLines.SelectMany(l => new[] { l.StartDate, l.EndDate }).Concat(serviceDates ?? []).ToList();
		StayFrom = days.Count == 0 ? null : days.Min();
		StayTo = days.Count == 0 ? null : days.Max();
	}

	public void CheckIn(DateTime now)
	{
		CheckedInAt ??= now;
	}

	public void UndoCheckIn()
	{
		CheckedInAt = null;
		CheckedOutAt = null;
		CheckoutEffectiveAt = null;
	}

	/// <summary>Uscita, anche anticipata: i giorni da oggi in poi si liberano (vedi il servizio).</summary>
	public void CheckOut(DateTime now)
	{
		CheckedInAt ??= now;
		CheckedOutAt = now;
		CheckoutEffectiveAt = now;
	}

	/// <summary>Annullata dal gestore: va nel cestino (la cancellazione logica la scrive l'interceptor).</summary>
	public void CancelForTrash(Guid? userId, DateTime now)
	{
		Status = ReservationStatus.Cancelled;
		CancellationReason = Enums.CancellationReason.Manager;
		CancelledAt = now;
		Touch(userId);
	}

	/// <summary>Stabilimento nel cestino: annullata senza rimborso né messaggi, si riattiva col ripristino.</summary>
	public void CancelForPropertyTrash(DateTime now)
	{
		Status = ReservationStatus.Cancelled;
		CancellationReason = Enums.CancellationReason.PropertyTrashed;
		CancelledAt = now;
	}

	/// <summary>Stabilimento ripristinato: tornano attive solo le prenotazioni annullate per il suo cestino.</summary>
	public bool ReactivateAfterPropertyRestore()
	{
		if (Status != ReservationStatus.Cancelled || CancellationReason != Enums.CancellationReason.PropertyTrashed)
			return false;
		Status = OnlineConfirmationState == OnlineConfirmationState.Pending ? ReservationStatus.Pending : ReservationStatus.Confirmed;
		CancellationReason = null;
		CancelledAt = null;
		return true;
	}

	/// <summary>Ripristino dal cestino: torna confermata; la cancellazione logica la toglie il repository.</summary>
	public void RestoreFromTrash(Guid? userId)
	{
		Status = ReservationStatus.Confirmed;
		CancellationReason = null;
		CancelledAt = null;
		Touch(userId);
	}

	private void RefreshBalance() => BalanceDue = Total - Paid;
}
