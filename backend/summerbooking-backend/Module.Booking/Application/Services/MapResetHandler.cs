using Module.Booking.Application.Dtos;
using Shared.Dtos;
using Shared.Localization;
using Module.Booking.Application.Abstractions;
using Module.Booking.Domain;
using Module.Booking.Domain.Entities;
using Module.Booking.Domain.Enums;
using Shared.Contracts.Audit;
using Shared.Contracts.Catalog;
using Shared.Contracts.Events;
using Shared.Contracts.Identity;

namespace Module.Booking.Application.Services;

/// <summary>
/// Postazioni tolte o create nell'editor della mappa. Con «elimina le prenotazioni» le prenotazioni non ancora
/// finite delle postazioni tolte si annullano (motivo <c>spot_removed</c>, anche se pagate). Altrimenti le loro righe restano
/// senza postazione e si ricollegano alla postazione nuova nella stessa posizione (fila e colonna) se il tipo coincide; se la
/// posizione non c'è più, a quella nuova con la stessa etichetta e lo stesso tipo. Un tipo diverso nella stessa posizione
/// non si ricollega: la prenotazione resta senza postazione e compare nel rapporto.
/// </summary>
public sealed class MapSpotsChangedHandler : IIntegrationEventHandler<MapSpotsChanged>
{
	private readonly IReservationRepository _reservations;
	private readonly IBookingCatalog _catalog;
	private readonly BookingAudit _audit;
	private readonly BookingScope _scope;

	public MapSpotsChangedHandler(IReservationRepository reservations, IBookingCatalog catalog, BookingAudit audit, BookingScope scope)
	{
		_reservations = reservations;
		_catalog = catalog;
		_audit = audit;
		_scope = scope;
	}

	public async Task HandleAsync(MapSpotsChanged e, CancellationToken ct = default)
	{
		var today = _scope.Today;
		var live = (await _reservations.ListForPropertyAsync(e.PropertyId, today, ct)).Where(r => r.IsActive).ToList();

		if (e.Removed.Count > 0 && e.DeletesBookings)
		{
			var removed = e.Removed.Select(s => s.Id).ToHashSet();
			foreach (var reservation in live.Where(r => r.Lines.Any(l => l.DeletedAt is null && l.SpotId is { } s && removed.Contains(s))))
			{
				foreach (var c in await _reservations.ListAllConsumptionsAsync(reservation, ct))
					if (c.State != ConsumptionState.Released)
						c.Release();
				reservation.Cancel(CancellationReason.SpotRemoved, _scope.UserId, _scope.UtcNow);
				await _audit.RecordAsync(AuditActionCodes.Booking.ReservationCancelledBySpotRemoval, reservation,
					new Dictionary<string, object?> { ["automatic"] = true }, ct: ct);
			}
			await _reservations.SaveChangesAsync(ct);
		}

		if (e.Created.Count == 0)
			return;
		var current = (await _catalog.ListSpotsAsync(e.PropertyId, ct)).Select(s => s.Id).ToHashSet();
		var created = e.Created.ToList();
		foreach (var reservation in live)
		{
			var orphans = reservation.Lines.Where(l => l.DeletedAt is null && l.SpotId is { } s && !current.Contains(s) && l.EndDate >= today).ToList();
			foreach (var line in orphans)
			{
				var byPosition = created.FirstOrDefault(c => c.Row == line.SpotRowSnapshot && c.Column == line.SpotColumnSnapshot);
				var target = byPosition is not null
					? (byPosition.SpotType == line.SpotTypeSnapshot ? byPosition : null)
					: created.FirstOrDefault(c => c.SpotType == line.SpotTypeSnapshot && string.Equals(c.Label, line.SpotLabelSnapshot, StringComparison.OrdinalIgnoreCase));
				if (target is null)
					continue;
				var oldLabel = line.SpotLabelSnapshot;
				var oldSpot = line.SpotId;
				line.Relink(new SpotSnapshot(target.Id, target.Label, target.SpotType, line.IsArea, line.SectorId, line.SectorLabelSnapshot, target.Row, target.Column));
				foreach (var c in await _reservations.ListLineConsumptionsAsync([line.Id], ct))
					if (c.ResourceId == oldSpot)
						c.MoveTo(target.Id, target.Label);
				await _audit.RecordAsync(AuditActionCodes.Booking.ReservationRelinked, reservation, new Dictionary<string, object?>
				{
					["fromSpot"] = oldLabel, ["toSpot"] = target.Label, ["byPosition"] = byPosition is not null,
				}, target.Label, ct);
			}
		}
		await _reservations.SaveChangesAsync(ct);
	}
}

/// <summary>Rapporto della reimpostazione: le prenotazioni non ancora finite che non hanno più una postazione sulla mappa.</summary>
public sealed class OrphanReportService
{
	private readonly IReservationRepository _reservations;
	private readonly SpotDirectory _spots;
	private readonly ICustomerDirectory _customers;
	private readonly BookingScope _scope;

	private readonly IOrphanReportWriter _writer;
	private readonly IBookingCatalog _catalog;
	private readonly IRequestLanguage _language;

	public OrphanReportService(
		IReservationRepository reservations, SpotDirectory spots, ICustomerDirectory customers, BookingScope scope, IOrphanReportWriter writer,
		IBookingCatalog catalog, IRequestLanguage language)
	{
		_writer = writer;
		_catalog = catalog;
		_language = language;
		_reservations = reservations;
		_spots = spots;
		_customers = customers;
		_scope = scope;
	}

	/// <summary>Lo stesso rapporto in PDF, nella lingua dell'operatore.</summary>
	public async Task<TabularFile> PdfAsync(CancellationToken ct = default)
	{
		var rows = await ListAsync(ct);
		var name = (await _catalog.FindOnlineRulesAsync(_scope.PropertyId, ct))?.PropertyName ?? "";
		return _writer.Write(rows, name, _language.Language, _scope.UtcNow);
	}

	public async Task<IReadOnlyList<OrphanLineDto>> ListAsync(CancellationToken ct = default)
	{
		var current = (await _spots.AllAsync(ct)).Keys.ToHashSet();
		var live = (await _reservations.ListForPropertyAsync(_scope.PropertyId, _scope.Today, ct)).Where(r => r.IsActive).ToList();
		var rows = live.SelectMany(r => r.Lines.Where(l => l.DeletedAt is null && l.SpotId is { } s && !current.Contains(s) && l.EndDate >= _scope.Today)
			.Select(l => (Reservation: r, Line: l))).ToList();
		var ids = rows.Select(x => x.Reservation.CustomerId).Where(c => c is not null).Select(c => c!.Value).Distinct().ToList();
		var names = ids.Count == 0 ? [] : (await _customers.ListAsync(_scope.PropertyId, ids, ct)).ToDictionary(c => c.Id, c => c.FullName);
		return rows.OrderBy(x => x.Line.StartDate).Select(x => new OrphanLineDto(
			x.Reservation.PublicId, x.Reservation.BookingNumber, x.Reservation.CustomerId is { } c ? names.GetValueOrDefault(c) : null, x.Line.PublicId,
			x.Line.SpotLabelSnapshot, x.Line.SpotTypeSnapshot, x.Line.SpotRowSnapshot, x.Line.SpotColumnSnapshot, x.Line.StartDate, x.Line.EndDate)).ToList();
	}
}
