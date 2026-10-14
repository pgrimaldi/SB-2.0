using Module.ZTest.Domain.Enums;
using Module.ZTest.Domain.Events;
using Module.ZTest.Domain.ValueObjects;

namespace Module.ZTest.Domain.Entities;

/// <summary>
/// Aggregate Root che rappresenta una prenotazione.
/// Racchiude le invarianti e la logica di dominio; non conosce persistenza ne HTTP.
/// </summary>
public sealed class Reservation
{
	private readonly List<IDomainEvent> _domainEvents = new();

	public Guid Id { get; private set; }
	public Guid GuestId { get; private set; }
	public DateRange Period { get; private set; }
	public ReservationStatus Status { get; private set; }
	public decimal TotalPrice { get; private set; }

	private Reservation(Guid id, Guid guestId, DateRange period, decimal totalPrice)
	{
		Id = id;
		GuestId = guestId;
		Period = period;
		TotalPrice = totalPrice;
		Status = ReservationStatus.Pending;
	}

	public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

	/// <summary>Factory: crea una prenotazione valida e registra l'evento di dominio.</summary>
	public static Reservation Create(Guid guestId, DateRange period, decimal pricePerNight)
	{
		if (guestId == Guid.Empty)
			throw new ArgumentException("GuestId obbligatorio.", nameof(guestId));

		if (pricePerNight <= 0)
			throw new ArgumentOutOfRangeException(nameof(pricePerNight), "Il prezzo per notte deve essere positivo.");

		var reservation = new Reservation(Guid.CreateVersion7(), guestId, period, pricePerNight * period.Nights);
		reservation._domainEvents.Add(new ReservationCreatedEvent(reservation.Id, guestId));
		return reservation;
	}

	public void Confirm()
	{
		if (Status != ReservationStatus.Pending)
			throw new InvalidOperationException("Solo una prenotazione in attesa puo essere confermata.");

		Status = ReservationStatus.Confirmed;
	}

	public void Cancel()
	{
		if (Status == ReservationStatus.Completed)
			throw new InvalidOperationException("Una prenotazione completata non puo essere annullata.");

		Status = ReservationStatus.Cancelled;
	}

	public void ClearDomainEvents() => _domainEvents.Clear();
}
