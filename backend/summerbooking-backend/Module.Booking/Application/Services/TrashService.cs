using Shared.Contracts.Catalog;
using BuildingBlocks.Application.Validation;
using Module.Booking.Application.Abstractions;
using Module.Booking.Application.Dtos;
using Module.Booking.Application.Validators;
using Module.Booking.Domain;
using Module.Booking.Domain.Entities;
using Module.Booking.Domain.Enums;
using Shared.Contracts.Audit;
using Shared.Contracts.Errors;
using Shared.Contracts.Fiskaly;
using Shared.Contracts.Identity;
using Shared.Contracts.Payment;
using Shared.Dtos;
using Shared.Localization;

namespace Module.Booking.Application.Services;

/// <summary>
/// Eliminazione e cestino. Una prenotazione eliminata va nel cestino (annullata, postazioni liberate) e da lì si
/// ripristina per 30 giorni, se nessuno ha preso le sue postazioni; poi il job notturno la elimina davvero. Della
/// prenotazione eliminata resta la traccia nel registro (12 mesi); incassi e scontrini restano in Payment e Fiskaly per
/// il tempo di legge. «Elimina definitivamente» e «Svuota cestino» possono anticipare i 30 giorni.
/// </summary>
public sealed class TrashService
{
	private readonly IReservationRepository _reservations;
	private readonly IReservationPayments _payments;
	private readonly IUserDirectory _users;
	private readonly BookingScope _scope;
	private readonly BookingAudit _audit;
	private readonly DeleteReservationsValidator _deleteValidator;
	private readonly CleanupMoveValidator _cleanupValidator;
	private readonly BulkIdsValidator _idsValidator;
	private readonly SharingLedger _sharing;
	private readonly IDiscountRedemption _redemptions;
	private readonly IReservationReceipts _receipts;

	public TrashService(
		IReservationRepository reservations, IReservationPayments payments, IUserDirectory users, BookingScope scope, BookingAudit audit,
		DeleteReservationsValidator deleteValidator, CleanupMoveValidator cleanupValidator, BulkIdsValidator idsValidator, SharingLedger sharing, IDiscountRedemption redemptions,
		IReservationReceipts receipts)
	{
		_receipts = receipts;
		_sharing = sharing;
		_redemptions = redemptions;
		_reservations = reservations;
		_payments = payments;
		_users = users;
		_scope = scope;
		_audit = audit;
		_deleteValidator = deleteValidator;
		_cleanupValidator = cleanupValidator;
		_idsValidator = idsValidator;
	}

	/// <summary>
	/// «Elimina» dalla pagina Prenotazioni: una scelta sola per tutta la selezione. Con il rimborso si rimborsano gli
	/// incassi (uscita di cassa con lo stesso metodo; quelli online con carta restano da rimborsare a mano) e l'annullo
	/// degli scontrini è obbligatorio. Gli scontrini si annullano in Fiskaly; quelli che non si possono annullare restano
	/// contati in <c>ReceiptsVoidPending</c>.
	/// </summary>
	public async Task<DeleteResultDto> DeleteAsync(DeleteReservationsRequest request, CancellationToken ct = default)
	{
		_deleteValidator.EnsureValid(request);
		if (request.Refund && !request.VoidReceipts)
			throw DomainException.Unprocessable(BookingErrors.RefundRequiresVoidReceipts);

		var propertyId = _scope.PropertyId;
		var found = (await _reservations.GetManyAsync(propertyId, request.Ids.Distinct().ToList(), trashed: false, ct)).ToDictionary(r => r.PublicId);
		var succeeded = new List<Guid>();
		var failed = new List<BulkFailure>();
		decimal refunded = 0, pending = 0;
		int receiptsVoided = 0, receiptsPending = 0;
		foreach (var id in request.Ids.Distinct())
		{
			if (!found.TryGetValue(id, out var reservation))
			{
				failed.Add(new BulkFailure(id, BookingErrors.ReservationNotFound));
				continue;
			}
			ReservationRefundResult? refund = null;
			if (request.Refund && reservation.Paid > 0)
			{
				refund = await _payments.RefundAllAsync(propertyId, reservation.PublicId, ct);
				refunded += refund.Refunded;
				pending += refund.PendingOnline;
			}
			ReceiptVoidResult? receipts = null;
			if (request.VoidReceipts)
			{
				receipts = await _receipts.VoidAllAsync(propertyId, reservation.PublicId, ct);
				receiptsVoided += receipts.Voided;
				if (!receipts.Available || receipts.Pending > 0)
					receiptsPending++;
			}
			await MoveToTrashAsync(reservation, ct);
			await _audit.RecordAsync(AuditActionCodes.Booking.ReservationTrashed, reservation, new Dictionary<string, object?>
			{
				["refund"] = request.Refund, ["voidReceipts"] = request.VoidReceipts, ["receiptsVoided"] = receipts?.Voided ?? 0,
				["receiptsVoidPending"] = receipts is { } rv && (!rv.Available || rv.Pending > 0), ["refunded"] = refund?.Refunded ?? 0,
				["refundPendingOnline"] = refund?.PendingOnline ?? 0, ["total"] = reservation.Total, ["paid"] = reservation.Paid,
			}, ct: ct);
			succeeded.Add(id);
		}
		return new DeleteResultDto(succeeded, failed, refunded, pending, receiptsVoided, receiptsPending);
	}

	/// <summary>
	/// «Cancella dati» della pagina Impostazioni › Database: pulizia di prenotazioni vecchie, concluse o no, confermata
	/// con la password. Nessun rimborso e nessun annullo degli scontrini; finiscono nello stesso cestino.
	/// </summary>
	public async Task<BulkResult> CleanupAsync(CleanupMoveRequest request, CancellationToken ct = default)
	{
		_cleanupValidator.EnsureValid(request);
		if (!await _users.VerifyPasswordAsync(_scope.RequiredUserId, request.Password, ct))
			throw DomainException.Unprocessable(BookingErrors.PasswordInvalid);

		var propertyId = _scope.PropertyId;
		var found = (await _reservations.GetManyAsync(propertyId, request.Ids.Distinct().ToList(), trashed: false, ct)).ToDictionary(r => r.PublicId);
		var succeeded = new List<Guid>();
		var failed = new List<BulkFailure>();
		foreach (var id in request.Ids.Distinct())
		{
			if (!found.TryGetValue(id, out var reservation))
			{
				failed.Add(new BulkFailure(id, BookingErrors.ReservationNotFound));
				continue;
			}
			await MoveToTrashAsync(reservation, ct);
			succeeded.Add(id);
		}
		await _audit.RecordAsync(AuditActionCodes.Booking.ReservationsCleanedUp, propertyId, AuditActionCodes.Booking.Targets.Reservation, null,
			new Dictionary<string, object?> { ["count"] = succeeded.Count, ["bookingNumbers"] = found.Values.Where(r => succeeded.Contains(r.PublicId)).Select(r => r.BookingNumber).ToList() }, ct: ct);
		return new BulkResult(succeeded, failed);
	}

	/// <summary>Ripristino dal cestino entro 30 giorni: torna confermata e da pagare per quanto rimborsato.</summary>
	public async Task<BulkResult> RestoreAsync(BulkIdsRequest request, CancellationToken ct = default)
	{
		_idsValidator.EnsureValid(request);
		var propertyId = _scope.PropertyId;
		var cutoff = _scope.UtcNow.AddDays(-BookingRules.TrashRestoreDays);
		await _reservations.ReviveTrashedChildrenAsync(propertyId, request.Ids.Distinct().ToList(), ct);
		var found = (await _reservations.GetManyAsync(propertyId, request.Ids.Distinct().ToList(), trashed: true, ct)).ToDictionary(r => r.PublicId);
		var succeeded = new List<Guid>();
		var failed = new List<BulkFailure>();
		foreach (var id in request.Ids.Distinct())
		{
			if (!found.TryGetValue(id, out var reservation))
			{
				failed.Add(new BulkFailure(id, BookingErrors.ReservationNotInTrash));
				continue;
			}
			if (reservation.DeletedAt < cutoff)
			{
				failed.Add(new BulkFailure(id, BookingErrors.RestoreWindowExpired));
				continue;
			}
			var ceded = await _sharing.CededDaysAsync(reservation, ct);
			if (await FirstConflictAsync(reservation, ceded, ct) is { } conflict)
			{
				failed.Add(new BulkFailure(id, BookingErrors.SpotUnavailable, conflict));
				continue;
			}
			try
			{
				var lineIds = reservation.Lines.Select(l => l.Id).ToList();
				var checkout = reservation.CheckoutEffectiveAt is { } c ? DateOnly.FromDateTime(ItalianTime.FromUtc(c)) : (DateOnly?)null;
				foreach (var consumption in await _reservations.ListLineConsumptionsAsync(lineIds, ct))
					if (consumption.State == ConsumptionState.Released && (checkout is null || consumption.ConsumptionDate < checkout)
						&& !(consumption.SourceId is { } sid && ceded.Contains((sid, consumption.ConsumptionDate))))
						consumption.Reactivate();
				// i servizi riprendono il loro posto (i limiti superati nel frattempo non bloccano il ripristino)
				foreach (var consumption in await _reservations.ListServiceConsumptionsAsync(reservation.ServiceLines.Select(s => s.Id).ToList(), ct))
					consumption.Reactivate();
				reservation.RestoreFromTrash(_scope.UserId);
				_reservations.Untrash(reservation);
				await _sharing.ClaimAsync(reservation, reservation.Lines.ToList(), ct);
				await _reservations.SaveChangesAsync(ct);
				await _sharing.PublishPendingAsync(ct);
				if (reservation.Discounts.Select(d => d.DiscountId).FirstOrDefault(d => d is not null) is { } discount)
					await _redemptions.TryRedeemAsync(reservation.PropertyId, discount, ct);
			}
			catch (DomainException ex) when (ex.Code == BookingErrors.SpotUnavailable)
			{
				failed.Add(new BulkFailure(id, ex.Code));
				continue;
			}
			await _audit.RecordAsync(AuditActionCodes.Booking.ReservationRestored, reservation,
				new Dictionary<string, object?> { ["paid"] = reservation.Paid, ["balanceDue"] = reservation.BalanceDue }, ct: ct);
			succeeded.Add(id);
		}
		return new BulkResult(succeeded, failed);
	}

	/// <summary>«Elimina definitivamente»: anche prima dei 30 giorni; resta la traccia nel registro.</summary>
	public async Task<BulkResult> PurgeAsync(BulkIdsRequest request, CancellationToken ct = default)
	{
		_idsValidator.EnsureValid(request);
		var propertyId = _scope.PropertyId;
		var found = (await _reservations.GetManyAsync(propertyId, request.Ids.Distinct().ToList(), trashed: true, ct)).ToDictionary(r => r.PublicId);
		var failed = request.Ids.Distinct().Where(id => !found.ContainsKey(id)).Select(id => new BulkFailure(id, BookingErrors.ReservationNotInTrash)).ToList();
		await PurgeCoreAsync(propertyId, found.Values.Select(r => (r.PublicId, r.BookingNumber)).ToList(), automatic: false, ct);
		return new BulkResult(found.Keys.ToList(), failed);
	}

	/// <summary>«Svuota cestino»: tutte le prenotazioni nel cestino dello stabilimento.</summary>
	public async Task<int> EmptyAsync(CancellationToken ct = default)
	{
		var propertyId = _scope.PropertyId;
		var total = 0;
		while (true)
		{
			var (items, _) = await _reservations.ListAsync(new ReservationFilter(
				propertyId, null, null, null, null, null, null, null, null, null, Trashed: true, "deletedAt", false, 1, BulkIdsRequest.MaxIds), ct);
			if (items.Count == 0)
				return total;
			await PurgeCoreAsync(propertyId, items.Select(r => (r.PublicId, r.BookingNumber)).ToList(), automatic: false, ct);
			total += items.Count;
		}
	}

	/// <summary>Job notturno: elimina le prenotazioni rimaste nel cestino oltre i 30 giorni, di tutti gli stabilimenti.</summary>
	public async Task<int> PurgeExpiredAsync(DateTime nowUtc, CancellationToken ct = default)
	{
		var cutoff = nowUtc.AddDays(-BookingRules.TrashRestoreDays);
		var total = 0;
		while (true)
		{
			var batch = await _reservations.ListTrashedBeforeAsync(cutoff, 500, ct);
			if (batch.Count == 0)
				return total;
			foreach (var group in batch.GroupBy(b => b.PropertyId))
				await PurgeCoreAsync(group.Key, group.Select(b => (b.ReservationId, b.BookingNumber)).ToList(), automatic: true, ct);
			total += batch.Count;
		}
	}

	private async Task PurgeCoreAsync(Guid propertyId, IReadOnlyList<(Guid Id, string Number)> reservations, bool automatic, CancellationToken ct)
	{
		if (reservations.Count == 0)
			return;
		await _reservations.PurgeAsync(propertyId, reservations.Select(r => r.Id).ToList(), ct);
		foreach (var (id, number) in reservations)
			await _audit.RecordAsync(AuditActionCodes.Booking.ReservationPurged, propertyId, AuditActionCodes.Booking.Targets.Reservation, id,
				new Dictionary<string, object?> { ["automatic"] = automatic }, number, ct: ct);
	}

	private async Task MoveToTrashAsync(Reservation reservation, CancellationToken ct)
	{
		foreach (var consumption in await _reservations.ListLineConsumptionsAsync(reservation.Lines.Select(l => l.Id).ToList(), ct))
			if (consumption.State != ConsumptionState.Released)
				consumption.Release();
		foreach (var consumption in await _reservations.ListServiceConsumptionsAsync(reservation.ServiceLines.Select(s => s.Id).ToList(), ct))
			consumption.Release();
		await _sharing.LinesRemovedAsync(reservation, reservation.Lines.ToList(), ct);
		reservation.CancelForTrash(_scope.UserId, _scope.UtcNow);
		_reservations.Trash(reservation, _scope.UtcNow);
		await _reservations.SaveChangesAsync(ct);
		await _sharing.PublishPendingAsync(ct);
		// annullata: il suo utilizzo del codice sconto torna disponibile (al ripristino si riprende se c'è ancora posto)
		if (reservation.Discounts.Select(d => d.DiscountId).FirstOrDefault(d => d is not null) is { } discount)
			await _redemptions.ReleaseAsync(reservation.PropertyId, discount, ct);
	}

	/// <summary>La prima postazione esclusiva occupata da altri nei giorni della prenotazione, se c'è.</summary>
	private async Task<IReadOnlyDictionary<string, object?>?> FirstConflictAsync(
		Reservation reservation, IReadOnlySet<(long LineId, DateOnly Day)> ceded, CancellationToken ct)
	{
		var lines = reservation.Lines.Where(l => l.SpotId is not null && !l.IsArea).ToList();
		if (lines.Count == 0)
			return null;
		var own = lines.Select(l => l.Id).ToHashSet();
		var occupied = await _reservations.ListOccupiedAsync(reservation.PropertyId, lines.Select(l => l.SpotId!.Value).Distinct().ToList(),
			lines.Min(l => l.StartDate), lines.Max(l => l.EndDate), ct);
		foreach (var o in occupied)
		{
			if (o.Source == ConsumptionSource.ReservationLine && o.SourceId is { } sid && own.Contains(sid))
				continue;
			var line = lines.FirstOrDefault(l => l.SpotId == o.SpotId && l.Covers(o.Day) && SpotKinds.Overlaps(l.BookingPeriod, o.Period)
				&& !ceded.Contains((l.Id, o.Day))
				&& SpotKinds.IsExclusive(SpotKinds.ResourceTypeOf(l.SpotTypeSnapshot ?? "umbrella")));
			if (line is not null)
				return new Dictionary<string, object?> { ["spotId"] = o.SpotId, ["label"] = line.SpotLabelSnapshot, ["day"] = o.Day };
		}
		return null;
	}
}
