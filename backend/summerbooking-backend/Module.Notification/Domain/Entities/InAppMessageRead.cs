using Shared.Contracts;

namespace Module.Notification.Domain.Entities;

/// <summary>
/// Stato di lettura per (messaggio × utente): l'assenza di riga significa non letta. Figlia di
/// <see cref="InAppMessage"/>.
/// </summary>
public sealed class InAppMessageRead : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public long MessageId { get; private set; }
	public InAppMessage Message { get; private set; } = null!;
	public Guid UserId { get; private set; }
	public Guid PropertyId { get; private set; }
	public DateTime ReadAt { get; private set; }
	public DateTime? DismissedAt { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private InAppMessageRead() { }   // unico costruttore, per il materializzatore di EF Core
}
