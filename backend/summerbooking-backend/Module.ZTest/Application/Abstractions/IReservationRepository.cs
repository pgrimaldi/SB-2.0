using Module.ZTest.Domain.Entities;
using Module.ZTest.Domain.ValueObjects;

namespace Module.ZTest.Application.Abstractions;

/// <summary>
/// Porta (port) verso la persistenza delle prenotazioni.
/// L'Application dipende da questa astrazione; l'implementazione vive in Infrastructure.
/// </summary>
public interface IReservationRepository
{
	Task<Reservation?> GetByIdAsync(Guid id, CancellationToken ct = default);

	Task AddAsync(Reservation reservation, CancellationToken ct = default);

	Task<bool> HasOverlapAsync(Guid guestId, DateRange period, CancellationToken ct = default);

	Task SaveChangesAsync(CancellationToken ct = default);
}
