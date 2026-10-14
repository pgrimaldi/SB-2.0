using Module.ZTest.Domain.Entities;

namespace Module.ZTest.Application.Dtos;

/// <summary>
/// DTO di lettura restituito verso l'esterno (Endpoints). Non espone entita di dominio.
/// </summary>
public sealed record ReservationDto(
	Guid Id,
	Guid GuestId,
	DateOnly From,
	DateOnly To,
	string Status,
	decimal TotalPrice)
{
	public static ReservationDto FromEntity(Reservation r) => new(
		r.Id,
		r.GuestId,
		r.Period.From,
		r.Period.To,
		r.Status.ToString(),
		r.TotalPrice);
}

/// <summary>
/// Payload di creazione ricevuto dagli Endpoints.
/// </summary>
public sealed record CreateReservationRequest(
	Guid GuestId,
	DateOnly From,
	DateOnly To,
	decimal PricePerNight);
