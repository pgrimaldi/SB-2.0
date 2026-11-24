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
using Shared.Dtos;

namespace Module.Booking.Application.Services;

/// <summary>
/// Gruppi di prenotazioni (comitive, famiglie su più ombrelloni): prenotazioni distinte, anche di clienti diversi, sotto un
/// nome; ognuna sta in un solo gruppo. Il gruppo non cambia i prezzi (il cumulativo vale solo dentro una prenotazione) e
/// serve a vederle insieme e a pagarle con un incasso solo.
/// </summary>
public sealed class GroupService
{
	private readonly IBookingGroupRepository _groups;
	private readonly IReservationRepository _reservations;
	private readonly IReservationPayments _payments;
	private readonly ReservationService _reservationService;
	private readonly SpotDirectory _spots;
	private readonly BookingScope _scope;
	private readonly BookingAudit _audit;
	private readonly CreateGroupValidator _createValidator;
	private readonly RenameGroupValidator _renameValidator;
	private readonly GroupMembersValidator _membersValidator;
	private readonly GroupPaymentValidator _paymentValidator;
	private readonly GroupQueryValidator _queryValidator;
	private readonly BulkIdsValidator _bulkValidator;

	public GroupService(
		IBookingGroupRepository groups, IReservationRepository reservations, IReservationPayments payments, ReservationService reservationService,
		SpotDirectory spots, BookingScope scope, BookingAudit audit, CreateGroupValidator createValidator, RenameGroupValidator renameValidator,
		GroupMembersValidator membersValidator, GroupPaymentValidator paymentValidator, GroupQueryValidator queryValidator, BulkIdsValidator bulkValidator)
	{
		_groups = groups;
		_reservations = reservations;
		_payments = payments;
		_reservationService = reservationService;
		_spots = spots;
		_scope = scope;
		_audit = audit;
		_createValidator = createValidator;
		_renameValidator = renameValidator;
		_membersValidator = membersValidator;
		_paymentValidator = paymentValidator;
		_queryValidator = queryValidator;
		_bulkValidator = bulkValidator;
	}

	public async Task<PagedResult<GroupSummaryDto>> ListAsync(GroupQuery query, CancellationToken ct = default)
	{
		_queryValidator.EnsureValid(query);
		var (items, total) = await _groups.ListAsync(
			_scope.PropertyId, query.Search, query.SortBy ?? "createdAt", query.SortDir == SortDirections.Desc || (query.SortDir is null && query.SortBy is null),
			query.Page, query.PageSize, ct);
		var cards = await _reservationService.CardsAsync(items.SelectMany(g => g.LiveMembers.Select(m => m.Reservation.CustomerId)), ct);
		return new PagedResult<GroupSummaryDto>(total, query.Page, query.PageSize, items.Select(g =>
		{
			var members = g.LiveMembers.ToList();
			var names = members.Select(m => m.Reservation.CustomerId is { } c && cards.TryGetValue(c, out var card) ? card.FullName : null)
				.Where(n => !string.IsNullOrWhiteSpace(n)).Select(n => n!).Distinct().ToList();
			return new GroupSummaryDto(g.PublicId, g.Name, members.Count, names, g.CreatedAt);
		}).ToList());
	}

	public async Task<GroupDto> GetAsync(Guid id, CancellationToken ct = default)
		=> await ToDtoAsync(await LoadAsync(id, ct), ct);

	/// <summary>Le prenotazioni già in un altro gruppo, annullate o non visibili restano fuori, con il motivo.</summary>
	public async Task<GroupChangeDto> CreateAsync(CreateGroupRequest request, CancellationToken ct = default)
	{
		_createValidator.EnsureValid(request);
		var propertyId = _scope.PropertyId;
		if (await _groups.NameExistsAsync(propertyId, request.Name, null, ct))
			throw DomainException.Conflict(BookingErrors.GroupNameTaken);
		var (accepted, failed) = await SelectMembersAsync(request.ReservationIds, null, ct);
		if (accepted.Count == 0)
			throw DomainException.Unprocessable(BookingErrors.GroupEmpty, new Dictionary<string, object?> { ["failed"] = failed });

		var group = BookingGroup.Create(propertyId, request.Name, _scope.UserId);
		foreach (var reservation in accepted)
			group.Add(reservation, _scope.UtcNow);
		_groups.Add(group);
		await _groups.SaveChangesAsync(ct);
		await RecordAsync(AuditActionCodes.Booking.GroupCreated, group, new Dictionary<string, object?>
		{
			["name"] = group.Name, ["reservations"] = accepted.Select(r => r.BookingNumber).ToList(),
		}, ct);
		return new GroupChangeDto(await ToDtoAsync(await LoadAsync(group.PublicId, ct), ct), failed);
	}

	public async Task<GroupDto> RenameAsync(Guid id, RenameGroupRequest request, CancellationToken ct = default)
	{
		_renameValidator.EnsureValid(request);
		var group = await LoadAsync(id, ct);
		if (await _groups.NameExistsAsync(group.PropertyId, request.Name, group.Id, ct))
			throw DomainException.Conflict(BookingErrors.GroupNameTaken);
		var previous = group.Name;
		group.Rename(request.Name);
		await _groups.SaveChangesAsync(ct);
		await RecordAsync(AuditActionCodes.Booking.GroupRenamed, group, new Dictionary<string, object?> { ["from"] = previous, ["to"] = group.Name }, ct);
		return await ToDtoAsync(group, ct);
	}

	public async Task<GroupChangeDto> AddMembersAsync(Guid id, GroupMembersRequest request, CancellationToken ct = default)
	{
		_membersValidator.EnsureValid(request);
		var group = await LoadAsync(id, ct);
		var (accepted, failed) = await SelectMembersAsync(request.ReservationIds, group, ct);
		foreach (var reservation in accepted)
			group.Add(reservation, _scope.UtcNow);
		if (accepted.Count > 0)
		{
			await _groups.SaveChangesAsync(ct);
			await RecordAsync(AuditActionCodes.Booking.GroupMembersChanged, group, new Dictionary<string, object?>
			{
				["added"] = accepted.Select(r => r.BookingNumber).ToList(),
			}, ct);
		}
		return new GroupChangeDto(await ToDtoAsync(group, ct), failed);
	}

	public async Task<GroupDto> RemoveMemberAsync(Guid id, Guid reservationId, CancellationToken ct = default)
	{
		var group = await LoadAsync(id, ct);
		var member = group.LiveMembers.FirstOrDefault(m => m.Reservation.PublicId == reservationId)
			?? throw DomainException.NotFound(BookingErrors.ReservationNotFound);
		_groups.RemoveMember(member);
		await _groups.SaveChangesAsync(ct);
		group.Members.Remove(member);
		await RecordAsync(AuditActionCodes.Booking.GroupMembersChanged, group, new Dictionary<string, object?>
		{
			["removed"] = new[] { member.Reservation.BookingNumber },
		}, ct);
		return await ToDtoAsync(group, ct);
	}

	/// <summary>Elimina i gruppi; le prenotazioni restano come sono.</summary>
	public async Task<BulkResult> DeleteAsync(BulkIdsRequest request, CancellationToken ct = default)
	{
		_bulkValidator.EnsureValid(request);
		var ids = request.Ids.Distinct().ToList();
		var groups = (await _groups.GetManyAsync(_scope.PropertyId, ids, ct)).ToDictionary(g => g.PublicId);
		var failed = ids.Where(i => !groups.ContainsKey(i)).Select(i => new BulkFailure(i, BookingErrors.GroupNotFound)).ToList();
		foreach (var group in groups.Values)
			_groups.Remove(group);
		await _groups.SaveChangesAsync(ct);
		foreach (var group in groups.Values)
			await RecordAsync(AuditActionCodes.Booking.GroupDeleted, group, new Dictionary<string, object?>
			{
				["name"] = group.Name, ["reservations"] = group.Members.Select(m => m.Reservation.BookingNumber).ToList(),
			}, ct);
		return new BulkResult(groups.Keys.ToList(), failed);
	}

	/// <summary>
	/// Pagamento unico: un incasso per prenotazione. Se l'importo copre i residui ognuna riceve il suo, altrimenti si
	/// divide in proporzione ai residui (il centesimo dell'arrotondamento all'ultima). Oltre il residuo del gruppo non si
	/// può: l'eccedenza non saprebbe a quale prenotazione andare.
	/// </summary>
	public async Task<GroupPaymentResultDto> PayAsync(Guid id, GroupPaymentRequest request, CancellationToken ct = default)
	{
		_paymentValidator.EnsureValid(request);
		var group = await LoadAsync(id, ct);
		var members = group.LiveMembers.Select(m => m.Reservation).Where(r => r.IsActive).ToList();
		if (request.ReservationIds is { } only)
		{
			var wanted = only.ToHashSet();
			if (!wanted.All(w => members.Any(r => r.PublicId == w)))
				throw DomainException.NotFound(BookingErrors.ReservationNotFound);
			members = members.Where(r => wanted.Contains(r.PublicId)).ToList();
		}
		if (request.PayerCustomerId is { } payer && !members.Any(r => r.CustomerId == payer))
			throw DomainException.Unprocessable(ValidationErrorCodes.Invalid, new Dictionary<string, object?> { ["field"] = "payerCustomerId" });

		var due = members.Where(r => r.BalanceDue > 0).OrderBy(r => r.StayFrom).ThenBy(r => r.Id).ToList();
		var balance = due.Sum(r => r.BalanceDue);
		if (balance <= 0)
			throw DomainException.Unprocessable(BookingErrors.NothingToPay);
		if (request.Amount > balance)
			throw DomainException.Conflict(BookingErrors.PaymentExceedsBalance, new Dictionary<string, object?> { ["balanceDue"] = balance });

		var shares = new List<(Reservation Reservation, decimal Amount)>();
		var left = request.Amount;
		for (var i = 0; i < due.Count; i++)
		{
			var amount = request.Amount == balance ? due[i].BalanceDue
				: i == due.Count - 1 ? left
				: Math.Min(due[i].BalanceDue, decimal.Round(request.Amount * due[i].BalanceDue / balance, 2, MidpointRounding.AwayFromZero));
			left -= amount;
			if (amount > 0)
				shares.Add((due[i], amount));
		}

		foreach (var (reservation, amount) in shares)
			await _payments.RecordAsync(new ReservationPaymentInput(
				reservation.PropertyId, reservation.PublicId, amount, request.Mode, request.IsDeposit, request.PayerCustomerId ?? reservation.CustomerId,
				reservation.StayFrom, reservation.StayTo), ct);
		await RecordAsync(AuditActionCodes.Booking.GroupPaymentRecorded, group, new Dictionary<string, object?>
		{
			["amount"] = request.Amount, ["mode"] = request.Mode, ["deposit"] = request.IsDeposit,
			["shares"] = shares.Select(s => new { reservation = s.Reservation.BookingNumber, amount = s.Amount }).ToList(),
		}, ct);
		return new GroupPaymentResultDto(
			shares.Select(s => new GroupPaymentShareDto(s.Reservation.PublicId, s.Reservation.BookingNumber, s.Amount)).ToList(),
			await ToDtoAsync(group, ct));
	}

	// ----- Supporto -----

	private async Task<BookingGroup> LoadAsync(Guid id, CancellationToken ct)
		=> await _groups.GetAsync(_scope.PropertyId, id, ct) ?? throw DomainException.NotFound(BookingErrors.GroupNotFound);

	/// <summary>Prenotazioni che possono entrare nel gruppo e quelle scartate con il motivo.</summary>
	private async Task<(List<Reservation> Accepted, List<BulkFailure> Failed)> SelectMembersAsync(
		IReadOnlyList<Guid> ids, BookingGroup? group, CancellationToken ct)
	{
		var wanted = ids.Distinct().ToList();
		var found = (await _reservations.GetManyAsync(_scope.PropertyId, wanted, false, ct)).ToDictionary(r => r.PublicId);
		var memberships = await _groups.GroupsOfAsync(found.Values.Select(r => r.Id).ToList(), ct);
		var accepted = new List<Reservation>();
		var failed = new List<BulkFailure>();
		foreach (var id in wanted)
		{
			if (!found.TryGetValue(id, out var reservation) || !await _spots.CanSeeAsync(reservation, ct))
				failed.Add(new BulkFailure(id, BookingErrors.ReservationNotFound));
			else if (reservation.Status == ReservationStatus.Cancelled)
				failed.Add(new BulkFailure(id, BookingErrors.ReservationCancelled));
			else if (memberships.TryGetValue(reservation.Id, out var other))
			{
				if (group is null || other.Id != group.Id)
					failed.Add(new BulkFailure(id, BookingErrors.AlreadyInGroup, new Dictionary<string, object?> { ["groupId"] = other.PublicId, ["groupName"] = other.Name }));
			}
			else
				accepted.Add(reservation);
		}
		return (accepted, failed);
	}

	private async Task<GroupDto> ToDtoAsync(BookingGroup group, CancellationToken ct)
	{
		var reservations = group.LiveMembers.Select(m => m.Reservation).Where(r => r.DeletedAt is null)
			.OrderBy(r => r.StayFrom).ThenBy(r => r.BookingNumber).ToList();
		var cards = await _reservationService.CardsAsync(reservations.Select(r => r.CustomerId), ct);
		var amounts = _scope.CanSeeAmounts;
		var active = reservations.Where(r => r.IsActive).ToList();
		return new GroupDto(
			group.PublicId, group.Name, group.CreatedAt, group.CreatedByUserId,
			reservations.Select(r => ReservationService.Summary(r, cards, amounts)).ToList(),
			amounts ? active.Sum(r => r.Total) : null, amounts ? active.Sum(r => r.BalanceDue) : null);
	}

	private Task RecordAsync(string action, BookingGroup group, IReadOnlyDictionary<string, object?> metadata, CancellationToken ct)
		=> _audit.RecordAsync(action, group.PropertyId, AuditActionCodes.Booking.Targets.Group, group.PublicId, metadata, ct: ct);
}
