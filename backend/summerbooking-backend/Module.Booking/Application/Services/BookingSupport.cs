using Module.Booking.Application.Abstractions;
using Module.Booking.Domain;
using Module.Booking.Domain.Entities;
using Shared.Contracts.Audit;
using Shared.Contracts.Catalog;
using Shared.Contracts.Errors;
using Shared.Contracts.Events;
using Shared.Contracts.Identity;
using Shared.Infrastructure;
using Shared.Localization;
using Shared.Security;

namespace Module.Booking.Application.Services;

/// <summary>Stabilimento e utente della richiesta: sempre dal token, mai dal corpo o dalla querystring.</summary>
public sealed class BookingScope
{
	private readonly ICurrentUser _user;
	private readonly IDateTimeProvider _clock;

	public BookingScope(ICurrentUser user, IDateTimeProvider clock)
	{
		_user = user;
		_clock = clock;
	}

	private Guid? _property;

	/// <summary>Lo stabilimento della richiesta: quello scelto dal sito o dall'area cliente, altrimenti quello del token.</summary>
	public Guid PropertyId => _property ?? _user.PropertyId ?? throw DomainException.Forbidden(BookingErrors.PropertyRequired);

	/// <summary>Prenotazione online: lo stabilimento viene dall'indirizzo pubblico, non dal token.</summary>
	public void UseProperty(Guid propertyId) => _property = propertyId;

	public bool IsAuthenticated => _user.IsAuthenticated;

	public bool IsCustomer => _user.Role == RoleCodes.Consumer;

	public Guid? UserId => _user.UserId;

	public Guid RequiredUserId => _user.UserId ?? throw DomainException.Forbidden(BookingErrors.PropertyRequired);

	public bool CanSeeAmounts => _user.HasPermission(Permissions.BookingsAmounts);

	public bool CanSetPrices => _user.HasPermission(Permissions.BookingsPricing);

	public DateTime UtcNow => _clock.UtcNow;

	/// <summary>Oggi in Italia: ingressi, check-out e «primo giorno» seguono il giorno civile dello stabilimento.</summary>
	public DateOnly Today => DateOnly.FromDateTime(ItalianTime.FromUtc(_clock.UtcNow));
}

/// <summary>Scrittura nel registro delle operazioni sulle prenotazioni (categoria operator).</summary>
public sealed class BookingAudit
{
	private readonly IAuditTrail _audit;
	private readonly ICurrentUser _user;
	private readonly IIntegrationEventPublisher _events;
	private readonly IDateTimeProvider _clock;

	public BookingAudit(IAuditTrail audit, ICurrentUser user, IIntegrationEventPublisher events, IDateTimeProvider clock)
	{
		_audit = audit;
		_user = user;
		_events = events;
		_clock = clock;
	}

	/// <summary>Attività per la campanella del gestionale.</summary>
	public Task ActivityAsync(string kind, Reservation reservation, IReadOnlyDictionary<string, object?>? details = null, CancellationToken ct = default)
		=> _events.PublishAsync(new Shared.Contracts.Booking.BookingActivity(
			Guid.CreateVersion7(), _clock.UtcNow, reservation.PropertyId, kind, reservation.PublicId, reservation.BookingNumber, _user.UserId, details), ct);

	public Task RecordAsync(string action, Reservation reservation, IReadOnlyDictionary<string, object?>? metadata = null, string? spotLabel = null, CancellationToken ct = default)
		=> _audit.RecordAsync(new AuditEntry(
			AuditCategory.Operator, AuditActionCodes.Booking.Module, action, PropertyId: reservation.PropertyId,
			TargetType: AuditActionCodes.Booking.Targets.Reservation, TargetId: reservation.PublicId, Metadata: metadata,
			BookingNumberSnapshot: reservation.BookingNumber, SpotLabelSnapshot: spotLabel), ct);

	public Task RecordAsync(string action, Guid propertyId, string targetType, Guid? targetId, IReadOnlyDictionary<string, object?>? metadata = null,
		string? bookingNumber = null, string? spotLabel = null, CancellationToken ct = default)
		=> _audit.RecordAsync(new AuditEntry(
			AuditCategory.Operator, AuditActionCodes.Booking.Module, action, PropertyId: propertyId,
			TargetType: targetType, TargetId: targetId, Metadata: metadata, ActorKind: _user.IsAuthenticated ? null : AuditActorKind.System,
			BookingNumberSnapshot: bookingNumber, SpotLabelSnapshot: spotLabel), ct);
}

/// <summary>
/// Postazioni della mappa per la richiesta (lette da Catalog una volta sola) e restrizione dell'affiliato: chi ha
/// postazioni assegnate vede e prenota solo quelle. Le assegnazioni del vecchio gestionale sono posizioni
/// (<c>R{fila}C{colonna}</c>) e valgono come tali finché non diventano id.
/// </summary>
public sealed class SpotDirectory
{
	private readonly IBookingCatalog _catalog;
	private readonly IUserDirectory _users;
	private readonly BookingScope _scope;
	private Dictionary<Guid, BookableSpot>? _spots;
	private Func<BookableSpot, bool>? _allowed;

	public SpotDirectory(IBookingCatalog catalog, IUserDirectory users, BookingScope scope)
	{
		_catalog = catalog;
		_users = users;
		_scope = scope;
	}

	public async Task<IReadOnlyDictionary<Guid, BookableSpot>> AllAsync(CancellationToken ct = default)
		=> _spots ??= (await _catalog.ListSpotsAsync(_scope.PropertyId, ct)).ToDictionary(s => s.Id);

	/// <summary>Postazione prenotabile dall'utente: 404 se non c'è, 422 se non è prenotabile, 403 se non è fra le sue.</summary>
	public async Task<BookableSpot> RequireBookableAsync(Guid spotId, CancellationToken ct = default)
	{
		var spots = await AllAsync(ct);
		if (!spots.TryGetValue(spotId, out var spot))
			throw DomainException.NotFound(BookingErrors.SpotNotFound);
		if (!spot.IsBookable)
			throw DomainException.Unprocessable(BookingErrors.SpotNotBookable, new Dictionary<string, object?> { ["spotId"] = spotId, ["label"] = spot.Label });
		if (!await IsAllowedAsync(spot, ct))
			throw DomainException.Forbidden(BookingErrors.SpotNotAllowed);
		return spot;
	}

	public async Task<bool> IsAllowedAsync(BookableSpot spot, CancellationToken ct = default)
	{
		if (_allowed is null)
		{
			var scope = _scope.UserId is { } user ? await _users.GetResourceScopeAsync(user, ct) : new UserResourceScope(false, []);
			if (!scope.IsRestricted)
				_allowed = _ => true;
			else
			{
				var keys = scope.Scopes.ToHashSet(StringComparer.OrdinalIgnoreCase);
				_allowed = s => keys.Contains(s.Id.ToString()) || (s.Row is { } r && s.Column is { } c && keys.Contains($"R{r}C{c}"));
			}
		}
		return _allowed(spot);
	}

	/// <summary>Postazioni visibili all'utente (tutte, o solo le assegnate all'affiliato).</summary>
	public async Task<IReadOnlySet<Guid>> VisibleIdsAsync(CancellationToken ct = default)
	{
		var visible = new HashSet<Guid>();
		foreach (var spot in (await AllAsync(ct)).Values)
			if (await IsAllowedAsync(spot, ct))
				visible.Add(spot.Id);
		return visible;
	}

	/// <summary>L'affiliato vede solo le prenotazioni che hanno almeno una delle sue postazioni.</summary>
	public async Task<bool> CanSeeAsync(Reservation reservation, CancellationToken ct = default)
	{
		var spots = reservation.Lines.Where(l => l.DeletedAt is null && l.SpotId is not null).Select(l => l.SpotId!.Value).ToList();
		if (spots.Count == 0)
			return true;
		var visible = await VisibleIdsAsync(ct);
		return spots.Any(visible.Contains);
	}

	public static SpotSnapshot Snapshot(BookableSpot s) => new(s.Id, s.Label, s.SpotType, s.IsArea, s.SectorId, s.SectorName, s.Row, s.Column);
}

/// <summary>Numero di prenotazione (prefisso dello stabilimento + progressivo) e codice prenotazione casuale.</summary>
public sealed class ReservationNumbers
{
	private const int CodeLength = 8;
	private const string CodeAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

	private readonly IReservationRepository _reservations;
	private readonly IBookingCatalog _catalog;

	public ReservationNumbers(IReservationRepository reservations, IBookingCatalog catalog)
	{
		_reservations = reservations;
		_catalog = catalog;
	}

	public async Task<string> NextNumberAsync(Guid propertyId, CancellationToken ct = default)
	{
		var prefix = await _catalog.FindNumberPrefixAsync(propertyId, ct) ?? throw DomainException.NotFound(BookingErrors.PropertyNotFound);
		return await _reservations.NextBookingNumberAsync(propertyId, prefix, ct);
	}

	public async Task<string> NewCodeAsync(CancellationToken ct = default)
	{
		for (var attempt = 0; attempt < 5; attempt++)
		{
			var code = System.Security.Cryptography.RandomNumberGenerator.GetString(CodeAlphabet, CodeLength);
			if (!await _reservations.ReservationCodeExistsAsync(code, ct))
				return code;
		}
		throw new InvalidOperationException("Codice di prenotazione: nessun valore libero dopo 5 tentativi.");
	}
}
