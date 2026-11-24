using Shared.Contracts;

namespace Module.Booking.Domain.Entities;

/// <summary>
/// Raggruppamento manuale di prenotazioni distinte sotto un'etichetta scelta dall'operatore (comitiva,
/// famiglia su più ombrelloni); non è la prenotazione multipla.
/// </summary>
public sealed class BookingGroup : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public Guid PublicId { get; private set; }
	public Guid PropertyId { get; private set; }
	public string Name { get; private set; } = null!;
	public Guid? CreatedByUserId { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	public ICollection<BookingGroupMember> Members { get; private set; } = new List<BookingGroupMember>();

	public const int NameMaxLength = 120;

	private BookingGroup() { }

	public static BookingGroup Create(Guid propertyId, string name, Guid? userId)
	{
		var group = new BookingGroup { PublicId = Guid.CreateVersion7(), PropertyId = propertyId, CreatedByUserId = userId };
		group.Rename(name);
		return group;
	}

	public void Rename(string name)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(name);
		var clean = name.Trim();
		if (clean.Length > NameMaxLength)
			throw new ArgumentOutOfRangeException(nameof(name));
		Name = clean;
	}

	/// <summary>Membri ancora nel gruppo (quelli tolti restano come cancellazione logica).</summary>
	public IEnumerable<BookingGroupMember> LiveMembers => Members.Where(m => m.DeletedAt is null);

	public BookingGroupMember Add(Reservation reservation, DateTime now)
	{
		var member = BookingGroupMember.Create(this, reservation, now);
		Members.Add(member);
		return member;
	}
}
