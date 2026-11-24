using Shared.Dtos;

namespace Module.Booking.Application.Dtos;

// ----- Gruppi -----

/// <summary>Nuovo gruppo con le prenotazioni scelte (anche di clienti diversi); ognuna può stare in un solo gruppo.</summary>
public sealed record CreateGroupRequest(string Name, IReadOnlyList<Guid> ReservationIds);

public sealed record RenameGroupRequest(string Name);

public sealed record GroupMembersRequest(IReadOnlyList<Guid> ReservationIds);

/// <summary>
/// «Pagamento unico» del gruppo: un incasso per ogni prenotazione, registrati insieme. Se l'importo copre tutto ogni
/// prenotazione riceve il suo residuo, altrimenti si divide in proporzione ai residui. Può essere anche un acconto.
/// </summary>
/// <param name="PayerCustomerId">Cliente che paga, fra quelli del gruppo; senza, il cliente di ogni prenotazione.</param>
/// <param name="ReservationIds">Solo queste prenotazioni del gruppo; senza, tutte.</param>
public sealed record GroupPaymentRequest(decimal Amount, string Mode, bool IsDeposit, Guid? PayerCustomerId, IReadOnlyList<Guid>? ReservationIds);

/// <summary>Il gruppo di una prenotazione, nel suo dettaglio.</summary>
public sealed record ReservationGroupDto(Guid Id, string Name, int ReservationCount);

public sealed record GroupSummaryDto(Guid Id, string Name, int ReservationCount, IReadOnlyList<string> CustomerNames, DateTime CreatedAt);

/// <param name="Total">Somma dei totali delle prenotazioni; null per chi non vede gli importi.</param>
public sealed record GroupDto(
	Guid Id, string Name, DateTime CreatedAt, Guid? CreatedBy, IReadOnlyList<ReservationSummaryDto> Reservations, decimal? Total, decimal? BalanceDue);

/// <summary>Esito di una modifica dei membri: le prenotazioni non aggiunte con il motivo.</summary>
public sealed record GroupChangeDto(GroupDto Group, IReadOnlyList<BulkFailure> Failed);

public sealed record GroupPaymentShareDto(Guid ReservationId, string BookingNumber, decimal Amount);

public sealed record GroupPaymentResultDto(IReadOnlyList<GroupPaymentShareDto> Shares, GroupDto Group);

// ----- Rinnovo, dividi conto, scambio -----

/// <summary>Una prenotazione rinnovata per la stagione successiva.</summary>
public sealed record RenewedDto(Guid SourceId, Guid ReservationId, string BookingNumber, decimal? Total);

public sealed record RenewResultDto(IReadOnlyList<RenewedDto> Succeeded, IReadOnlyList<BulkFailure> Failed);

/// <summary>Quota di una postazione in «Dividi conto» con assegnazione a mano.</summary>
/// <param name="Paid">Parte dell'incassato che passa a questa postazione; se manca per tutte, in proporzione alle quote.</param>
public sealed record SplitShareInput(Guid SpotId, decimal Total, decimal? Paid);

/// <summary>«Dividi conto»: una prenotazione per postazione.</summary>
/// <param name="Mode">proportional (predefinito: in proporzione al prezzo di ogni postazione) o manual.</param>
public sealed record SplitReservationRequest(string? Mode, IReadOnlyList<SplitShareInput>? Shares);

/// <summary>La prenotazione di partenza (con la prima postazione) seguita da quelle nuove.</summary>
public sealed record SplitResultDto(IReadOnlyList<ReservationDto> Reservations);

/// <summary>Scambio di postazione fra due prenotazioni, solo nei giorni in comune (eventualmente ristretti).</summary>
/// <param name="LineId">Riga della prenotazione indicata nel percorso.</param>
/// <param name="WithLineId">Riga dell'altra prenotazione.</param>
public sealed record SwapReservationsRequest(Guid LineId, Guid WithLineId, DateOnly? From, DateOnly? To);

public sealed record SwapResultDto(ReservationDto First, ReservationDto Second, DateOnly From, DateOnly To);

// ----- Ricerche sulla mappa -----

public sealed record DateRangeDto(DateOnly From, DateOnly To);

/// <param name="Status">free, partial, occupied.</param>
public sealed record SpotFreeRangesDto(Guid SpotId, string? Label, Guid? SectorId, string SpotType, string Status, IReadOnlyList<DateRangeDto> FreeRanges);

public sealed record FreeRangesDto(DateOnly From, DateOnly To, IReadOnlyList<SpotFreeRangesDto> Spots);

/// <summary>Filtri della ricerca guidata del planner.</summary>
/// <param name="SpotType">Tipo di postazione («articolo»): umbrella, sunbed…</param>
/// <param name="MaxMoves">Numero massimo di cambi di postazione.</param>
/// <param name="NearSpotId">A parità di cambi, le postazioni più vicine a questa (meno caselle di distanza).</param>
public sealed record GuidedSearchQuery(
	DateOnly From, DateOnly To, string? DayPart, string? SpotType, Guid? SectorId, int? MaxMoves, Guid? NearSpotId);

public sealed record GuidedSegmentDto(Guid SpotId, string? Label, Guid? SectorId, DateOnly From, DateOnly To);

/// <param name="Moves">Cambi di postazione (tratti meno uno).</param>
public sealed record GuidedSolutionDto(int Moves, IReadOnlyList<GuidedSegmentDto> Segments);

/// <summary>Fino a due alternative, con meno cambi di postazione prima.</summary>
public sealed record GuidedSearchDto(IReadOnlyList<GuidedSolutionDto> Solutions);

/// <summary>«Prenotazione ripetuta»: i giorni della settimana scelti, nel periodo, su una postazione.</summary>
/// <param name="Weekdays">1 = lunedì … 7 = domenica.</param>
/// <param name="ExcludeReservationId">La prenotazione che si sta modificando: i suoi giorni non contano come occupati.</param>
public sealed record RepeatPlanRequest(Guid SpotId, DateOnly From, DateOnly To, IReadOnlyList<int> Weekdays, string? DayPart, Guid? ExcludeReservationId);

/// <summary>Un blocco di giorni consecutivi; i giorni già occupati li segnala, la scelta resta al gestore.</summary>
public sealed record RepeatBlockDto(DateOnly From, DateOnly To, IReadOnlyList<DateOnly> BusyDays);

public sealed record RepeatPlanDto(Guid SpotId, IReadOnlyList<RepeatBlockDto> Blocks);

/// <summary>Riserva di una postazione per un periodo suo (ricerca smart: una riserva per postazione).</summary>
public sealed record HoldSegmentInput(Guid SpotId, DateOnly From, DateOnly To);

// ----- Sharing e assenze -----

/// <summary>Impostazioni › Sharing: tre modalità di credito indipendenti, importi in euro.</summary>
/// <param name="MinBookingDays">Giorni minimi della prenotazione; null = nessun minimo.</param>
/// <param name="SeasonalOnly">Solo sulle prenotazioni stagionali.</param>
/// <param name="AllArticles">Su tutti gli articoli; senza, solo sugli ombrelloni.</param>
/// <param name="AbsencesAsSharing">Le assenze programmate valgono come sharing (il giorno ripreso fa maturare il credito).</param>
public sealed record SharingSettingsDto(
	bool IsEnabled, int? MinBookingDays, bool SeasonalOnly, bool AllArticles, bool AbsencesAsSharing,
	bool ManualCreditEnabled, int ManualCreditUnitsPerDay, decimal ManualCreditAmount,
	bool VoucherCreditEnabled, decimal VoucherCreditAmount, bool CardTopUpEnabled, decimal CardTopUpAmount);

/// <summary>Giorni ceduti da una postazione della prenotazione.</summary>
/// <param name="Kind">absence (assenza programmata) o sharing.</param>
/// <param name="DayPart">Fascia ceduta: full_day (predefinita), morning, afternoon; mezza giornata solo su una riga di giornata intera.</param>
/// <param name="RecalculatePrice">Solo per le assenze: i giorni non si pagano più (il gestore sceglie, anche su una prenotazione pagata).</param>
public sealed record ReleaseDaysRequest(string Kind, Guid LineId, IReadOnlyList<DateOnly> Dates, string? DayPart, bool RecalculatePrice);

/// <summary>Ritiro di giorni ceduti non ancora ripresi.</summary>
public sealed record WithdrawDaysRequest(IReadOnlyList<Guid> OfferIds);

/// <summary>Un giorno ceduto di una riga della prenotazione.</summary>
/// <param name="Kind">absence o sharing.</param>
/// <param name="State">available (ancora disponibile), reassigned (ripreso da un altro cliente).</param>
public sealed record ReleasedDayDto(Guid OfferId, DateOnly Date, string DayPart, string Kind, string State, bool ExcludesFromPrice, Guid? TakenByReservationId);

/// <summary>Una riga della pagina «Sharing creati»: i giorni ceduti da una postazione di una prenotazione.</summary>
/// <param name="Status">available se almeno un giorno è ancora disponibile, completed se sono stati ripresi tutti.</param>
public sealed record SharingRowDto(
	Guid ReservationId, string BookingNumber, string? CustomerName, Guid? LineId, string? SpotLabel, string Kind, string Status,
	IReadOnlyList<DateOnly> Dates, IReadOnlyList<DateOnly> AvailableDates, IReadOnlyList<Guid> AvailableOfferIds);

/// <summary>Filtri della pagina «Sharing creati».</summary>
/// <param name="Kind">sharing (predefinito), absence, all.</param>
/// <param name="Status">available, completed.</param>
public sealed record SharingQuery(DateOnly? From, DateOnly? To, string? Kind, string? Status, string? Search, int Page = 1, int PageSize = 25);

public sealed record CreditEntryDto(DateTime OccurredAt, string Type, int Units, decimal Amount, string? BookingNumber, string? Note);

/// <summary>Crediti sharing di un cliente: saldo e movimenti.</summary>
public sealed record CustomerCreditsDto(Guid CustomerId, int Units, decimal Amount, IReadOnlyList<CreditEntryDto> Entries);

/// <summary>Correzione del gestore: crediti usati (valori negativi) o aggiunti.</summary>
public sealed record CreditAdjustmentRequest(Guid CustomerId, int Units, decimal Amount, string? Note);

/// <summary>Un giorno ceduto visibile sulla mappa del giorno: la postazione è libera e si può riprendere.</summary>
public sealed record MapReleasedDto(Guid SpotId, Guid OfferId, string DayPart, string Kind, string State, Guid ReservationId, string BookingNumber);

/// <summary>Risposta già data a un comando con la stessa chiave di idempotenza.</summary>
public sealed record StoredCommandResponse(string Method, string Path, int StatusCode, string? Body);

/// <summary>Una riga rimasta senza postazione dopo «Reimposta mappa», da sistemare a mano.</summary>
public sealed record OrphanLineDto(
	Guid ReservationId, string BookingNumber, string? CustomerName, Guid LineId, string? SpotLabel, string? SpotType, short? Row, short? Column,
	DateOnly From, DateOnly To);
