using BuildingBlocks.Application.Validation;
using Module.Booking.Application.Abstractions;
using Module.Booking.Application.Dtos;
using Module.Booking.Application.Validators;
using Module.Booking.Domain;
using Module.Booking.Domain.Entities;
using Module.Booking.Domain.Enums;
using Shared.Contracts.Audit;
using Shared.Contracts.Catalog;
using Shared.Contracts.Errors;
using Shared.Dtos;

namespace Module.Booking.Application.Services;

/// <summary>
/// Rinnovo («Duplica») per la stagione successiva: una prenotazione nuova, confermata e da pagare, con le stesse
/// postazioni e le stesse date dell'anno dopo (le stagionali: le date della nuova stagione). Prezzo con il listino di
/// oggi sui periodi della stagione successiva. Restano pacchetti, articoli aggiuntivi (non quelli «solo per determinati
/// giorni»), convenzioni e ospiti; non passano note, sconti, incremento, prezzi scritti a mano, servizi e gruppo.
/// Nessun messaggio al cliente. Una prenotazione si rinnova una volta sola.
/// </summary>
public sealed class RenewalService
{
	private const string ManagerCatalogPriceListNotFound = "pricing.price_list_not_found";
	private const string ManagerCatalogNotAPackage = "pricing.not_a_package";

	private readonly IReservationRepository _reservations;
	private readonly ReservationWriter _writer;
	private readonly ReservationNumbers _numbers;
	private readonly IBookingCatalog _catalog;
	private readonly SpotDirectory _spots;
	private readonly BookingScope _scope;
	private readonly BookingAudit _audit;
	private readonly BulkIdsValidator _bulkValidator;

	public RenewalService(
		IReservationRepository reservations, ReservationWriter writer, ReservationNumbers numbers, IBookingCatalog catalog, SpotDirectory spots,
		BookingScope scope, BookingAudit audit, BulkIdsValidator bulkValidator)
	{
		_reservations = reservations;
		_writer = writer;
		_numbers = numbers;
		_catalog = catalog;
		_spots = spots;
		_scope = scope;
		_audit = audit;
		_bulkValidator = bulkValidator;
	}

	/// <summary>Rinnovo di più prenotazioni: quelle che non si possono rinnovare restano nell'esito con il motivo.</summary>
	public async Task<RenewResultDto> RenewManyAsync(BulkIdsRequest request, CancellationToken ct = default)
	{
		_bulkValidator.EnsureValid(request);
		var season = await RequireNextSeasonAsync(ct);
		var succeeded = new List<RenewedDto>();
		var failed = new List<BulkFailure>();
		foreach (var id in request.Ids.Distinct())
		{
			try
			{
				succeeded.Add(await RenewOneAsync(id, season, ct));
			}
			catch (DomainException ex)
			{
				_reservations.DiscardChanges();
				failed.Add(new BulkFailure(id, ex.Code, ex.Args));
			}
		}
		return new RenewResultDto(succeeded, failed);
	}

	public async Task<RenewedDto> RenewAsync(Guid id, CancellationToken ct = default)
		=> await RenewOneAsync(id, await RequireNextSeasonAsync(ct), ct);

	private async Task<BookingSeason> RequireNextSeasonAsync(CancellationToken ct)
	{
		var season = await _catalog.FindSeasonAsync(_scope.PropertyId, ct);
		if (season?.NextStartsOn is null || season.NextEndsOn is null)
			throw DomainException.Unprocessable(BookingErrors.NextSeasonMissing);
		return season;
	}

	private async Task<RenewedDto> RenewOneAsync(Guid id, BookingSeason season, CancellationToken ct)
	{
		var source = await _reservations.GetAsync(_scope.PropertyId, id, includeTrashed: false, ct)
			?? throw DomainException.NotFound(BookingErrors.ReservationNotFound);
		if (!await _spots.CanSeeAsync(source, ct))
			throw DomainException.NotFound(BookingErrors.ReservationNotFound);
		if (source.Status == ReservationStatus.Cancelled)
			throw DomainException.Conflict(BookingErrors.ReservationCancelled);
		if (await _reservations.HasRenewalAsync(source.Id, ct))
			throw DomainException.Conflict(BookingErrors.AlreadyRenewed);

		var lines = source.Lines.Where(l => l.DeletedAt is null && l.SpotId is not null).OrderBy(l => l.StartDate).ToList();
		if (lines.Count == 0)
			throw DomainException.Unprocessable(BookingErrors.RenewalOutsideNextSeason);
		var nextStart = season.NextStartsOn!.Value;
		var nextEnd = season.NextEndsOn!.Value;

		var spots = new List<ReservationSpotInput>();
		foreach (var line in lines)
		{
			var (from, to) = (line.StartDate.AddYears(1), line.EndDate.AddYears(1));
			if (source.HasSeasonalPass)
			{
				// stagionale: la nuova stagione intera, con i tratti nelle stesse posizioni
				if (line.StartDate == lines[0].StartDate)
					from = nextStart;
				if (line.EndDate == lines.Max(l => l.EndDate))
					to = nextEnd;
			}
			if (from < nextStart || to > nextEnd || to < from)
				throw DomainException.Unprocessable(BookingErrors.RenewalOutsideNextSeason, new Dictionary<string, object?>
				{
					["from"] = from, ["to"] = to, ["seasonFrom"] = nextStart, ["seasonTo"] = nextEnd,
				});
			var additionals = source.AdditionalItems
				.Where(a => (a.BookingLineId == line.Id || a.BookingLine == line) && a.CatalogItemId is not null && a.Dates.Count == 0)
				.Select(a => new AdditionalInput(a.CatalogItemId!.Value, a.Quantity, null))
				.ToList();
			spots.Add(ReservationRequests.InputOf(source, line, line.SpotId!.Value, from, to) with { ManualPrice = null, Additionals = additionals });
		}

		var request = new SaveReservationRequest(
			null, spots.Min(s => s.From!.Value), spots.Max(s => s.To!.Value), SpotKinds.CodeOf(lines[0].BookingPeriod), source.HasSeasonalPass,
			spots, null, null, null, null, null, null, null, true);
		var renewal = Reservation.RenewalOf(source, await _numbers.NextNumberAsync(source.PropertyId, ct), await _numbers.NewCodeAsync(ct), _scope.RequiredUserId);
		foreach (var guest in source.Guests)
			renewal.Guests.Add(BookingGuest.Create(renewal, guest.FullNameSnapshot, null));

		WritePlan plan;
		try
		{
			plan = await _writer.ApplyAsync(renewal, request, new WriteOptions(NextSeason: true), ct);
		}
		catch (DomainException ex) when (ex.Code is ManagerCatalogPriceListNotFound or ManagerCatalogNotAPackage)
		{
			throw DomainException.Unprocessable(BookingErrors.RenewalPackageMissing);
		}
		if (plan.Warnings.Contains(QuoteWarnings.DaysWithoutPeriod) || plan.Warnings.Contains(QuoteWarnings.PeriodWithoutPrice))
			throw DomainException.Unprocessable(BookingErrors.RenewalPeriodMissing);

		_reservations.Add(renewal);
		await _writer.SaveAsync([(renewal, plan)], ct);
		await _audit.RecordAsync(AuditActionCodes.Booking.ReservationRenewed, renewal, new Dictionary<string, object?>
		{
			["renewedFrom"] = source.BookingNumber, ["from"] = renewal.StayFrom, ["to"] = renewal.StayTo, ["total"] = renewal.Total,
		}, ct: ct);
		return new RenewedDto(source.PublicId, renewal.PublicId, renewal.BookingNumber, _scope.CanSeeAmounts ? renewal.Total : null);
	}
}
