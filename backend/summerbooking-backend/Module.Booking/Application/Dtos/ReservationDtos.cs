namespace Module.Booking.Application.Dtos;

/// <summary>
/// Cliente della prenotazione: un contatto esistente (<paramref name="CustomerId"/>), oppure i dati di uno nuovo, oppure
/// l'utente generico (nessun contatto). Con il solo nome si crea sempre un contatto nuovo; con email o telefono si riusa
/// quello che coincide.
/// </summary>
public sealed record CustomerInput(Guid? CustomerId, string? FullName, string? Email, string? PhoneCountryCode, string? Phone, bool IsGeneric);

/// <param name="Type">percentage o amount.</param>
/// <param name="Label">Solo per l'incremento: etichetta libera, riga a sé sullo scontrino.</param>
public sealed record AdjustmentInput(string Type, decimal Value, string? Label);

/// <param name="Dates">Solo questi giorni; vuoto = tutto il soggiorno della postazione.</param>
public sealed record AdditionalInput(Guid PriceListId, short Quantity, IReadOnlyList<DateOnly>? Dates);

/// <summary>
/// Una postazione della prenotazione. Senza <paramref name="From"/>/<paramref name="To"/> valgono le date della
/// prenotazione; con date proprie la stessa prenotazione può passare da una postazione all'altra.
/// </summary>
/// <param name="AgreementPartnerId">Struttura convenzionata, con il suo listino o voucher.</param>
/// <param name="ManualPrice">Prezzo del pacchetto scritto a mano; cambiando le date si perde.</param>
/// <param name="Seats">Posti occupati, solo per le aree.</param>
public sealed record ReservationSpotInput(
	Guid SpotId, DateOnly? From, DateOnly? To, Guid? PriceListId, Guid? AgreementPartnerId, Guid? AgreementPriceListId, Guid? VoucherPriceListId,
	decimal? ManualPrice, short? Seats, IReadOnlyList<AdditionalInput>? Additionals);

/// <summary>Un servizio dell'ordine: bar, standard o orario.</summary>
/// <param name="Date">Giorno di utilizzo; senza, il primo giorno della prenotazione. Obbligatorio per i servizi orari.</param>
/// <param name="TimeSlotId">Servizi orari: la durata scelta (id delle durate del servizio in Catalog).</param>
/// <param name="StartTime">Servizi orari: l'ora di inizio.</param>
/// <param name="ManualPrice">Importo scritto a mano per la riga (prezzo × quantità); serve il permesso sui prezzi.</param>
/// <param name="SpotId">Servizio legato a una postazione della prenotazione.</param>
public sealed record ServiceInput(
	Guid ServiceId, short Quantity, string? Variant, DateOnly? Date, Guid? TimeSlotId, TimeOnly? StartTime, decimal? ManualPrice, Guid? SpotId);

/// <summary>Un tavolo: servizio tavoli, giorno, inizio della fascia, coperti.</summary>
/// <param name="TableSeats">Tavoli scelti a mano (posti di ciascuno); senza, si assegna il tavolo con i posti uguali o più vicini per eccesso.</param>
public sealed record TableInput(Guid TableServiceId, DateOnly Date, TimeOnly SlotStart, short Covers, IReadOnlyList<short>? TableSeats);

/// <summary>Creazione o modifica di una prenotazione dal gestionale: postazioni, servizi, tavoli.</summary>
/// <param name="DayPart">full_day (predefinito), morning, afternoon.</param>
/// <param name="PriceOverride">Prezzo applicato (totale) scritto a mano: vince su tutto e resta anche cambiando le date.</param>
/// <param name="ConfirmBlacklisted">Il gestore conferma di prenotare per un cliente in blacklist.</param>
/// <param name="Spots">Postazioni; vuoto = prenotazione di soli servizi o tavoli.</param>
/// <param name="ConfirmCapacity">Il gestore conferma di superare i limiti dei servizi o dei tavoli.</param>
public sealed record SaveReservationRequest(
	CustomerInput? Customer, DateOnly From, DateOnly To, string? DayPart, bool Seasonal, IReadOnlyList<ReservationSpotInput> Spots,
	string? DiscountCode, AdjustmentInput? ManualDiscount, AdjustmentInput? Surcharge, decimal? PriceOverride, string? Note,
	IReadOnlyList<string>? CustomerTags, IReadOnlyList<string>? Guests, bool ConfirmBlacklisted, IReadOnlyList<ServiceInput>? Services = null,
	IReadOnlyList<TableInput>? Tables = null, bool ConfirmCapacity = false);

/// <summary>Creazione: come la modifica, più la spunta «Pagato» (incasso del totale con il metodo scelto).</summary>
/// <param name="PaidWith">cash, credit_card, bank_transfer, other; null = non pagata.</param>
public sealed record CreateReservationRequest(SaveReservationRequest Reservation, string? PaidWith);

/// <summary>Spostamento di una postazione, anche solo per alcuni giorni (la riga si spezza).</summary>
public sealed record MoveReservationRequest(Guid LineId, Guid ToSpotId, DateOnly? From, DateOnly? To);

/// <param name="Mode">cash, credit_card, bank_transfer, other.</param>
/// <param name="ConfirmOverBalance">Il gestore conferma un incasso oltre il residuo.</param>
public sealed record RecordPaymentRequest(decimal Amount, string Mode, bool IsDeposit, bool ConfirmOverBalance);

public sealed record MarkPaidRequest(string Mode);

/// <summary>Eliminazione (nel cestino): una scelta sola per tutte. Con il rimborso l'annullo degli scontrini è obbligatorio.</summary>
public sealed record DeleteReservationsRequest(IReadOnlyList<Guid> Ids, bool Refund, bool VoidReceipts);

public sealed record CleanupMoveRequest(IReadOnlyList<Guid> Ids, string Password);

public sealed record EntranceRequest(Guid LineId);

/// <summary>Riserva di più postazioni con le stesse date, oppure (<paramref name="Segments"/>) ognuna con il suo periodo.</summary>
public sealed record CreateHoldsRequest(IReadOnlyList<Guid>? SpotIds, DateOnly From, DateOnly To, string? DayPart, IReadOnlyList<HoldSegmentInput>? Segments = null);

/// <summary>Riga della prenotazione: una postazione per un tratto di date.</summary>
public sealed record ReservationLineDto(
	Guid Id, string LineNumber, Guid? SpotId, string? SpotLabel, string? SpotType, Guid? SectorId, string? SectorName, DateOnly From, DateOnly To,
	string DayPart, short Seats, bool IsArea, Guid? PriceListId, string? PackageName, decimal? ManualPrice, decimal? Amount,
	Guid? AgreementPartnerId, Guid? AgreementPriceListId, Guid? VoucherPriceListId, IReadOnlyList<ReservationAdditionalDto> Additionals,
	bool EnteredToday, IReadOnlyList<ReleasedDayDto> ReleasedDays);

/// <param name="Kind">bar, normal, hourly, automatic_price.</param>
public sealed record ReservationServiceDto(
	Guid Id, Guid? ServiceId, string Kind, string Name, string? Variant, short Quantity, DateOnly? Date, TimeOnly? SlotStart, TimeOnly? SlotEnd,
	Guid? TimeSlotId, decimal? Amount, Guid? SpotId);

public sealed record ReservationTableDto(
	Guid Id, Guid TableServiceId, string Name, DateOnly Date, TimeOnly SlotStart, TimeOnly SlotEnd, short Covers, IReadOnlyList<short> TableSeats,
	decimal? DepositDue);

public sealed record ReservationAdditionalDto(Guid? PriceListId, string Name, short Quantity, IReadOnlyList<DateOnly> Dates, decimal? Amount);

public sealed record ReservationCustomerDto(Guid Id, string? FullName, string? Email, string? PhoneCountryCode, string? Phone, bool IsBlacklisted);

/// <summary>Importi: assenti per chi non vede i prezzi (bagnino).</summary>
public sealed record ReservationAmountsDto(
	decimal Subtotal, decimal CodeDiscount, string? DiscountCode, AdjustmentInput? ManualDiscount, decimal ManualDiscountAmount,
	AdjustmentInput? Surcharge, decimal SurchargeAmount, decimal? PriceOverride, decimal Total, decimal Deposit, decimal Paid, decimal BalanceDue,
	decimal CreditAmount, string? PrevalentPaymentMode, bool IsMarkedPaid);

/// <param name="Status">pending, confirmed, cancelled.</param>
/// <param name="PaymentStatus">paid, partial, unpaid.</param>
/// <param name="Channel">customer (online) o manager (gestionale).</param>
public sealed record ReservationDto(
	Guid Id, string BookingNumber, string ReservationCode, string Status, string? CancellationReason, string Channel, bool IsSeasonal,
	DateOnly? From, DateOnly? To, ReservationCustomerDto? Customer, string PaymentStatus, ReservationAmountsDto? Amounts, string? Note,
	DateTime? NoteUpdatedAt, Guid? NoteUpdatedBy, IReadOnlyList<string> CustomerTags, IReadOnlyList<string> Guests,
	IReadOnlyList<ReservationLineDto> Lines, DateTime? CheckedInAt, DateTime? CheckedOutAt, bool IsTrashed, DateTime? TrashedAt,
	DateTime CreatedAt, Guid? CreatedBy, DateTime UpdatedAt, IReadOnlyList<string> Warnings, ReservationGroupDto? Group, bool IsRenewal,
	bool IsSplitPart, string Kind, IReadOnlyList<ReservationServiceDto> Services, IReadOnlyList<ReservationTableDto> Tables, uint Version);

/// <summary>Una prenotazione negli elenchi (pagina Prenotazioni, Database, Cestino).</summary>
public sealed record ReservationSummaryDto(
	Guid Id, string BookingNumber, string Status, string Channel, bool IsSeasonal, DateOnly? From, DateOnly? To, Guid? CustomerId,
	string? CustomerName, IReadOnlyList<string> SpotLabels, Guid? AgreementPartnerId, string PaymentStatus, decimal? Total, decimal? BalanceDue,
	bool IsCheckedIn, bool IsTrashed, DateTime? TrashedAt, DateTime CreatedAt);

/// <summary>Esito di un'eliminazione: incassi rimborsati in cassa e quelli online da rimborsare a mano.</summary>
/// <param name="ReceiptsVoided">Scontrini annullati.</param>
/// <param name="ReceiptsVoidPending">Prenotazioni i cui scontrini restano da annullare (servizio non attivo o non raggiungibile).</param>
public sealed record DeleteResultDto(
	IReadOnlyList<Guid> Succeeded, IReadOnlyList<Shared.Dtos.BulkFailure> Failed, decimal Refunded, decimal RefundPendingOnline,
	int ReceiptsVoided = 0, int ReceiptsVoidPending = 0);

/// <summary>Una postazione occupata in un giorno della mappa.</summary>
/// <param name="Kind">reservation o hold.</param>
public sealed record MapOccupantDto(
	Guid SpotId, string DayPart, string Kind, Guid? ReservationId, Guid? LineId, Guid? HoldId, string? BookingNumber, string? CustomerName,
	string? PaymentStatus, decimal? BalanceDue, bool IsSeasonal, bool IsFirstDay, bool IsLastDay, bool IsCheckedIn, bool IsCheckedOut,
	bool EnteredToday, short Seats, bool HasNote);

/// <param name="Released">Giorni ceduti (assenze e sharing) di quel giorno: la postazione è libera.</param>
public sealed record MapDayDto(DateOnly Date, IReadOnlyList<MapOccupantDto> Occupants, IReadOnlyList<MapReleasedDto> Released);

public sealed record PlannerLineDto(
	Guid LineId, Guid ReservationId, Guid? SpotId, DateOnly From, DateOnly To, string DayPart, string BookingNumber, string? CustomerName,
	string Status, string PaymentStatus, bool IsSeasonal, short Seats);

public sealed record PlannerHoldDto(Guid HoldId, Guid SpotId, DateOnly From, DateOnly To, string DayPart);

public sealed record PlannerDto(DateOnly From, DateOnly To, IReadOnlyList<PlannerLineDto> Lines, IReadOnlyList<PlannerHoldDto> Holds);

/// <summary>Disponibilità di una postazione nell'intervallo richiesto.</summary>
/// <param name="Status"><c>free</c>, <c>held</c> (solo riserve) o <c>occupied</c>.</param>
/// <param name="BusyDays">Giorni occupati (da prenotazioni o riserve); vuoto se libera.</param>
public sealed record SpotAvailabilityDto(Guid SpotId, string Status, IReadOnlyList<DateOnly> BusyDays);

public sealed record HoldDto(Guid Id, Guid SpotId, string? SpotLabel, DateOnly From, DateOnly To, string DayPart, DateTime CreatedAt, Guid? CreatedBy);

/// <summary>Un limite di un servizio o di un tavolo superato, da mostrare al gestore (che può confermare).</summary>
/// <param name="Limit">Il limite (per <c>interval</c> i minuti da attendere; <c>Requested</c> = minuti passati dall'ultimo ordine).</param>
/// <param name="Rule">not_available, outside_hours, slot_not_aligned, max_per_day, max_per_hour, max_per_slot, min_items, max_items, interval,
/// table_slot, table_seats, max_bookings_per_slot, no_table, seats_full.</param>
public sealed record CapacityViolation(Guid ServiceId, string Name, DateOnly Date, string Rule, int? Limit, int? Requested);
