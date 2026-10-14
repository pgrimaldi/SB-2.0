using Module.Notification.Domain.Enums;
using Shared.Contracts;

namespace Module.Notification.Domain.Entities;

/// <summary>
/// Iscrizione alla newsletter globale di piattaforma (nessuno stabilimento associato).
/// </summary>
public sealed class NewsletterSubscription : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public Guid PublicId { get; private set; }
	public string Email { get; private set; } = null!;
	public NewsletterStatus Status { get; private set; }
	public NewsletterSource Source { get; private set; }
	public string? ConfirmationToken { get; private set; }
	public DateTime? ConfirmedAt { get; private set; }
	public string UnsubscribeToken { get; private set; } = null!;
	public string? ConsentNoticeVersion { get; private set; }
	public Guid? CustomerPublicId { get; private set; }
	public DateTime? UnsubscribedAt { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private NewsletterSubscription() { }   // unico costruttore, per il materializzatore di EF Core
}
