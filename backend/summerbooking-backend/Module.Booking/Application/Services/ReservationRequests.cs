using Module.Booking.Application.Dtos;
using Module.Booking.Domain;
using Module.Booking.Domain.Entities;
using Module.Booking.Domain.Enums;
using Shared.Utils;

namespace Module.Booking.Application.Services;

/// <summary>
/// Lo stato attuale di una prenotazione nella forma della richiesta di modifica: serve alle operazioni che la riscrivono
/// partendo da com'è (spostamento, scambio, rinnovo, dividi conto).
/// </summary>
public static class ReservationRequests
{
	/// <summary>Una riga come postazione della richiesta, con altre date o un'altra postazione.</summary>
	public static ReservationSpotInput InputOf(Reservation reservation, BookingLine line, Guid spotId, DateOnly from, DateOnly to)
	{
		var agreements = reservation.AgreementLines.Where(a => a.BookingLineId == line.Id || a.BookingLine == line).ToList();
		var regular = agreements.FirstOrDefault(a => a.ListType == AgreementLineType.Regular);
		var voucher = agreements.FirstOrDefault(a => a.ListType == AgreementLineType.Voucher);
		var additionals = reservation.AdditionalItems.Where(a => (a.BookingLineId == line.Id || a.BookingLine == line) && a.CatalogItemId is not null)
			.Select(a => new AdditionalInput(a.CatalogItemId!.Value, a.Quantity, a.Dates.Where(d => d >= from && d <= to).ToList()))
			.ToList();
		return new ReservationSpotInput(spotId, from, to, regular is null ? line.PriceListId : null, regular?.AgreementPartnerId ?? voucher?.AgreementPartnerId,
			regular?.AgreementPriceListId, voucher?.AgreementPriceListId, line.ManualPrice, line.IsArea ? line.Quantity : null, additionals);
	}

	/// <summary>La prenotazione come richiesta di modifica, con le postazioni indicate.</summary>
	public static SaveReservationRequest StateOf(Reservation r, IReadOnlyList<ReservationSpotInput> spots)
	{
		var period = r.Lines.Where(l => l.DeletedAt is null).Select(l => l.BookingPeriod).FirstOrDefault();
		return new SaveReservationRequest(
			null, spots.Min(s => s.From!.Value), spots.Max(s => s.To!.Value), SpotKinds.CodeOf(period), r.HasSeasonalPass, spots,
			r.Discounts.FirstOrDefault(d => d.Origin == DiscountOrigin.CodeEntered)?.CodeSnapshot,
			r.ManualDiscountType is { } mt ? new AdjustmentInput(Codes.Of(mt), r.ManualDiscountValue ?? 0, null) : null,
			r.SurchargeType is { } st ? new AdjustmentInput(Codes.Of(st), r.SurchargeValue ?? 0, r.SurchargeLabel) : null,
			r.PriceOverride, r.Note, r.CustomerTags, r.Guests.Select(g => g.FullNameSnapshot).ToList(), true);
	}

}
