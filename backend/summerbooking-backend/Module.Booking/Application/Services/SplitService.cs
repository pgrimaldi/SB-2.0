using BuildingBlocks.Application.Validation;
using Module.Booking.Application.Abstractions;
using Module.Booking.Application.Dtos;
using Module.Booking.Application.Validators;
using Module.Booking.Domain;
using Module.Booking.Domain.Entities;
using Module.Booking.Domain.Enums;
using Shared.Contracts.Audit;
using Shared.Contracts.Errors;
using Shared.Contracts.Payment;

namespace Module.Booking.Application.Services;

/// <summary>
/// «Dividi conto»: una prenotazione per postazione. La prima postazione resta sulla prenotazione di partenza, le altre
/// passano a prenotazioni nuove (stesso cliente, date, pacchetti, aggiuntivi e convenzioni), non collegate fra loro né al
/// gruppo. Il prezzo non si ricalcola: il totale di prima (cumulativo, sconti e incremento compresi) si divide in
/// proporzione al prezzo di ogni postazione o come lo assegna il gestore; l'incassato segue le quote (o l'assegnazione a
/// mano) e si sposta con un movimento di Payment che non tocca la cassa. Lo scontrino già emesso resta uno.
/// </summary>
public sealed class SplitService
{
	private readonly IReservationRepository _reservations;
	private readonly ReservationWriter _writer;
	private readonly ReservationNumbers _numbers;
	private readonly IReservationPayments _payments;
	private readonly ReservationService _reservationService;
	private readonly SpotDirectory _spots;
	private readonly BookingScope _scope;
	private readonly BookingAudit _audit;
	private readonly SplitReservationValidator _validator;

	public SplitService(
		IReservationRepository reservations, ReservationWriter writer, ReservationNumbers numbers, IReservationPayments payments,
		ReservationService reservationService, SpotDirectory spots, BookingScope scope, BookingAudit audit, SplitReservationValidator validator)
	{
		_reservations = reservations;
		_writer = writer;
		_numbers = numbers;
		_payments = payments;
		_reservationService = reservationService;
		_spots = spots;
		_scope = scope;
		_audit = audit;
		_validator = validator;
	}

	private sealed record Part(Guid SpotId, List<BookingLine> Lines, decimal Weight)
	{
		public decimal Total { get; set; }
		public decimal Paid { get; set; }
		public Reservation? Reservation { get; set; }
		public WritePlan? Plan { get; set; }
	}

	public async Task<SplitResultDto> SplitAsync(Guid id, SplitReservationRequest request, CancellationToken ct = default)
	{
		_validator.EnsureValid(request);
		var source = await _reservations.GetAsync(_scope.PropertyId, id, includeTrashed: false, ct)
			?? throw DomainException.NotFound(BookingErrors.ReservationNotFound);
		if (!await _spots.CanSeeAsync(source, ct))
			throw DomainException.NotFound(BookingErrors.ReservationNotFound);
		if (source.Status == ReservationStatus.Cancelled)
			throw DomainException.Conflict(BookingErrors.ReservationCancelled);

		var parts = source.Lines.Where(l => l.DeletedAt is null && l.SpotId is not null)
			.GroupBy(l => l.SpotId!.Value)
			.Select(g => new Part(g.Key, g.OrderBy(l => l.StartDate).ToList(), g.Sum(l => l.LinePrice)))
			.OrderBy(p => p.Lines[0].StartDate).ThenBy(p => p.Lines[0].SpotLabelSnapshot)
			.ToList();
		if (parts.Count < 2)
			throw DomainException.Unprocessable(BookingErrors.SplitNeedsTwoSpots);
		AssignShares(source, parts, request);

		// Prima le prenotazioni nuove, poi quella di partenza resta con la prima postazione.
		var moved = parts.Skip(1).SelectMany(p => p.Lines).Select(l => l.Id).ToList();
		foreach (var part in parts.Skip(1))
		{
			var reservation = Reservation.SplitOf(source, await _numbers.NextNumberAsync(source.PropertyId, ct), await _numbers.NewCodeAsync(ct), _scope.RequiredUserId);
			var spots = part.Lines.Select(l => ReservationRequests.InputOf(source, l, l.SpotId!.Value, l.StartDate, l.EndDate) with { ManualPrice = null }).ToList();
			part.Plan = await _writer.ApplyAsync(reservation, Unpriced(ReservationRequests.StateOf(source, spots)),
				new WriteOptions(ReleasingLineIds: part.Lines.Select(l => l.Id).ToList(), FixedTotal: true), ct);
			part.Reservation = reservation;
			_reservations.Add(reservation);
		}
		var keep = parts[0].Lines.Select(l => ReservationRequests.InputOf(source, l, l.SpotId!.Value, l.StartDate, l.EndDate)).ToList();
		parts[0].Plan = await _writer.ApplyAsync(source, Unpriced(ReservationRequests.StateOf(source, keep)), new WriteOptions(FixedTotal: true), ct);
		parts[0].Reservation = source;

		foreach (var part in parts)
			part.Reservation!.FixTotal(part.Total);
		// La spunta «Pagato» non si può più togliere: il suo incasso ora è diviso fra più prenotazioni.
		source.MarkPaidEntry(null);
		source.Touch(_scope.UserId);
		await _writer.SaveAsync(parts.Select(p => (p.Reservation!, p.Plan!)).ToList(), ct);

		foreach (var part in parts.Skip(1).Where(p => p.Paid > 0))
			await _payments.TransferAsync(source.PropertyId, source.PublicId, part.Reservation!.PublicId, part.Paid, ct);

		await _audit.RecordAsync(AuditActionCodes.Booking.ReservationSplit, source, new Dictionary<string, object?>
		{
			["mode"] = request.Mode ?? SplitModes.Proportional,
			["parts"] = parts.Select(p => new { bookingNumber = p.Reservation!.BookingNumber, spot = p.Lines[0].SpotLabelSnapshot, total = p.Total, paid = p.Paid }).ToList(),
			["movedLines"] = moved.Count,
		}, ct: ct);

		var result = new List<ReservationDto>();
		foreach (var part in parts)
			result.Add(await _reservationService.ToDtoAsync(part.Reservation!, part.Plan!.Warnings, ct));
		return new SplitResultDto(result);
	}

	/// <summary>Quote del totale e dell'incassato: in proporzione al prezzo delle postazioni, o come le assegna il gestore.</summary>
	private static void AssignShares(Reservation source, List<Part> parts, SplitReservationRequest request)
	{
		var total = source.Total;
		var paid = Math.Max(0, source.Paid);
		if (request.Mode == SplitModes.Manual)
		{
			var shares = request.Shares!;
			var bySpot = shares.GroupBy(s => s.SpotId).ToDictionary(g => g.Key, g => g.ToList());
			var paidGiven = shares.Any(s => s.Paid is not null);
			if (bySpot.Count != parts.Count || bySpot.Values.Any(v => v.Count != 1) || parts.Any(p => !bySpot.ContainsKey(p.SpotId))
				|| shares.Sum(s => s.Total) != total
				|| (paidGiven && (shares.Any(s => s.Paid is null) || shares.Sum(s => s.Paid!.Value) != paid)))
				throw DomainException.Unprocessable(BookingErrors.SplitSharesInvalid, new Dictionary<string, object?> { ["total"] = total, ["paid"] = paid });
			foreach (var part in parts)
				part.Total = bySpot[part.SpotId][0].Total;
			if (paidGiven)
			{
				foreach (var part in parts)
					part.Paid = bySpot[part.SpotId][0].Paid!.Value;
				return;
			}
		}
		else
			Distribute(total, parts, p => p.Weight, (p, v) => p.Total = v);
		Distribute(paid, parts, p => p.Total, (p, v) => p.Paid = v);
	}

	/// <summary>Ripartisce <paramref name="amount"/> in proporzione ai pesi (in parti uguali se sono tutti zero); il resto all'ultima.</summary>
	private static void Distribute(decimal amount, List<Part> parts, Func<Part, decimal> weight, Action<Part, decimal> set)
	{
		var weights = parts.Select(weight).ToList();
		var sum = weights.Sum();
		var left = amount;
		for (var i = 0; i < parts.Count; i++)
		{
			var share = i == parts.Count - 1
				? left
				: decimal.Round(sum > 0 ? amount * weights[i] / sum : amount / parts.Count, 2, MidpointRounding.AwayFromZero);
			set(parts[i], share);
			left -= share;
		}
	}

	/// <summary>Righe da riscrivere senza sconti, incremento e prezzo applicato: il totale lo fissa la quota.</summary>
	private static SaveReservationRequest Unpriced(SaveReservationRequest request)
		=> request with { DiscountCode = null, ManualDiscount = null, Surcharge = null, PriceOverride = null };
}
