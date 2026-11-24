using Module.Booking.Domain.Entities;
using Module.Booking.Domain.Enums;

namespace Module.Booking.Application.Abstractions;

/// <summary>Un giorno già occupato su una postazione: da una riga di prenotazione o da una riserva.</summary>
public sealed record OccupiedDay(Guid SpotId, DateOnly Day, DayPeriod Period, ConsumptionSource Source, long? SourceId, int Quantity);

/// <summary>Allegati delle prenotazioni.</summary>
public interface IAttachmentRepository
{
	/// <summary>Allegati vivi della prenotazione (anche con il file già cancellato).</summary>
	Task<IReadOnlyList<BookingAttachment>> ListAsync(long reservationId, CancellationToken ct = default);

	/// <summary>Allegati con il file ancora in archivio caricati prima di <paramref name="cutoff"/>, di tutti gli stabilimenti.</summary>
	Task<IReadOnlyList<BookingAttachment>> ListWithFilesBeforeAsync(DateTime cutoff, int max, CancellationToken ct = default);

	void Add(BookingAttachment attachment);

	void Remove(BookingAttachment attachment);

	Task SaveChangesAsync(CancellationToken ct = default);
}

/// <summary>Comandi già eseguiti con la chiave di idempotenza.</summary>
public interface IProcessedCommandRepository
{
	Task<ProcessedCommand?> FindAsync(Guid propertyId, string key, CancellationToken ct = default);

	/// <summary>Ricorda il comando; se nel frattempo un'altra richiesta con la stessa chiave l'ha già ricordato, non fa nulla.</summary>
	Task TryAddAsync(ProcessedCommand command, CancellationToken ct = default);

	Task<int> PurgeBeforeAsync(DateTime cutoff, CancellationToken ct = default);
}

/// <summary>Una modifica per la sincronizzazione dei dispositivi.</summary>
public sealed record SyncChanges(IReadOnlyList<Reservation> Reservations, IReadOnlyList<SpotHold> Holds);

/// <summary>Quanto di un articolo di magazzino è già impegnato in un giorno.</summary>
public sealed record ItemUsage(Guid ItemId, DateOnly Day, int Quantity);

/// <summary>Quanto di un servizio è già prenotato in un giorno (e fascia, per gli orari).</summary>
public sealed record ServiceUsage(Guid ServiceId, DateOnly Day, TimeOnly? Start, TimeOnly? End, int Quantity);

/// <summary>Filtri dell'elenco delle prenotazioni; le date selezionano le prenotazioni che toccano l'intervallo.</summary>
/// <param name="PaymentStatus">paid, partial, unpaid.</param>
/// <param name="SpotIds">Prenotazioni con almeno una di queste postazioni (storico di una postazione).</param>
/// <param name="SearchSpotIds">Postazioni il cui nome corrisponde alla ricerca: si aggiungono agli altri risultati della ricerca.</param>
public sealed record ReservationFilter(
	Guid PropertyId, DateOnly? From, DateOnly? To, ReservationStatus? Status, string? PaymentStatus, BookingChannel? Channel,
	IReadOnlyCollection<Guid>? CustomerIds, string? NumberSearch, Guid? AgreementPartnerId, bool? Seasonal, bool Trashed,
	string SortBy, bool Descending, int Page, int PageSize, IReadOnlyCollection<Guid>? SpotIds = null,
	IReadOnlyCollection<Guid>? SearchSpotIds = null);

/// <summary>Persistenza dell'aggregato Prenotazione e dei suoi consumi.</summary>
public interface IReservationRepository
{
	/// <summary>Prenotazione con righe, aggiuntivi, convenzioni, sconti e ospiti; null se non è dello stabilimento.</summary>
	Task<Reservation?> GetAsync(Guid propertyId, Guid publicId, bool includeTrashed = false, CancellationToken ct = default);

	/// <summary>Prenotazione viva che contiene la riga indicata.</summary>
	Task<Reservation?> FindByLineAsync(Guid propertyId, Guid linePublicId, CancellationToken ct = default);

	Task<IReadOnlyList<Reservation>> GetManyAsync(Guid propertyId, IReadOnlyCollection<Guid> publicIds, bool trashed, CancellationToken ct = default);

	Task<(IReadOnlyList<Reservation> Items, int Total)> ListAsync(ReservationFilter filter, CancellationToken ct = default);

	/// <summary>Righe vive delle prenotazioni attive che toccano l'intervallo, con la testata.</summary>
	Task<IReadOnlyList<BookingLine>> ListLinesAsync(Guid propertyId, DateOnly from, DateOnly to, CancellationToken ct = default);

	/// <summary>Giorni occupati (consumi non rilasciati) sulle postazioni indicate nell'intervallo.</summary>
	Task<IReadOnlyList<OccupiedDay>> ListOccupiedAsync(Guid propertyId, IReadOnlyCollection<Guid> spotIds, DateOnly from, DateOnly to, CancellationToken ct = default);

	Task<IReadOnlyList<ResourceConsumption>> ListLineConsumptionsAsync(IReadOnlyCollection<long> lineIds, CancellationToken ct = default);

	/// <summary>Prenotazioni vive dello stabilimento con la fine dal giorno indicato in poi (null = tutte), con le righe.</summary>
	Task<IReadOnlyList<Reservation>> ListForPropertyAsync(Guid propertyId, DateOnly? endingFrom, CancellationToken ct = default);

	Task<IReadOnlyList<BookingEntrance>> ListEntrancesAsync(Guid propertyId, DateOnly day, CancellationToken ct = default);

	Task<string> NextBookingNumberAsync(Guid propertyId, string prefix, CancellationToken ct = default);

	Task<bool> ReservationCodeExistsAsync(string code, CancellationToken ct = default);

	void Add(Reservation reservation);

	void AddConsumptions(IEnumerable<ResourceConsumption> consumptions);

	void AddEntrance(BookingEntrance entrance);

	void RemoveEntrance(BookingEntrance entrance);

	void RemoveLine(BookingLine line);

	void RemoveChildren(Reservation reservation, BookingLine? onlyLine = null);

	/// <summary>Nel cestino: cancellazione logica della sola testata (righe, aggiuntivi e ospiti restano com'erano).</summary>
	void Trash(Reservation reservation, DateTime now);

	/// <summary>
	/// Prenotazioni messe nel cestino prima che la testata si cancellasse da sola: righe, aggiuntivi, convenzioni, sconti e
	/// ospiti cancellati insieme a lei tornano vivi, così il ripristino li ritrova.
	/// </summary>
	Task ReviveTrashedChildrenAsync(Guid propertyId, IReadOnlyCollection<Guid> reservationIds, CancellationToken ct = default);

	/// <summary>Fuori dal cestino: la cancellazione logica si toglie.</summary>
	void Untrash(Reservation reservation);

	/// <summary>Prenotazioni nel cestino da prima di <paramref name="cutoff"/>, di tutti gli stabilimenti.</summary>
	Task<IReadOnlyList<(Guid PropertyId, Guid ReservationId, string BookingNumber)>> ListTrashedBeforeAsync(DateTime cutoff, int max, CancellationToken ct = default);

	/// <summary>Elimina davvero le prenotazioni (e i loro consumi) dal database; restituisce quante.</summary>
	Task<int> PurgeAsync(Guid propertyId, IReadOnlyCollection<Guid> reservationIds, CancellationToken ct = default);

	Task<IReadOnlyList<ResourceConsumption>> ListServiceConsumptionsAsync(IReadOnlyCollection<long> serviceLineIds, CancellationToken ct = default);

	/// <summary>Articoli di magazzino impegnati nei giorni indicati, esclusi quelli della prenotazione indicata.</summary>
	Task<IReadOnlyList<ItemUsage>> ListItemUsageAsync(
		Guid propertyId, IReadOnlyCollection<Guid> itemIds, DateOnly from, DateOnly to, long excludeReservationId, CancellationToken ct = default);

	/// <summary>Consumo tolto (cancellazione logica): righe ricalcolate, che non devono tornare in vita con il ripristino.</summary>
	void RemoveConsumption(ResourceConsumption consumption);

	/// <summary>Servizi già prenotati nei giorni indicati, esclusi quelli della prenotazione indicata.</summary>
	/// <summary>Ultimo ordine di ciascun servizio del cliente dopo <paramref name="sinceUtc"/>, nelle altre prenotazioni vive.</summary>
	Task<IReadOnlyDictionary<Guid, DateTime>> LastServiceOrdersAsync(
		Guid propertyId, Guid customerId, IReadOnlyCollection<Guid> serviceIds, DateTime sinceUtc, long excludeReservationId, CancellationToken ct = default);

	Task<IReadOnlyList<ServiceUsage>> ListServiceUsageAsync(
		Guid propertyId, IReadOnlyCollection<Guid> serviceIds, DateOnly from, DateOnly to, long excludeReservationId, CancellationToken ct = default);

	/// <summary>Tavoli prenotati (prenotazioni vive) nei giorni indicati, esclusi quelli della prenotazione indicata.</summary>
	Task<IReadOnlyList<BookingTableLine>> ListTableLinesAsync(
		Guid propertyId, IReadOnlyCollection<Guid> tableServiceIds, DateOnly from, DateOnly to, long excludeReservationId, CancellationToken ct = default);

	/// <summary>Righe dei servizi orari di un giorno, con la prenotazione (planner dei servizi orari).</summary>
	Task<IReadOnlyList<BookingServiceTimeSlot>> ListServiceSlotsAsync(Guid propertyId, DateOnly day, CancellationToken ct = default);

	void RemoveServiceLine(BookingServiceLine line);

	void RemoveTableLine(BookingTableLine line);

	/// <summary>Prenotazioni vive di questi clienti, in qualsiasi stabilimento (area cliente).</summary>
	Task<IReadOnlyList<Reservation>> ListByCustomersAsync(IReadOnlyCollection<Guid> customerIds, CancellationToken ct = default);

	/// <summary>Prenotazioni online in attesa con il pagamento o la conferma scaduti.</summary>
	Task<IReadOnlyList<Reservation>> ListOnlineExpiredAsync(DateTime now, int max, CancellationToken ct = default);

	/// <summary>Postazioni per giorno delle prenotazioni online vive del cliente (limite giornaliero).</summary>
	Task<IReadOnlyDictionary<DateOnly, int>> CountOnlineSpotsPerDayAsync(Guid propertyId, Guid customerId, DateOnly from, DateOnly to, CancellationToken ct = default);

	/// <summary>Consumi di tutte le righe (postazioni e servizi) della prenotazione.</summary>
	Task<IReadOnlyList<ResourceConsumption>> ListAllConsumptionsAsync(Reservation reservation, CancellationToken ct = default);

	/// <summary>
	/// Prenotazioni (anche annullate e nel cestino) e riserve cambiate dopo il cursore, più vecchie di qualche secondo (le
	/// transazioni ancora aperte non devono far saltare numeri).
	/// </summary>
	Task<SyncChanges> ListChangesAsync(Guid propertyId, long since, DateTime olderThan, int max, CancellationToken ct = default);

	/// <summary>C'è già un rinnovo vivo (non nel cestino) di questa prenotazione.</summary>
	Task<bool> HasRenewalAsync(long reservationId, CancellationToken ct = default);

	Task SaveChangesAsync(CancellationToken ct = default);

	/// <summary>Scarta le modifiche non salvate (dopo un errore in un'operazione su più prenotazioni).</summary>
	void DiscardChanges();

	Task<IBookingTransaction> BeginTransactionAsync(CancellationToken ct = default);
}

/// <summary>Il gruppo di una prenotazione: id, nome e quante prenotazioni contiene.</summary>
public sealed record GroupInfo(long Id, Guid PublicId, string Name, int ReservationCount);

/// <summary>Gruppi di prenotazioni dello stabilimento.</summary>
public interface IBookingGroupRepository
{
	/// <summary>Gruppo con i membri vivi e le loro prenotazioni (con le righe).</summary>
	Task<BookingGroup?> GetAsync(Guid propertyId, Guid publicId, CancellationToken ct = default);

	Task<IReadOnlyList<BookingGroup>> GetManyAsync(Guid propertyId, IReadOnlyCollection<Guid> publicIds, CancellationToken ct = default);

	/// <summary>Elenco con i membri vivi e le loro prenotazioni; ricerca sul nome del gruppo.</summary>
	Task<(IReadOnlyList<BookingGroup> Items, int Total)> ListAsync(
		Guid propertyId, string? search, string sortBy, bool descending, int page, int pageSize, CancellationToken ct = default);

	Task<bool> NameExistsAsync(Guid propertyId, string name, long? exceptId, CancellationToken ct = default);

	/// <summary>Gruppo vivo di ciascuna delle prenotazioni indicate (id interni), se ne hanno uno.</summary>
	Task<IReadOnlyDictionary<long, GroupInfo>> GroupsOfAsync(IReadOnlyCollection<long> reservationIds, CancellationToken ct = default);

	void Add(BookingGroup group);

	/// <summary>Toglie il membro (cancellazione logica).</summary>
	void RemoveMember(BookingGroupMember member);

	/// <summary>Gruppo eliminato con i suoi membri (cancellazione logica); le prenotazioni restano.</summary>
	void Remove(BookingGroup group);

	Task SaveChangesAsync(CancellationToken ct = default);
}

/// <summary>Riserve delle postazioni senza cliente.</summary>
public interface ISpotHoldRepository
{
	Task<SpotHold?> GetAsync(Guid propertyId, Guid publicId, CancellationToken ct = default);

	Task<IReadOnlyList<SpotHold>> ListActiveAsync(Guid propertyId, DateOnly from, DateOnly to, IReadOnlyCollection<Guid>? spotIds = null, CancellationToken ct = default);

	Task<IReadOnlyList<ResourceConsumption>> ListConsumptionsAsync(IReadOnlyCollection<long> holdIds, CancellationToken ct = default);

	void Add(SpotHold hold);
}

/// <summary>Transazione esplicita: righe e consumi si salvano in due passi (i consumi vogliono l'id delle righe).</summary>
public interface IBookingTransaction : IAsyncDisposable
{
	Task CommitAsync(CancellationToken ct = default);
}

/// <summary>Sharing e assenze: impostazioni, giorni ceduti, crediti dei titolari.</summary>
public interface IResaleRepository
{
	/// <summary>Impostazioni dello stabilimento (tracciate); null se non ancora salvate.</summary>
	Task<ResaleSettings?> GetSettingsAsync(Guid propertyId, CancellationToken ct = default);

	void AddSettings(ResaleSettings settings);

	/// <summary>Giorni ceduti vivi (disponibili o ripresi) delle prenotazioni indicate, come titolari.</summary>
	Task<IReadOnlyList<ResaleOffer>> ListBySourceAsync(IReadOnlyCollection<long> reservationIds, CancellationToken ct = default);

	/// <summary>Giorni ceduti ancora disponibili sulle postazioni e nei giorni indicati.</summary>
	Task<IReadOnlyList<ResaleOffer>> ListAvailableAsync(Guid propertyId, IReadOnlyCollection<Guid> spotIds, DateOnly from, DateOnly to, CancellationToken ct = default);

	/// <summary>Giorni ceduti che la prenotazione ha ripreso.</summary>
	Task<IReadOnlyList<ResaleOffer>> ListTakenByAsync(long reservationId, CancellationToken ct = default);

	/// <summary>Giorni ceduti vivi in un giorno (per la mappa).</summary>
	Task<IReadOnlyList<ResaleOffer>> ListOnDayAsync(Guid propertyId, DateOnly day, CancellationToken ct = default);

	Task<IReadOnlyList<ResaleOffer>> GetOffersAsync(Guid propertyId, IReadOnlyCollection<Guid> publicIds, CancellationToken ct = default);

	/// <summary>Giorni ceduti dello stabilimento per la pagina «Sharing creati», con la prenotazione del titolare e la riga.</summary>
	Task<IReadOnlyList<ResaleOffer>> SearchAsync(Guid propertyId, DateOnly? from, DateOnly? to, IReadOnlyCollection<ResaleOrigin>? origins, int max, CancellationToken ct = default);

	/// <summary>Somma dei crediti maturati per l'offerta (accrediti meno storni).</summary>
	Task<(int Units, decimal Amount, bool EverEarned)> CreditOfAsync(long offerId, CancellationToken ct = default);

	Task<IReadOnlyList<ResaleCreditLedgerEntry>> ListCreditsAsync(Guid propertyId, Guid customerId, CancellationToken ct = default);

	void AddOffer(ResaleOffer offer);

	void AddCredit(ResaleCreditLedgerEntry entry);

	Task SaveChangesAsync(CancellationToken ct = default);
}
