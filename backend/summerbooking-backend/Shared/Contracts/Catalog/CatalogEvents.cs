using Shared.Contracts.Events;

namespace Shared.Contracts.Catalog;

/// <summary>
/// Struttura registrata dal form pubblico: Notification manda il benvenuto al referente e l'avviso a info@.
/// <see cref="Language"/> è il codice di una lingua gestita.
/// </summary>
public sealed record PropertyRegistered(
	Guid EventId, DateTime OccurredAt, Guid PropertyId, string Name, string City, string ContactFullName,
	string ContactEmail, string ContactPhone, string Language, int PhotoCount) : IIntegrationEvent;

/// <summary>
/// Il systemadmin cambia lo stato di uno stabilimento: <see cref="Status"/> = pending, approved, rejected, imported, hidden.
/// Solo approved e imported stanno sul sito; con pending il gestore non entra nel gestionale e non riceve email.
/// <see cref="Comment"/> è il motivo facoltativo, da mostrare al titolare.
/// </summary>
public sealed record PropertyStatusChanged(
	Guid EventId, DateTime OccurredAt, Guid PropertyId, string Status, string? PreviousStatus, string? Comment) : IIntegrationEvent;

/// <summary>
/// Stabilimento spostato nel cestino, ripristinato o eliminato definitivamente (<see cref="Change"/> = trashed, restored,
/// purged). Nel cestino le prenotazioni future e i pagamenti in sospeso si annullano senza rimborso né email; col
/// ripristino tornano attive; dall'eliminazione definitiva partono i tempi di conservazione dei dati.
/// </summary>
public sealed record PropertyTrashChanged(Guid EventId, DateTime OccurredAt, Guid PropertyId, string Change) : IIntegrationEvent;

/// <summary>Valori di <see cref="PropertyTrashChanged.Change"/>.</summary>
public static class PropertyTrashChanges
{
	public const string Trashed = "trashed";
	public const string Restored = "restored";
	public const string Purged = "purged";
}

/// <summary>Una postazione della mappa, come la vedono le prenotazioni: per ricollegarle dopo «Reimposta mappa».</summary>
/// <param name="SpotType">Codice snake_case del tipo (umbrella, sunbed…).</param>
public sealed record MapSpotRef(Guid Id, string? Label, string SpotType, short Row, short Column);

/// <summary>
/// Postazioni tolte o create nell'editor della mappa («Reimposta mappa», righe e colonne cancellate, postazioni eliminate o
/// aggiunte). Con <paramref name="DeletesBookings"/> le prenotazioni delle postazioni tolte si annullano; senza restano
/// senza postazione finché una postazione nuova dello stesso tipo non occupa la stessa posizione.
/// </summary>
public sealed record MapSpotsChanged(
	Guid EventId, DateTime OccurredAt, Guid PropertyId, IReadOnlyList<MapSpotRef> Removed, IReadOnlyList<MapSpotRef> Created, bool DeletesBookings)
	: IIntegrationEvent;

