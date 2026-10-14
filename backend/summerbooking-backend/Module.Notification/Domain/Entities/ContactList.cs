using Module.Notification.Domain.Enums;
using Shared.Contracts;

namespace Module.Notification.Domain.Entities;

/// <summary>
/// Segmento di clientela di uno stabilimento, target di campagne e automazioni marketing.
/// </summary>
public sealed class ContactList : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public Guid PublicId { get; private set; }
	public Guid PropertyId { get; private set; }
	public string Name { get; private set; } = null!;
	public bool IsDefault { get; private set; }
	public ContactListOrigin Origin { get; private set; }
	public int MemberCount { get; private set; }
	public Guid? CreatedByUserId { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	public ICollection<ContactListMembership> Members { get; private set; } = null!;

	private ContactList() { }   // unico costruttore, per il materializzatore di EF Core
}
