using Module.Notification.Domain.Enums;
using Shared.Contracts;

namespace Module.Notification.Domain.Entities;

/// <summary>
/// Tabella ponte: appartenenza di un contatto marketing a una lista, con storico add/remove.
/// </summary>
public sealed class ContactListMembership : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public long ListId { get; private set; }
	public ContactList List { get; private set; } = null!;
	public long ContactId { get; private set; }
	public MarketingContact Contact { get; private set; } = null!;
	public Guid PropertyId { get; private set; }
	public ListMemberSource AddedBy { get; private set; }
	public DateTime AddedAt { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private ContactListMembership() { }   // unico costruttore, per il materializzatore di EF Core
}
