using Module.ZTest.Application.Abstractions;
using Module.ZTest.Application.Dtos;
using Module.ZTest.Application.Validators;
using Module.ZTest.Domain.Entities;
using Module.ZTest.Domain.Exceptions;
using Module.ZTest.Domain.ValueObjects;

namespace Module.ZTest.Application.Services;

/// <summary>
/// Servizio applicativo (use case) che orchestra dominio e persistenza.
/// Non conosce EF Core ne dettagli HTTP: dipende solo dalle astrazioni.
/// </summary>
public sealed class ReservationService
{
	private readonly IReservationRepository _repository;
	private readonly CreateReservationValidator _validator;

	public ReservationService(IReservationRepository repository, CreateReservationValidator validator)
	{
		_repository = repository;
		_validator = validator;
	}

	public async Task<ReservationDto> CreateAsync(CreateReservationRequest request, CancellationToken ct = default)
	{
		var errors = _validator.Validate(request);
		if (errors.Count > 0)
			throw new ArgumentException(string.Join(" ", errors));

		var period = new DateRange(request.From, request.To);

		if (await _repository.HasOverlapAsync(request.GuestId, period, ct))
			throw new InvalidOperationException("Esiste gia una prenotazione sovrapposta per questo ospite.");

		var reservation = Reservation.Create(request.GuestId, period, request.PricePerNight);

		await _repository.AddAsync(reservation, ct);
		await _repository.SaveChangesAsync(ct);

		return ReservationDto.FromEntity(reservation);
	}

	public async Task<ReservationDto> GetAsync(Guid id, CancellationToken ct = default)
	{
		var reservation = await _repository.GetByIdAsync(id, ct)
			?? throw new ReservationNotFoundException(id);

		return ReservationDto.FromEntity(reservation);
	}
}
