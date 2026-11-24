namespace Module.Booking.Application.Dtos;

/// <summary>Una prenotazione cambiata: il dispositivo ricarica il dettaglio se gli serve.</summary>
/// <param name="Removed">Annullata o nel cestino: il dispositivo la toglie dalla mappa.</param>
public sealed record SyncReservationDto(Guid Id, string BookingNumber, string Status, bool Removed, uint Version, long Seq, DateOnly? From, DateOnly? To,
	IReadOnlyList<SyncLineDto> Lines);

public sealed record SyncLineDto(Guid LineId, Guid? SpotId, DateOnly From, DateOnly To, string DayPart);

public sealed record SyncHoldDto(Guid Id, Guid SpotId, DateOnly From, DateOnly To, string DayPart, bool Active, long Seq);

/// <param name="Cursor">Da rimandare come <c>since</c> alla chiamata successiva.</param>
/// <param name="HasMore">Ci sono altre modifiche: richiamare subito con il nuovo cursore.</param>
public sealed record SyncDto(long Cursor, bool HasMore, IReadOnlyList<SyncReservationDto> Reservations, IReadOnlyList<SyncHoldDto> Holds);
