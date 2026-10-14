using Shared.Contracts;

namespace Module.Booking.Domain.Entities;

/// <summary>
/// Raggruppamento manuale di prenotazioni distinte sotto un'etichetta scelta dall'operatore (comitiva,
/// famiglia su più ombrelloni); non è la prenotazione multipla.
/// </summary>
public sealed class BookingGroup : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public Guid PropertyId { get; private set; }
	public string Name { get; private set; } = null!;
	public Guid? CreatedByUserId { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	public ICollection<BookingGroupMember> Members { get; private set; } = new List<BookingGroupMember>();

	private BookingGroup() { }
}
