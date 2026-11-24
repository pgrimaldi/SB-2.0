using Module.Payment.Domain.Entities;

namespace Module.Payment.Application.Abstractions;

/// <summary>Movimenti di cassa delle prenotazioni.</summary>
public interface ILedgerRepository
{
	Task<IReadOnlyList<LedgerEntry>> ListByReservationAsync(Guid propertyId, Guid reservationId, CancellationToken ct = default);

	Task<LedgerEntry?> FindAsync(Guid propertyId, Guid entryId, CancellationToken ct = default);

	void Add(LedgerEntry entry);

	/// <summary>Annulla il movimento (cancellazione logica): resta nella base dati e nel registro.</summary>
	void Void(LedgerEntry entry);

	Task SaveChangesAsync(CancellationToken ct = default);
}
