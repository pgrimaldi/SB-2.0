using Module.Booking.Application.Abstractions;
using Module.Booking.Domain;
using Module.Booking.Domain.Entities;
using Module.Booking.Domain.Enums;
using Shared.Contracts.Booking;
using Shared.Contracts.Events;

namespace Module.Booking.Application.Services;

/// <summary>
/// I giorni ceduti (assenze e sharing) visti dalle altre operazioni sulle prenotazioni:
/// <list type="bullet">
/// <item>chi prenota una postazione in un giorno ceduto lo «riprende» e il titolare matura il credito;</item>
/// <item>chi l'aveva ripreso e lo toglie lo rimette a disposizione, con lo storno del credito;</item>
/// <item>il titolare che cambia o toglie la riga perde i giorni ceduti non ancora ripresi;</item>
/// <item>i giorni di un'assenza con «ricalcola prezzo» non si pagano.</item>
/// </list>
/// Gli eventi (voucher, ricarica della tessera) partono dopo il salvataggio, con <see cref="PublishPendingAsync"/>.
/// </summary>
public sealed class SharingLedger
{
	private readonly IResaleRepository _resale;
	private readonly IIntegrationEventPublisher _events;
	private readonly BookingScope _scope;
	private readonly List<IIntegrationEvent> _pending = [];
	private ResaleSettings? _settings;
	private bool _settingsLoaded;

	public SharingLedger(IResaleRepository resale, IIntegrationEventPublisher events, BookingScope scope)
	{
		_resale = resale;
		_events = events;
		_scope = scope;
	}

	public async Task<ResaleSettings?> SettingsAsync(Guid propertyId, CancellationToken ct = default)
	{
		if (!_settingsLoaded)
		{
			_settings = await _resale.GetSettingsAsync(propertyId, ct);
			_settingsLoaded = true;
		}
		return _settings;
	}

	/// <summary>Giorni da non far pagare, per postazione: assenze con «ricalcola prezzo».</summary>
	public async Task<IReadOnlySet<(Guid SpotId, DateOnly Day)>> ExcludedDaysAsync(Reservation reservation, CancellationToken ct = default)
		=> reservation.Id == 0
			? new HashSet<(Guid, DateOnly)>()
			: (await _resale.ListBySourceAsync([reservation.Id], ct)).Where(o => o.ExcludesFromPrice).Select(o => (o.ResourceId, o.OfferDate)).ToHashSet();

	/// <summary>Giorni (riga, giorno) che il titolare ha ceduto e che non vanno rioccupati.</summary>
	public async Task<IReadOnlySet<(long LineId, DateOnly Day)>> CededDaysAsync(Reservation reservation, CancellationToken ct = default)
		=> reservation.Id == 0
			? new HashSet<(long, DateOnly)>()
			: (await _resale.ListBySourceAsync([reservation.Id], ct)).Where(o => o.BookingLineId is not null)
				.Select(o => (o.BookingLineId!.Value, o.OfferDate)).ToHashSet();

	/// <summary>
	/// Righe nuove: i giorni ceduti sulla stessa postazione, nella stessa fascia, passano a questa prenotazione e il
	/// titolare matura il credito.
	/// </summary>
	public async Task ClaimAsync(Reservation taker, IReadOnlyCollection<BookingLine> lines, CancellationToken ct = default)
	{
		var withSpot = lines.Where(l => l.SpotId is not null && !l.IsArea).ToList();
		if (withSpot.Count == 0)
			return;
		var offers = await _resale.ListAvailableAsync(taker.PropertyId, withSpot.Select(l => l.SpotId!.Value).Distinct().ToList(),
			withSpot.Min(l => l.StartDate), withSpot.Max(l => l.EndDate), ct);
		foreach (var offer in offers)
		{
			if (offer.SourceReservationId == taker.Id && taker.Id != 0)
				continue;
			var line = withSpot.FirstOrDefault(l => l.SpotId == offer.ResourceId && l.Covers(offer.OfferDate) && SpotKinds.Overlaps(l.BookingPeriod, offer.OfferedPeriod));
			if (line is null)
				continue;
			offer.Reassign(taker, line.BookingPeriod, _scope.UtcNow);
			await CreditAsync(offer, ct);
		}
	}

	/// <summary>
	/// Righe tolte (modifica, spostamento, cestino): come titolare i giorni ceduti non ancora ripresi si ritirano; come
	/// chi li aveva ripresi tornano a disposizione, con lo storno del credito.
	/// </summary>
	public async Task LinesRemovedAsync(Reservation reservation, IReadOnlyCollection<BookingLine> lines, CancellationToken ct = default)
	{
		if (reservation.Id == 0 || lines.Count == 0)
			return;
		var ids = lines.Select(l => l.Id).ToHashSet();
		foreach (var offer in await _resale.ListBySourceAsync([reservation.Id], ct))
			if (offer.BookingLineId is { } lid && ids.Contains(lid) && offer.OfferState == ResaleOfferState.Available)
				offer.Withdraw();
		foreach (var offer in await _resale.ListTakenByAsync(reservation.Id, ct))
		{
			if (!lines.Any(l => l.SpotId == offer.ResourceId && l.Covers(offer.OfferDate)))
				continue;
			await ReverseAsync(offer, ct);
			offer.ReturnToAvailable();
		}
	}

	public async Task PublishPendingAsync(CancellationToken ct = default)
	{
		var events = _pending.ToList();
		_pending.Clear();
		foreach (var e in events)
			switch (e)
			{
				case ResaleCreditEarned earned: await _events.PublishAsync(earned, ct); break;
				case ResaleCreditReversed reversed: await _events.PublishAsync(reversed, ct); break;
			}
	}

	private async Task CreditAsync(ResaleOffer offer, CancellationToken ct)
	{
		var settings = await SettingsAsync(offer.PropertyId, ct);
		if (!offer.EarnsCredit || settings is not { IsEnabled: true, GivesCredit: true } || offer.SourceReservation.CustomerId is not { } customer)
			return;
		if (settings.ManualCreditEnabled && settings.ManualCreditUnitsPerDay > 0)
		{
			var (_, _, ever) = offer.Id == 0 ? (0, 0m, false) : await _resale.CreditOfAsync(offer.Id, ct);
			_resale.AddCredit(ResaleCreditLedgerEntry.Earned(offer, customer, settings.ManualCreditUnitsPerDay,
				settings.ManualCreditUnitsPerDay * settings.ManualCreditAmount, _scope.UtcNow, again: ever));
		}
		if (settings.VoucherCreditEnabled || settings.CardTopUpEnabled)
			_pending.Add(new ResaleCreditEarned(Guid.CreateVersion7(), _scope.UtcNow, offer.PropertyId, customer, offer.SourceReservation.PublicId,
				offer.PublicId, offer.OfferDate, settings.VoucherCreditEnabled ? settings.VoucherCreditAmount : null,
				settings.CardTopUpEnabled ? settings.CardTopUpAmount : null));
		offer.MarkCreditSettled();
	}

	private async Task ReverseAsync(ResaleOffer offer, CancellationToken ct)
	{
		if (!offer.CreditSettled || offer.SourceReservation.CustomerId is not { } customer)
			return;
		var (units, amount, _) = await _resale.CreditOfAsync(offer.Id, ct);
		if (units != 0 || amount != 0)
			_resale.AddCredit(ResaleCreditLedgerEntry.Reversal(offer, customer, units, amount, _scope.UtcNow));
		var settings = await SettingsAsync(offer.PropertyId, ct);
		if (settings is { VoucherCreditEnabled: true } or { CardTopUpEnabled: true })
			_pending.Add(new ResaleCreditReversed(Guid.CreateVersion7(), _scope.UtcNow, offer.PropertyId, customer, offer.SourceReservation.PublicId,
				offer.PublicId, offer.OfferDate, settings.VoucherCreditEnabled ? settings.VoucherCreditAmount : null,
				settings.CardTopUpEnabled ? settings.CardTopUpAmount : null));
	}
}
